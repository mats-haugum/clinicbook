using ClinicAppointmentBookingSystem.Models.DTOs.AppointmentCategories;
using ClinicAppointmentBookingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>Manages appointment categories.</summary>
[ApiController]
[Route("appointment-categories")]
[Produces("application/json")]
[Consumes("application/json")]
public class AppointmentCategoriesController(IAppointmentCategoryService categoryService) : ControllerBase
{
    /// <summary>Returns all appointment categories.</summary>
    /// <response code="200">List of categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<AppointmentCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await categoryService.GetAllAsync());

    /// <summary>Returns a single category by ID.</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="200">The category.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AppointmentCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await categoryService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Creates a new appointment category.</summary>
    /// <param name="request">Category details.</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">A category with this name already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateAppointmentCategoryRequest request)
    {
        try
        {
            var result = await categoryService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Updates an existing category.</summary>
    /// <param name="id">The category ID.</param>
    /// <param name="request">Updated category details.</param>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">A category with this name already exists.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AppointmentCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, CreateAppointmentCategoryRequest request)
    {
        try
        {
            return Ok(await categoryService.UpdateAsync(id, request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Deletes a category.</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="204">Category deleted.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has appointments assigned and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await categoryService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
