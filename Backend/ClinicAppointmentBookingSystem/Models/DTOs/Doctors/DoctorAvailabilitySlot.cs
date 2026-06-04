namespace ClinicAppointmentBookingSystem.Models.DTOs.Doctors;

/// <summary>Represents a single 30-minute slot in a doctor's schedule.</summary>
public class DoctorAvailabilitySlot
{
    /// <summary>Start of the slot (wall-clock time, no timezone).</summary>
    public DateTime StartTime { get; set; }

    /// <summary>End of the slot (StartTime + 30 minutes).</summary>
    public DateTime EndTime { get; set; }

    /// <summary>True when no appointment overlaps this slot.</summary>
    public bool IsAvailable { get; set; }
}
