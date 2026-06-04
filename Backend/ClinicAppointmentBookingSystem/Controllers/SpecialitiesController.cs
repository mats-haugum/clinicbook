using ClinicAppointmentBookingSystem.Models.DTOs.Specialities;
using ClinicAppointmentBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>Manages doctor specialities.</summary>
[ApiController]
[Route("specialities")]
[Produces("application/json")]
[Consumes("application/json")]
public class SpecialitiesController(ISpecialityService specialityService) : ControllerBase
{
    /// <summary>Returns all specialities.</summary>
    /// <response code="200">List of specialities.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<SpecialityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await specialityService.GetAllAsync());

    /// <summary>Returns a single speciality by ID.</summary>
    /// <param name="id">The speciality ID.</param>
    /// <response code="200">The speciality.</response>
    /// <response code="404">Speciality not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SpecialityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await specialityService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a new speciality.</summary>
    /// <param name="request">Speciality details.</param>
    /// <response code="201">Speciality created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">A speciality with this name already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SpecialityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateSpecialityRequest request)
    {
        try
        {
            var result = await specialityService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Updates an existing speciality.</summary>
    /// <param name="id">The speciality ID.</param>
    /// <param name="request">Updated speciality details.</param>
    /// <response code="200">Speciality updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Speciality not found.</response>
    /// <response code="409">A speciality with this name already exists.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SpecialityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, CreateSpecialityRequest request)
    {
        try
        {
            return Ok(await specialityService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Deletes a speciality.</summary>
    /// <param name="id">The speciality ID.</param>
    /// <response code="204">Speciality deleted.</response>
    /// <response code="404">Speciality not found.</response>
    /// <response code="409">Speciality has doctors assigned and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await specialityService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
