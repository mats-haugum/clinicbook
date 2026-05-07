using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.AppointmentCategories;

public class CreateAppointmentCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
}
