using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Auth;

public class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        factory.ResetDatabase();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Register
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", ValidRegisterRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsTokenAndUserInfo()
    {
        var request = ValidRegisterRequest();

        var response = await _client.PostAsJsonAsync("/auth/register", request);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        body!.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(request.Email);
        body.FirstName.Should().Be(request.FirstName);
        body.LastName.Should().Be(request.LastName);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsValidJwtToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", ValidRegisterRequest());
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(body!.Token).Should().BeTrue();

        var token = handler.ReadJwtToken(body.Token);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
    }

    [Fact]
    public async Task Register_WithAllOptionalFields_ReturnsOk()
    {
        var request = ValidRegisterRequest();
        request.SSN = "123-45-6789";
        request.TaxNumber = "TAX123";
        request.Religion = "None";
        request.DriversLicenseNumber = "DL987654";
        request.InsuranceMemberNumber = "INS001";

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var request = ValidRegisterRequest();

        await _client.PostAsJsonAsync("/auth/register", request);
        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithMissingEmail_ReturnsBadRequest()
    {
        var request = new { FirstName = "John", LastName = "Doe", Password = "Password123!", Gender = "Male", Birthdate = "1990-01-01" };

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        var request = ValidRegisterRequest();
        request.Email = "not-an-email";

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithPasswordTooShort_ReturnsBadRequest()
    {
        var request = ValidRegisterRequest();
        request.Password = "short";

        var response = await _client.PostAsJsonAsync("/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var register = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", register);

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = register.Email,
            Password = register.Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUserInfo()
    {
        var register = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", register);

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = register.Email,
            Password = register.Password
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        body!.Token.Should().NotBeNullOrEmpty();
        body.Email.Should().Be(register.Email);
        body.FirstName.Should().Be(register.FirstName);
        body.LastName.Should().Be(register.LastName);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var register = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", register);

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = register.Email,
            Password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithMissingEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { Password = "Password123!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = "not-an-email",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static RegisterRequest ValidRegisterRequest() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = $"john.{Guid.NewGuid()}@example.com",
        Password = "Password123!",
        Birthdate = new DateTime(1990, 1, 1),
        Gender = "Male"
    };
}
