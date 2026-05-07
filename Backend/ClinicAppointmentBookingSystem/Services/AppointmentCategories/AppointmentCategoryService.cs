using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.AppointmentCategories;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Services;

public class AppointmentCategoryService(ClinicBookingDbContext db) : IAppointmentCategoryService
{
    public async Task<List<AppointmentCategoryResponse>> GetAllAsync() =>
        await db.AppointmentCategories
            .Select(c => new AppointmentCategoryResponse { Id = c.Id, Name = c.Name })
            .ToListAsync();

    public async Task<AppointmentCategoryResponse?> GetByIdAsync(int id) =>
        await db.AppointmentCategories
            .Where(c => c.Id == id)
            .Select(c => new AppointmentCategoryResponse { Id = c.Id, Name = c.Name })
            .FirstOrDefaultAsync();

    public async Task<AppointmentCategoryResponse> CreateAsync(CreateAppointmentCategoryRequest request)
    {
        if (await db.AppointmentCategories.AnyAsync(c => c.Name == request.Name))
            throw new InvalidOperationException($"A category named '{request.Name}' already exists.");

        var category = new Models.Entities.AppointmentCategory { Name = request.Name };
        db.AppointmentCategories.Add(category);
        await db.SaveChangesAsync();

        return new AppointmentCategoryResponse { Id = category.Id, Name = category.Name };
    }

    public async Task<AppointmentCategoryResponse> UpdateAsync(int id, CreateAppointmentCategoryRequest request)
    {
        var category = await db.AppointmentCategories.FindAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        if (await db.AppointmentCategories.AnyAsync(c => c.Name == request.Name && c.Id != id))
            throw new InvalidOperationException($"A category named '{request.Name}' already exists.");

        category.Name = request.Name;
        await db.SaveChangesAsync();

        return new AppointmentCategoryResponse { Id = category.Id, Name = category.Name };
    }

    public async Task DeleteAsync(int id)
    {
        var category = await db.AppointmentCategories.FindAsync(id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        if (await db.Appointments.AnyAsync(a => a.CategoryId == id))
            throw new InvalidOperationException("Cannot delete a category that has appointments assigned to it.");

        db.AppointmentCategories.Remove(category);
        await db.SaveChangesAsync();
    }
}
