namespace ClinicAppointmentBookingSystem.Models.Entities;

public class DoctorClinic
{
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int ClinicId { get; set; }
    public Clinic Clinic { get; set; } = null!;
}