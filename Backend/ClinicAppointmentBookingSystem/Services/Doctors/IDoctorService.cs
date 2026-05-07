using ClinicAppointmentBookingSystem.Models.DTOs.Doctors;

namespace ClinicAppointmentBookingSystem.Services;

public interface IDoctorService
{
    Task<List<DoctorResponse>> GetAllAsync();
    Task<DoctorResponse?> GetByIdAsync(int id);
    Task<List<DoctorSearchResponse>> SearchAsync(string name);
    Task<DoctorResponse> CreateAsync(CreateDoctorRequest request);
    Task<DoctorResponse> UpdateAsync(int id, UpdateDoctorRequest request);
    Task DeleteAsync(int id);
}
