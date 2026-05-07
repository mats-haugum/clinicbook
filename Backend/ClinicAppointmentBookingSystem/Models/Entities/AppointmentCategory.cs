namespace ClinicAppointmentBookingSystem.Models.Entities;

public class AppointmentCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
}
