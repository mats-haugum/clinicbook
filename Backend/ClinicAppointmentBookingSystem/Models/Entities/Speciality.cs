namespace ClinicAppointmentBookingSystem.Models.Entities;

public class Speciality
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = [];
}
