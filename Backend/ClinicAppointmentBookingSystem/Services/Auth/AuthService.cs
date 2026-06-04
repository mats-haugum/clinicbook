using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using ClinicAppointmentBookingSystem.Models.Entities;
using ClinicAppointmentBookingSystem.Models.Enums;
using Konscious.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClinicAppointmentBookingSystem.Services;

public class AuthService(ClinicBookingDbContext db, IConfiguration config) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await db.Patients.AnyAsync(p => p.Email == request.Email && p.UserType == UserType.Patient))
            throw new InvalidOperationException("An account with this email already exists.");

        var salt = GenerateSalt();

        // If a guest account exists with this email, upgrade it in-place rather than
        // creating a second row. The PatientId stays the same, so every appointment
        // the guest already booked is automatically owned by the new registered account.
        var existing = await db.Patients.FirstOrDefaultAsync(p =>
            p.Email == request.Email && p.UserType == UserType.Guest);

        Patient patient;
        if (existing is not null)
        {
            existing.UserType             = UserType.Patient;
            existing.FirstName            = request.FirstName;
            existing.LastName             = request.LastName;
            existing.Birthdate            = request.Birthdate;
            existing.Gender               = request.Gender;
            existing.PasswordSalt         = salt;
            existing.PasswordHash         = HashPassword(request.Password, salt);
            existing.SSN                  = request.SSN;
            existing.TaxNumber            = request.TaxNumber;
            existing.Religion             = request.Religion;
            existing.DriversLicenseNumber = request.DriversLicenseNumber;
            existing.InsuranceMemberNumber = request.InsuranceMemberNumber;
            patient = existing;
        }
        else
        {
            patient = new Patient
            {
                FirstName             = request.FirstName,
                LastName              = request.LastName,
                Email                 = request.Email,
                Birthdate             = request.Birthdate,
                Gender                = request.Gender,
                UserType              = UserType.Patient,
                PasswordSalt          = salt,
                PasswordHash          = HashPassword(request.Password, salt),
                SSN                   = request.SSN,
                TaxNumber             = request.TaxNumber,
                Religion              = request.Religion,
                DriversLicenseNumber  = request.DriversLicenseNumber,
                InsuranceMemberNumber = request.InsuranceMemberNumber
            };
            db.Patients.Add(patient);
        }

        await db.SaveChangesAsync();
        return await BuildAuthResponseAsync(patient);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var patient = await db.Patients
            .FirstOrDefaultAsync(p => p.Email == request.Email && p.UserType == UserType.Patient);

        if (patient is null || !VerifyPassword(request.Password, patient.PasswordHash!, patient.PasswordSalt!))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await BuildAuthResponseAsync(patient);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        // Find the token in the database and load the associated patient
        var stored = await db.RefreshTokens
            .Include(rt => rt.Patient)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        // Reject if the token does not exist, the patient was soft-deleted, is expired, or it was revoked
        if (stored is null || stored.Patient is null || stored.ExpiresAt < DateTime.UtcNow || stored.IsRevoked)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Token rotation: revoke the used token so it cannot be reused
        stored.IsRevoked = true;

        var response = await BuildAuthResponseAsync(stored.Patient);
        await db.SaveChangesAsync();
        return response;
    }

    public async Task<GuestPrefillResponse> GetGuestPrefillAsync(string email)
    {
        var guest = await db.Patients
            .Where(p => p.Email == email && p.UserType == UserType.Guest)
            .Select(p => new GuestPrefillResponse
            {
                FirstName = p.FirstName,
                LastName  = p.LastName,
                Email     = p.Email,
                Birthdate = p.Birthdate,
                Gender    = p.Gender
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("No guest booking found for this email address.");

        return guest;
    }

    // Creates an access token + refresh token, saves the refresh token to the DB,
    // and returns the full AuthResponse. Used by register, login, and refresh.
    private async Task<AuthResponse> BuildAuthResponseAsync(Patient patient)
    {
        var refreshToken = new RefreshToken
        {
            // RandomNumberGenerator.GetBytes produces a cryptographically secure random value
            Token     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            PatientId = patient.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return new AuthResponse
        {
            Token        = GenerateToken(patient),
            RefreshToken = refreshToken.Token,
            FirstName    = patient.FirstName,
            LastName     = patient.LastName,
            Email        = patient.Email
        };
    }

    private static byte[] GenerateSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static string HashPassword(string password, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 1,
            MemorySize = 65536,
            Iterations = 3
        };

        return Convert.ToBase64String(argon2.GetBytes(32));
    }

    private static bool VerifyPassword(string password, string storedHash, byte[] salt)
    {
        var expectedHash = Convert.FromBase64String(storedHash);

        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 1,
            MemorySize = 65536,
            Iterations = 3
        };

        var hash = argon2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }

    private string GenerateToken(Patient patient)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // jti (JWT ID) is a unique identifier for this specific token instance.
            // Without it, two tokens for the same patient issued in the same second would be identical.
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, patient.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, patient.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, patient.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, patient.LastName),
            // "role" matches the RoleClaimType configured in Program.cs — used by [Authorize(Roles = "...")]
            new Claim("role", "Patient"),
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
