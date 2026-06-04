using ClinicAppointmentBookingSystem.Data;
using ClinicAppointmentBookingSystem.Models.DTOs.Clinics;
using ClinicAppointmentBookingSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointmentBookingSystem.Services;

public class ClinicService(ClinicBookingDbContext db) : IClinicService
{
    public async Task<List<ClinicResponse>> GetAllAsync() =>
        await db.Clinics
            .Select(c => new ClinicResponse { Id = c.Id, Name = c.Name, Address = c.Address })
            .ToListAsync();

    public async Task<ClinicResponse?> GetByIdAsync(int id) =>
        await db.Clinics
            .Where(c => c.Id == id)
            .Select(c => new ClinicResponse { Id = c.Id, Name = c.Name, Address = c.Address })
            .FirstOrDefaultAsync();

    public async Task<ClinicResponse> CreateAsync(CreateClinicRequest request)
    {
        if (await db.Clinics.AnyAsync(c => c.Name == request.Name))
            throw new InvalidOperationException($"A clinic named '{request.Name}' already exists.");

        var clinic = new Clinic { Name = request.Name, Address = request.Address };
        db.Clinics.Add(clinic);
        await db.SaveChangesAsync();

        return new ClinicResponse { Id = clinic.Id, Name = clinic.Name, Address = clinic.Address };
    }

    public async Task<ClinicResponse> UpdateAsync(int id, UpdateClinicRequest request)
    {
        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Clinic {id} not found.");

        if (await db.Clinics.AnyAsync(c => c.Name == request.Name && c.Id != id))
            throw new InvalidOperationException($"A clinic named '{request.Name}' already exists.");

        clinic.Name = request.Name;
        clinic.Address = request.Address;
        await db.SaveChangesAsync();

        return new ClinicResponse { Id = clinic.Id, Name = clinic.Name, Address = clinic.Address };
    }

    public async Task DeleteAsync(int id)
    {
        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Clinic {id} not found.");

        if (await db.Appointments.AnyAsync(a => a.ClinicId == id))
            throw new InvalidOperationException("Cannot delete a clinic that has appointments assigned to it.");

        db.Clinics.Remove(clinic);
        await db.SaveChangesAsync();
    }
}
