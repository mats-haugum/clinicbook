using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Admin;
using Konscious.Security.Cryptography;
// Alias avoids the ambiguity between the "Admin" entity and the "Services.Admin" namespace
using AdminEntity = ClinicAppointmentBookingSystem.Models.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClinicAppointmentBookingSystem.Services.Admin;

public class AdminAuthService(ClinicBookingDbContext db, IConfiguration config) : IAdminAuthService
{
    public async Task<AdminAuthResponse> LoginAsync(AdminLoginRequest request)
    {
        var admin = await db.Admins.FirstOrDefaultAsync(a => a.Email == request.Email);

        if (admin is null || !VerifyPassword(request.Password, admin.PasswordHash, admin.PasswordSalt))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AdminAuthResponse
        {
            Token     = GenerateToken(admin),
            FirstName = admin.FirstName,
            LastName  = admin.LastName,
            Email     = admin.Email,
        };
    }

    // Creates a JWT with role: "Admin" — this is what [Authorize(Roles = "Admin")] checks
    private string GenerateToken(AdminEntity admin)
    {
        var jwtSettings  = config.GetSection("JwtSettings");
        var key          = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var credentials  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub,        admin.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,      admin.Email),
            new Claim(JwtRegisteredClaimNames.GivenName,  admin.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, admin.LastName),
            // "role" is the claim name configured in Program.cs via RoleClaimType = "role"
            new Claim("role", "Admin"),
        };

        var token = new JwtSecurityToken(
            issuer:             jwtSettings["Issuer"],
            audience:           jwtSettings["Audience"],
            claims:             claims,
            // Admins get a longer-lived token since there is no refresh mechanism
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Hashes the password using the same Argon2id parameters as patient auth
    public static string HashPassword(string password, byte[] salt)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt                = salt,
            DegreeOfParallelism = 1,
            MemorySize          = 65536,
            Iterations          = 3
        };
        return Convert.ToBase64String(argon2.GetBytes(32));
    }

    public static byte[] GenerateSalt()
    {
        var salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static bool VerifyPassword(string password, string storedHash, byte[] salt)
    {
        var expectedHash = Convert.FromBase64String(storedHash);
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt                = salt,
            DegreeOfParallelism = 1,
            MemorySize          = 65536,
            Iterations          = 3
        };
        var hash = argon2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }
}
