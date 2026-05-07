using ClinicAppointmentBookingSystem.Models.DTOs.Specialities;

namespace ClinicAppointmentBookingSystem.Services;

public interface ISpecialityService
{
    Task<List<SpecialityResponse>> GetAllAsync();
    Task<SpecialityResponse?> GetByIdAsync(int id);
    Task<SpecialityResponse> CreateAsync(CreateSpecialityRequest request);
    Task<SpecialityResponse> UpdateAsync(int id, CreateSpecialityRequest request);
    Task DeleteAsync(int id);
}
