using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Admin;

public class AdminLoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
