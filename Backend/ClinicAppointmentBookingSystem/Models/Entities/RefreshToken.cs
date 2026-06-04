namespace ClinicAppointmentBookingSystem.Models.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    // The actual token value — a cryptographically random string, not a JWT
    public required string Token { get; set; }

    // Foreign key linking this token to the patient who owns it
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    // Set to true on logout or when a new refresh token is issued (token rotation)
    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
