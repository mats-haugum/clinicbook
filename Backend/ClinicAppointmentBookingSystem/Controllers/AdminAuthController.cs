using ClinicAppointmentBookingSystem.Models.DTOs.Admin;
using ClinicAppointmentBookingSystem.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>
/// Handles admin authentication. Separate from patient auth — admins live in their own table.
/// </summary>
[ApiController]
[Route("admin/auth")]
[Produces("application/json")]
[Consumes("application/json")]
public class AdminAuthController(IAdminAuthService adminAuthService) : ControllerBase
{
    /// <summary>Logs in an admin user.</summary>
    /// <remarks>Returns a JWT with role "Admin". No refresh token is issued — admins must re-authenticate when the token expires.</remarks>
    /// <param name="request">Admin credentials.</param>
    /// <returns>A JWT token along with the admin's basic info.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AdminAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(AdminLoginRequest request)
    {
        try
        {
            var response = await adminAuthService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
