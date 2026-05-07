namespace ClinicAppointmentBookingSystem.Models.Entities;

public class Doctor
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public int SpecialityId { get; set; }
    public Speciality Speciality { get; set; } = null!;

    public ICollection<DoctorClinic> DoctorClinics { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
