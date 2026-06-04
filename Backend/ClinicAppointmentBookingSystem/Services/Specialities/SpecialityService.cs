using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Specialities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Services;

public class SpecialityService(ClinicBookingDbContext db) : ISpecialityService
{
    public async Task<List<SpecialityResponse>> GetAllAsync() =>
        await db.Specialities
            .Select(s => new SpecialityResponse { Id = s.Id, Name = s.Name })
            .ToListAsync();

    public async Task<SpecialityResponse?> GetByIdAsync(int id) =>
        await db.Specialities
            .Where(s => s.Id == id)
            .Select(s => new SpecialityResponse { Id = s.Id, Name = s.Name })
            .FirstOrDefaultAsync();

    public async Task<SpecialityResponse> CreateAsync(CreateSpecialityRequest request)
    {
        if (await db.Specialities.AnyAsync(s => s.Name == request.Name))
            throw new InvalidOperationException($"A speciality named '{request.Name}' already exists.");

        var speciality = new Models.Entities.Speciality { Name = request.Name };
        db.Specialities.Add(speciality);
        await db.SaveChangesAsync();

        return new SpecialityResponse { Id = speciality.Id, Name = speciality.Name };
    }

    public async Task<SpecialityResponse> UpdateAsync(int id, CreateSpecialityRequest request)
    {
        var speciality = await db.Specialities.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Speciality {id} not found.");

        if (await db.Specialities.AnyAsync(s => s.Name == request.Name && s.Id != id))
            throw new InvalidOperationException($"A speciality named '{request.Name}' already exists.");

        speciality.Name = request.Name;
        await db.SaveChangesAsync();

        return new SpecialityResponse { Id = speciality.Id, Name = speciality.Name };
    }

    public async Task DeleteAsync(int id)
    {
        var speciality = await db.Specialities.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Speciality {id} not found.");

        if (await db.Doctors.AnyAsync(d => d.SpecialityId == id))
            throw new InvalidOperationException("Cannot delete a speciality that has doctors assigned to it.");

        db.Specialities.Remove(speciality);
        await db.SaveChangesAsync();
    }
}
