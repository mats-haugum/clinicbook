namespace ClinicAppointmentBookingSystem.Models.Entities;

// Separate table for admin accounts — completely decoupled from the Patient table
// so there is no way for a patient to escalate their own privileges.
public class Admin
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    // Password is stored as an Argon2id hash — same algorithm used for patients
    public required string PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
}
