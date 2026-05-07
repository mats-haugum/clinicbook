using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    [Required]
    public DateTime Birthdate { get; set; }

    [Required]
    public required string Gender { get; set; }

    public string? SSN { get; set; }
    public string? TaxNumber { get; set; }
    public string? Religion { get; set; }
    public string? DriversLicenseNumber { get; set; }
    public string? InsuranceMemberNumber { get; set; }
}
