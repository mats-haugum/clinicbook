using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Clinics;

public class UpdateClinicRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(300)]
    public required string Address { get; set; }
}
