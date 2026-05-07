using ClinicAppointmentBookingSystem.Models.DTOs.Doctors;
using ClinicAppointmentBookingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>Manages doctors and doctor search.</summary>
[ApiController]
[Route("doctors")]
[Produces("application/json")]
[Consumes("application/json")]
public class DoctorsController(IDoctorService doctorService) : ControllerBase
{
    /// <summary>Returns all doctors.</summary>
    /// <response code="200">List of doctors.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<DoctorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await doctorService.GetAllAsync());

    /// <summary>Returns a single doctor by ID.</summary>
    /// <param name="id">The doctor ID.</param>
    /// <response code="200">The doctor.</response>
    /// <response code="404">Doctor not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await doctorService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Searches for doctors by first or last name.</summary>
    /// <param name="name">The name to search for.</param>
    /// <returns>A list of matching doctors with their clinic and speciality.</returns>
    /// <response code="200">List of matching doctors.</response>
    /// <response code="400">Search term is empty.</response>
    /// <response code="404">No doctors found matching the search term.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<DoctorSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Search([FromQuery] string name)
    {
        try
        {
            var results = await doctorService.SearchAsync(name);
            if (results.Count == 0)
                return NotFound(new { message = $"No doctors found matching '{name}'." });
            return Ok(results);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Creates a new doctor and assigns them to one or more clinics.</summary>
    /// <param name="request">Doctor details including clinic assignments.</param>
    /// <response code="201">Doctor created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Speciality or clinic not found.</response>
    [HttpPost]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateDoctorRequest request)
    {
        try
        {
            var result = await doctorService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Updates a doctor's name and speciality.</summary>
    /// <param name="id">The doctor ID.</param>
    /// <param name="request">Updated doctor details.</param>
    /// <response code="200">Doctor updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Doctor or speciality not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateDoctorRequest request)
    {
        try
        {
            return Ok(await doctorService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Deletes a doctor.</summary>
    /// <param name="id">The doctor ID.</param>
    /// <response code="204">Doctor deleted.</response>
    /// <response code="404">Doctor not found.</response>
    /// <response code="409">Doctor has appointments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await doctorService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
