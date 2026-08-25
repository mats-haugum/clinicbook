// Reference wiring for the API. Merge the relevant pieces into your existing
// Program.cs - this is not meant to be dropped in wholesale.
//
// Required packages:
//   Microsoft.EntityFrameworkCore.SqlServer
//   Microsoft.Extensions.Caching.StackExchangeRedis
//   AspNetCore.HealthChecks.SqlServer
//   AspNetCore.HealthChecks.Redis

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Forwarded headers
// ---------------------------------------------------------------------------
// Traffic arrives as: client -> Cloudflare -> cloudflared -> Caddy -> Kestrel.
// Without this, every request looks like it comes from the Caddy container and
// per-IP rate limiting silently degrades into a single global bucket.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Docker networks are dynamic, so clearing these accepts the header from
    // the container network. Safe here because nothing else can reach Kestrel.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ---------------------------------------------------------------------------
// Redis distributed cache
// ---------------------------------------------------------------------------
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "app:";
});

// ---------------------------------------------------------------------------
// Output caching
// ---------------------------------------------------------------------------
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());

    options.AddPolicy("short", policy => policy
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("*"));

    options.AddPolicy("long", policy => policy
        .Expire(TimeSpan.FromMinutes(10))
        .SetVaryByQuery("*"));
});

// ---------------------------------------------------------------------------
// Rate limiting
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General API traffic, partitioned by real client IP.
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Tighter bucket for auth endpoints - brute force protection.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Try again shortly.", token);
    };
});

// ---------------------------------------------------------------------------
// Health checks
// ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "mssql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

builder.Services.AddControllers();

var app = builder.Build();

// Order matters: forwarded headers must run before anything that reads the IP.
app.UseForwardedHeaders();

app.UseExceptionHandler("/error");
app.UseHsts();

app.UseOutputCache();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("api");

app.Run();

// Usage on endpoints:
//
//   [HttpGet]
//   [OutputCache(PolicyName = "short")]
//   public async Task<IActionResult> GetItems() =>
//       Ok(await _db.Items.AsNoTracking()
//           .Select(i => new ItemDto(i.Id, i.Name))
//           .ToListAsync());
//
//   [HttpPost("login")]
//   [EnableRateLimiting("auth")]
//   public async Task<IActionResult> Login(LoginRequest request) { ... }
