using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Appointments;

public class RescheduleAppointmentRequest
{
    // Optional — if omitted the existing doctor/clinic/category is kept
    public int? DoctorId { get; set; }
    public int? ClinicId { get; set; }
    public int? CategoryId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}
