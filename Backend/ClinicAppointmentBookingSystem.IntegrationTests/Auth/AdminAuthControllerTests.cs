using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Admin;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Auth;

public class AdminAuthControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        factory.ResetDatabase();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // POST /admin/auth/login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", ValidAdminLogin());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndAdminInfo()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", ValidAdminLogin());
        var body = await response.Content.ReadFromJsonAsync<AdminAuthResponse>();

        body!.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be("admin@clinicbook.com");
        body.FirstName.Should().Be("Admin");
        body.LastName.Should().Be("User");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsValidJwtWithAdminRole()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", ValidAdminLogin());
        var body = await response.Content.ReadFromJsonAsync<AdminAuthResponse>();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(body!.Token).Should().BeTrue();

        var token = handler.ReadJwtToken(body.Token);
        // The admin JWT uses "role" as the claim name (configured via RoleClaimType in Program.cs)
        token.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest
        {
            Email    = "admin@clinicbook.com",
            Password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest
        {
            Email    = "nobody@clinicbook.com",
            Password = "Admin@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login",
            new { Email = "admin@clinicbook.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest
        {
            Email    = "not-an-email",
            Password = "Admin@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static AdminLoginRequest ValidAdminLogin() => new()
    {
        Email    = "admin@clinicbook.com",
        Password = "Admin@123"
    };
}
