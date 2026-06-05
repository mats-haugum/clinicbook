using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using ClinicAppointmentBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>Manages appointment booking, viewing, rescheduling, and cancellation.</summary>
[ApiController]
[Route("appointments")]
[Produces("application/json")]
[Consumes("application/json")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    /// <summary>Books an appointment as a guest (no login required).</summary>
    /// <remarks>Guest patient information is stored alongside the appointment. Sensitive PII is not collected.</remarks>
    /// <param name="request">Guest patient details and appointment details.</param>
    /// <response code="201">Appointment booked.</response>
    /// <response code="400">Validation failed or end time is before start time.</response>
    /// <response code="404">Doctor, clinic, or category not found.</response>
    /// <response code="409">A conflicting appointment already exists at this time slot.</response>
    [HttpPost("book/guest")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookAsGuest(GuestBookAppointmentRequest request)
    {
        try
        {
            var result = await appointmentService.BookAsGuestAsync(request);
            return CreatedAtAction(nameof(BookAsGuest), result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Books an appointment as a registered patient.</summary>
    /// <param name="request">Appointment details.</param>
    /// <response code="201">Appointment booked.</response>
    /// <response code="400">Validation failed or end time is before start time.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Doctor, clinic, or category not found.</response>
    /// <response code="409">A conflicting appointment already exists at this time slot.</response>
    [HttpPost("book")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(BookAppointmentRequest request)
    {
        try
        {
            var patientId = GetPatientId();
            var result = await appointmentService.BookAsPatientAsync(patientId, request);
            return CreatedAtAction(nameof(Book), result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Returns all appointments for the currently logged-in patient.</summary>
    /// <response code="200">List of appointments.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyAppointments()
    {
        var patientId = GetPatientId();
        return Ok(await appointmentService.GetPatientAppointmentsAsync(patientId));
    }

    /// <summary>Reschedules an existing appointment to a new time slot.</summary>
    /// <param name="id">The appointment ID.</param>
    /// <param name="request">New start and end times.</param>
    /// <response code="200">Appointment rescheduled.</response>
    /// <response code="400">Validation failed or end time is before start time.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">This appointment does not belong to you.</response>
    /// <response code="404">Appointment not found.</response>
    /// <response code="409">A conflicting appointment already exists at the new time slot.</response>
    [HttpPut("{id}/reschedule")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reschedule(int id, RescheduleAppointmentRequest request)
    {
        try
        {
            var patientId = GetPatientId();
            return Ok(await appointmentService.RescheduleAsync(id, patientId, request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Cancels an existing appointment.</summary>
    /// <param name="id">The appointment ID.</param>
    /// <response code="204">Appointment cancelled.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">This appointment does not belong to you.</response>
    /// <response code="404">Appointment not found.</response>
    [HttpDelete("{id}/cancel")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var patientId = GetPatientId();
            await appointmentService.CancelAsync(id, patientId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    private int GetPatientId() =>
        int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
