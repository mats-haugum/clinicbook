namespace ClinicAppointmentBookingSystem.Models.Entities;

public class Speciality : ISoftDeletable
{
    public int Id { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public required string Name { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = [];
}
