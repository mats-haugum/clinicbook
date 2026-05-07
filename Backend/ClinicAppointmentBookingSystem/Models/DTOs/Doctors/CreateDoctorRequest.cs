using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Doctors;

public class CreateDoctorRequest
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    public int SpecialityId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one clinic must be assigned.")]
    public List<int> ClinicIds { get; set; } = [];
}
