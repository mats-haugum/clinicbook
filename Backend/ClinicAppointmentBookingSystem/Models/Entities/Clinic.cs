namespace ClinicAppointmentBookingSystem.Models.Entities;

public class Clinic
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }

    public ICollection<DoctorClinic> DoctorClinics { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
