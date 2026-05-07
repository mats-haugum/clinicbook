using ClinicAppointmentBookingSystem.Models.DTOs.AppointmentCategories;

namespace ClinicAppointmentBookingSystem.Services;

public interface IAppointmentCategoryService
{
    Task<List<AppointmentCategoryResponse>> GetAllAsync();
    Task<AppointmentCategoryResponse?> GetByIdAsync(int id);
    Task<AppointmentCategoryResponse> CreateAsync(CreateAppointmentCategoryRequest request);
    Task<AppointmentCategoryResponse> UpdateAsync(int id, CreateAppointmentCategoryRequest request);
    Task DeleteAsync(int id);
}
