namespace ClinicAppointmentBookingSystem.Models.Entities;

// Entities that implement this interface are never hard-deleted from the database.
// Instead, IsDeleted is set to true and DeletedAt records when it happened.
// ClinicBookingDbContext intercepts Remove() calls and converts them to soft deletes automatically.
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
