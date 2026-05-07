using ClinicAppointmentBookingSystem.Models.DTOs.Clinics;

namespace ClinicAppointmentBookingSystem.Services;

public interface IClinicService
{
    Task<List<ClinicResponse>> GetAllAsync();
    Task<ClinicResponse?> GetByIdAsync(int id);
    Task<ClinicResponse> CreateAsync(CreateClinicRequest request);
    Task<ClinicResponse> UpdateAsync(int id, UpdateClinicRequest request);
    Task DeleteAsync(int id);
}
