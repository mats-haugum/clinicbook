using ClinicAppointmentBookingSystem.Models.DTOs.Clinics;
using ClinicAppointmentBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>Manages clinics.</summary>
[ApiController]
[Route("clinics")]
[Produces("application/json")]
[Consumes("application/json")]
public class ClinicsController(IClinicService clinicService) : ControllerBase
{
    /// <summary>Returns all clinics.</summary>
    /// <response code="200">List of clinics.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ClinicResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await clinicService.GetAllAsync());

    /// <summary>Returns a single clinic by ID.</summary>
    /// <param name="id">The clinic ID.</param>
    /// <response code="200">The clinic.</response>
    /// <response code="404">Clinic not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await clinicService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a new clinic.</summary>
    /// <param name="request">Clinic details.</param>
    /// <response code="201">Clinic created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">A clinic with this name already exists.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateClinicRequest request)
    {
        try
        {
            var result = await clinicService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Updates an existing clinic.</summary>
    /// <param name="id">The clinic ID.</param>
    /// <param name="request">Updated clinic details.</param>
    /// <response code="200">Clinic updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Clinic not found.</response>
    /// <response code="409">A clinic with this name already exists.</response>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateClinicRequest request)
    {
        try
        {
            return Ok(await clinicService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Deletes a clinic.</summary>
    /// <param name="id">The clinic ID.</param>
    /// <response code="204">Clinic deleted.</response>
    /// <response code="404">Clinic not found.</response>
    /// <response code="409">Clinic has appointments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await clinicService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
