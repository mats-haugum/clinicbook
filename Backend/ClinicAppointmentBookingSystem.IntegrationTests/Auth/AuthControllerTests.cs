using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task Register_WithValidData_ReturnsRefreshToken()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", ValidRegisterRequest());
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        body!.RefreshToken.Should().NotBeNullOrEmpty();
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
    public async Task Login_WithValidCredentials_ReturnsRefreshToken()
    {
        var register = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", register);

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = register.Email,
            Password = register.Password
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        body!.RefreshToken.Should().NotBeNullOrEmpty();
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
    // Refresh
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOk()
    {
        var tokens = await RegisterAndGetTokensAsync();

        var response = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessAndRefreshTokens()
    {
        var original = await RegisterAndGetTokensAsync();

        var response = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = original.RefreshToken });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // Both new tokens must be present and different from the originals
        body!.Token.Should().NotBeNullOrEmpty().And.NotBe(original.Token);
        body.RefreshToken.Should().NotBeNullOrEmpty().And.NotBe(original.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithValidToken_PreservesUserInfo()
    {
        var register = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/auth/register", register);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest { Email = register.Email, Password = register.Password });
        var original = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = original!.RefreshToken });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // User info must be the same even though the tokens changed
        body!.Email.Should().Be(register.Email);
        body.FirstName.Should().Be(register.FirstName);
        body.LastName.Should().Be(register.LastName);
    }

    [Fact]
    public async Task Refresh_TokenRotation_SecondUseIsRejected()
    {
        // Token rotation means using a refresh token invalidates it immediately.
        // The second call with the same token must be rejected.
        var tokens = await RegisterAndGetTokensAsync();

        await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = tokens.RefreshToken });

        var secondResponse = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = tokens.RefreshToken });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = "this-is-not-a-real-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var tokens = await RegisterAndGetTokensAsync();

        // Directly set ExpiresAt to the past in the database.
        // This simulates a token that has reached its 7-day expiry without
        // needing to wait or use real timers in the test.
        await ExpireRefreshTokenInDbAsync(tokens.RefreshToken);

        var response = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest { RefreshToken = tokens.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithMissingToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/refresh", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Guest account upgrade (register with an email that was used for a guest booking)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_WithGuestEmail_ReturnsOk()
    {
        // Book as a guest to create a guest patient record
        var guestEmail = $"guest.upgrade.{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/appointments/book/guest", GuestBookingRequest(guestEmail));

        // Registering with that same email should succeed (upgrade, not duplicate)
        var response = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            FirstName = "Upgraded",
            LastName  = "User",
            Email     = guestEmail,
            Password  = "Password123!",
            Birthdate = new DateTime(1990, 1, 1),
            Gender    = "Male"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithGuestEmail_CanLoginWithNewPassword()
    {
        var guestEmail = $"guest.login.{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/appointments/book/guest", GuestBookingRequest(guestEmail));

        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            FirstName = "Upgraded", LastName = "User",
            Email = guestEmail, Password = "NewPassword123!",
            Birthdate = new DateTime(1990, 1, 1), Gender = "Male"
        });

        // Logging in with the new password must succeed
        var loginResponse = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest { Email = guestEmail, Password = "NewPassword123!" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithGuestEmail_ExistingAppointmentsTransferToNewAccount()
    {
        // Book as guest — this creates an appointment on the guest patient row
        var guestEmail = $"guest.transfer.{Guid.NewGuid()}@example.com";
        var bookingRes = await _client.PostAsJsonAsync("/appointments/book/guest", GuestBookingRequest(guestEmail));
        var guestAppointment = await bookingRes.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Upgrade the guest to a registered patient
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            FirstName = "Transferred", LastName = "Patient",
            Email = guestEmail, Password = "Password123!",
            Birthdate = new DateTime(1990, 1, 1), Gender = "Male"
        });

        // Login and retrieve appointments — the guest booking must appear
        var loginRes = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest { Email = guestEmail, Password = "Password123!" });
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponse>();

        var myReq = new HttpRequestMessage(HttpMethod.Get, "/appointments/my");
        myReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var myRes = await _client.SendAsync(myReq);

        myRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var appointments = await myRes.Content.ReadFromJsonAsync<List<AppointmentResponse>>();
        appointments!.Should().Contain(a => a.Id == guestAppointment!.Id,
            "the guest booking must be visible once the account is upgraded");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // A minimal valid guest booking request used by the upgrade tests.
    // Doctor 1 exists in the seed data and works at clinic 1.
    private static GuestBookAppointmentRequest GuestBookingRequest(string email) => new()
    {
        FirstName = "Guest",
        LastName  = "User",
        Email     = email,
        Birthdate = new DateTime(1990, 1, 1),
        Gender    = "Female",
        DoctorId  = 1,
        ClinicId  = 1,
        CategoryId = 1,
        StartTime = new DateTime(2031, 3, 1, 9, 0, 0),
        EndTime   = new DateTime(2031, 3, 1, 10, 0, 0)
    };

    private static RegisterRequest ValidRegisterRequest() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = $"john.{Guid.NewGuid()}@example.com",
        Password = "Password123!",
        Birthdate = new DateTime(1990, 1, 1),
        Gender = "Male"
    };

    private async Task<AuthResponse> RegisterAndGetTokensAsync()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", ValidRegisterRequest());
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    // Opens a direct connection to the test database and backdates the refresh token's
    // expiry — simulating a token that has naturally aged past its 7-day window.
    private async Task ExpireRefreshTokenInDbAsync(string refreshToken)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
        var token = await db.RefreshTokens.FirstAsync(rt => rt.Token == refreshToken);
        token.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
    }
}
