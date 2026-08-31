using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ClinicAppointmentBookingSystem.IntegrationTests.RateLimiting;

// CustomWebApplicationFactory raises the rate limits sky-high by default, so
// ordinary functional tests elsewhere in the suite don't trip them just by
// calling an endpoint like /auth/register once per test. This subclass
// layers the real production values back on top (config providers added
// later win), since these specific tests exist to verify that enforcement.
public class StrictRateLimitFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RateLimiting:ApiPermitLimit"] = "100",
                ["RateLimiting:AuthPermitLimit"] = "5",
            }));
    }
}

// The "auth" rate limit policy (5 requests/min per IP, see Program.cs) is a
// fixed-window limiter with real wall-clock time, so these tests only ever
// need to fire requests fast enough to land inside one window - never wait
// for a window to expire.
//
// This class gets its own StrictRateLimitFactory instance (a fresh DI
// container, and therefore a fresh in-memory rate limiter) so its request
// counts can never leak into - or be polluted by - AuthControllerTests'
// existing functional Login/Register tests, which share a factory instance
// across all their own [Fact]s too.
public class AuthRateLimitingTests(StrictRateLimitFactory factory)
    : IClassFixture<StrictRateLimitFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        factory.ResetDatabase();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_SixthRequestWithinWindow_Returns429()
    {
        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/auth/login", BadLogin());

        var response = await _client.PostAsJsonAsync("/auth/login", BadLogin());

        response.StatusCode.Should().Be((HttpStatusCode)429);
        response.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(60));
        // OnRejected in Program.cs writes plain text, not a JSON body.
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Too many requests. Try again shortly.");
    }

    [Fact]
    public async Task Register_SixthRequestWithinWindow_Returns429()
    {
        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/auth/register", ValidRegister());

        var response = await _client.PostAsJsonAsync("/auth/register", ValidRegister());

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task AuthPolicy_IsSharedBudgetAcrossLoginAndRegister_ReturnsRateLimitedOnCombinedSixthRequest()
    {
        // [EnableRateLimiting("auth")] on both Login and Register refers to the
        // SAME named policy, partitioned only by client IP - not one separate
        // 5/min budget per endpoint. 3 + 3 from one client should trip the
        // limit on the 6th combined request, regardless of which endpoint it
        // hits.
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync("/auth/login", BadLogin());
        for (var i = 0; i < 2; i++)
            await _client.PostAsJsonAsync("/auth/register", ValidRegister());

        var response = await _client.PostAsJsonAsync("/auth/login", BadLogin());

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task GeneralApiPolicy_Returns429After100RequestsPerMinute()
    {
        // The general "api" policy (100/min) applies globally via
        // MapControllers().RequireRateLimiting("api") - exercised here through
        // a public, unauthenticated GET so nothing else about the request
        // (auth, validation) can produce a non-200/429 status and confuse the
        // assertion.
        for (var i = 0; i < 100; i++)
            await _client.GetAsync("/specialities");

        var response = await _client.GetAsync("/specialities");

        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task RateLimiting_PartitionsByForwardedForHeader_NotBySharedTestServerAddress()
    {
        // Without ForwardedHeadersMiddleware correctly reading X-Forwarded-For,
        // every request through Caddy in production would appear to come from
        // Caddy's own container IP - collapsing rate limiting into one shared
        // bucket for every real visitor, exactly the failure mode called out
        // in Program.cs's forwarded-headers comment. Two different
        // X-Forwarded-For values must therefore get two independent budgets.
        for (var i = 0; i < 5; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
            {
                Content = JsonContent.Create(BadLogin())
            };
            request.Headers.Add("X-Forwarded-For", "203.0.113.10");
            await _client.SendAsync(request);
        }

        var sameClientSixthRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(BadLogin())
        };
        sameClientSixthRequest.Headers.Add("X-Forwarded-For", "203.0.113.10");
        var sixthResponse = await _client.SendAsync(sameClientSixthRequest);
        sixthResponse.StatusCode.Should().Be((HttpStatusCode)429);

        var otherClientRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(BadLogin())
        };
        otherClientRequest.Headers.Add("X-Forwarded-For", "203.0.113.99");
        var otherClientResponse = await _client.SendAsync(otherClientRequest);

        // A different client IP must still have its own untouched budget.
        otherClientResponse.StatusCode.Should().NotBe((HttpStatusCode)429);
    }

    private static LoginRequest BadLogin() => new()
    {
        Email = "nobody@example.com",
        Password = "wrong-password"
    };

    private static RegisterRequest ValidRegister() => new()
    {
        FirstName = "Rate",
        LastName = "Limited",
        Email = $"ratelimit.{Guid.NewGuid()}@example.com",
        Password = "Password123!",
        Birthdate = new DateTime(1990, 1, 1),
        Gender = "Female"
    };
}
