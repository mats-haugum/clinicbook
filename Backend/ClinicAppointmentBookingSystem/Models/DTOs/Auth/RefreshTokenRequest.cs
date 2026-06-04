using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}
