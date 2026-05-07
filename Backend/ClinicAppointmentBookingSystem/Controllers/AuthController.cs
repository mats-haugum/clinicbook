using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using ClinicAppointmentBookingSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAppointmentBookingSystem.Controllers;

/// <summary>
/// Handles patient registration and authentication.
/// </summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
[Consumes("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Registers a new patient account.
    /// </summary>
    /// <remarks>
    /// Creates a registered patient with full PII storage and returns a JWT token.
    /// Guest users only need to supply the required fields; sensitive fields (SSN, TaxNumber, etc.) are optional.
    /// </remarks>
    /// <param name="request">Patient registration details.</param>
    /// <returns>A JWT token along with the patient's basic info.</returns>
    /// <response code="200">Registration successful. Returns a JWT token.</response>
    /// <response code="400">Validation failed — missing or invalid fields.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var response = await authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Logs in an existing patient.
    /// </summary>
    /// <remarks>
    /// Verifies the email and password, and returns a JWT token to be used in the Authorization header for protected endpoints.
    /// </remarks>
    /// <param name="request">Login credentials.</param>
    /// <returns>A JWT token along with the patient's basic info.</returns>
    /// <response code="200">Login successful. Returns a JWT token.</response>
    /// <response code="400">Validation failed — missing or invalid fields.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var response = await authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
