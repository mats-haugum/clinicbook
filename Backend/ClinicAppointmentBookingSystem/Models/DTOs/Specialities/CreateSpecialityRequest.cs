using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Specialities;

public class CreateSpecialityRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
}
