using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Services;
using ClinicAppointmentBookingSystem.Services.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Traffic in production arrives as: client -> Cloudflare -> cloudflared -> Caddy -> Kestrel.
// Without this, every request looks like it comes from the Caddy container - the
// rate limiter below would see one IP for all traffic instead of each real client.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Docker networks are dynamic, so clearing these accepts the header from
    // the container network. Safe here because nothing else can reach Kestrel
    // directly - Caddy is the only thing in front of it.
    // KnownIPNetworks replaces the older KnownNetworks property (.NET 10).
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();
builder.Services.AddScoped<ISpecialityService, SpecialityService>();
builder.Services.AddScoped<IAppointmentCategoryService, AppointmentCategoryService>();
builder.Services.AddScoped<IClinicService, ClinicService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

builder.Services.AddDbContext<ClinicBookingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // Retries a handful of known-transient SQL Server errors (e.g. a brief
        // network blip) automatically instead of failing the request outright.
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Redis-backed distributed cache. Registered as IDistributedCache for anything
// that needs to share cached data across multiple API instances - the API
// container itself only ever runs as one instance here, but this keeps the
// door open without an infrastructure change later.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "clinicbook:";
});

// Output caching: caches the full HTTP response for endpoints tagged with
// [OutputCache(PolicyName = "...")], keyed separately per distinct query
// string (SetVaryByQuery("*")) so e.g. ?name=wilson and ?name=connor don't
// collide. NoCache() as the base policy means nothing is cached unless an
// endpoint opts in explicitly.
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

// Rate limiting: caps how many requests one client IP can make in a time
// window. The general 100/min cap applies to every request via GlobalLimiter
// - not a named "api" policy attached through MapControllers().RequireRateLimiting(),
// because that would silently lose to a more specific per-action policy: ASP.NET
// Core resolves an endpoint's rate limiter by taking the LAST matching policy in
// its metadata, and MVC attributes like [EnableRateLimiting("auth")] are baked into
// endpoint metadata before route-level conventions run - so a convention-applied
// policy always wins over a per-action attribute, the opposite of what you'd
// expect from e.g. [Authorize]. GlobalLimiter doesn't have this problem: it runs
// for every request IN ADDITION to any named policy, so "auth" (5/min) still
// applies its own tighter cap on top of this one for Login/Register.
// Limits are read from configuration (RateLimiting section in appsettings.json)
// rather than hardcoded, specifically so CustomWebApplicationFactory (the test
// project's shared test host) can loosen them for ordinary functional tests -
// e.g. AppointmentsControllerTests registers a fresh user in every single
// test's setup, which would otherwise trip the tight "auth" limit purely as a
// side effect of running the test suite quickly, nothing to do with abuse.
// The config is read from httpContext.RequestServices INSIDE each partition
// callback (not captured into a local variable up here) so it reflects
// whatever IConfiguration is actually resolved once the app is fully built -
// a plain local variable would freeze in whatever value existed at this
// exact line, which isn't guaranteed to already include a test factory's
// ConfigureAppConfiguration override.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = config.GetValue("RateLimiting:ApiPermitLimit", 100),
                Window = TimeSpan.FromSeconds(config.GetValue("RateLimiting:ApiWindowSeconds", 60)),
                QueueLimit = 0
            });
    });

    options.AddPolicy("auth", httpContext =>
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = config.GetValue("RateLimiting:AuthPermitLimit", 5),
                Window = TimeSpan.FromSeconds(config.GetValue("RateLimiting:AuthWindowSeconds", 60)),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, token) =>
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var windowSeconds = config.GetValue("RateLimiting:AuthWindowSeconds", 60);
        context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Try again shortly.", token);
    };
});

// Exposes /health, checking real connectivity to SQL Server and Redis rather
// than just "is the process running". The Caddyfile already proxies /health
// to this container (deploy/Caddyfile) - this is what makes that route
// actually answer instead of 404ing.
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "mssql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
            // Tell ASP.NET Core that the "role" claim in our JWTs is the role claim.
            // Without this, [Authorize(Roles = "Admin")] would look for the long
            // ClaimTypes.Role URI instead of the short "role" name we put in the token.
            RoleClaimType = "role",
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Clinic Appointment Booking API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    };
    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// One-off CLI mode: reset the demo database and exit immediately, without
// starting Kestrel. Run via `dotnet ClinicAppointmentBookingSystem.dll
// --reset-demo` (see deploy/README.md for the cron schedule that invokes
// this against the live demo server).
if (args.Contains("--reset-demo"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
    await DemoResetService.ResetAsync(db);
    return;
}

// On startup, create the default admin account if one does not exist yet.
// Credentials come from appsettings.json → AdminSeed, so they are never hardcoded.
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await AdminSeeder.SeedAsync(db, config);
}

// Must run before anything that reads the client's IP or scheme (the rate
// limiter's partition key, HTTPS redirection, HSTS) - otherwise they'd all
// see the Caddy container's connection instead of the real client's.
app.UseForwardedHeaders();

// ./Properties/launchSettings.json
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "doc";
    });
}
else
{
    // Development skips these: local HTTP-only testing doesn't need HSTS,
    // and the default developer exception page is more useful than a
    // generic /error response while debugging.
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseOutputCache();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
// No .RequireRateLimiting("api") here - the 100/min cap is applied to every
// request already via GlobalLimiter above, which composes with per-action
// policies like [EnableRateLimiting("auth")] instead of overriding them.
app.MapControllers();

// Minimal fallback for UseExceptionHandler("/error") above - returns a
// generic RFC 7807 ProblemDetails JSON body instead of leaking exception
// details to the client.
app.Map("/error", () => Results.Problem());

app.Run();
