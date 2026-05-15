using System.ComponentModel.DataAnnotations;

namespace ClinicAppointmentBookingSystem.Models.DTOs.Appointments;

public class BookAppointmentRequest
{
    [Required]
    public int DoctorId { get; set; }

    [Required]
    public int ClinicId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }
}
