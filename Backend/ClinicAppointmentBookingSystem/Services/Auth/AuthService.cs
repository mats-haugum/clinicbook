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
        var patient = new Patient
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Birthdate = request.Birthdate,
            Gender = request.Gender,
            UserType = UserType.Patient,
            PasswordSalt = salt,
            PasswordHash = HashPassword(request.Password, salt),
            SSN = request.SSN,
            TaxNumber = request.TaxNumber,
            Religion = request.Religion,
            DriversLicenseNumber = request.DriversLicenseNumber,
            InsuranceMemberNumber = request.InsuranceMemberNumber
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        return new AuthResponse
        {
            Token = GenerateToken(patient),
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var patient = await db.Patients
            .FirstOrDefaultAsync(p => p.Email == request.Email && p.UserType == UserType.Patient);

        if (patient is null || !VerifyPassword(request.Password, patient.PasswordHash!, patient.PasswordSalt!))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponse
        {
            Token = GenerateToken(patient),
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email
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
            new Claim(JwtRegisteredClaimNames.Sub, patient.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, patient.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, patient.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, patient.LastName)
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
