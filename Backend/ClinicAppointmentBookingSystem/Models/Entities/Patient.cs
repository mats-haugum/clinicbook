using ClinicAppointmentBookingSystem.Models.Enums;

namespace ClinicAppointmentBookingSystem.Models.Entities;

public class Patient : ISoftDeletable
{
    public int Id { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public DateTime Birthdate { get; set; }
    public required string Gender { get; set; }
    public UserType UserType { get; set; } = UserType.Guest;

    // Populated only for registered patients
    public string? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public string? SSN { get; set; }
    public string? TaxNumber { get; set; }
    public string? Religion { get; set; }
    public string? DriversLicenseNumber { get; set; }
    public string? InsuranceMemberNumber { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
