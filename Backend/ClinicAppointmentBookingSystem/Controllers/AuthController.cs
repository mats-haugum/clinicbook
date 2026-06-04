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
    /// Issues a new access token and refresh token using a valid refresh token.
    /// </summary>
    /// <remarks>The old refresh token is revoked immediately (token rotation).</remarks>
    /// <param name="request">The refresh token.</param>
    /// <response code="200">New tokens issued.</response>
    /// <response code="401">Refresh token is invalid, expired, or revoked.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        try
        {
            var response = await authService.RefreshAsync(request.RefreshToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns the non-sensitive PII stored for a guest booking, used to pre-fill the registration form.
    /// </summary>
    /// <remarks>
    /// Only returns data for UserType.Guest rows — registered patients are not exposed here.
    /// The frontend calls this on the registration page when the user already has a guest booking.
    /// </remarks>
    /// <param name="email">The email address used when the guest booked their appointment.</param>
    /// <returns>First name, last name, email, birthdate, and gender from the guest record.</returns>
    /// <response code="200">Guest record found, pre-fill data returned.</response>
    /// <response code="404">No guest booking exists for this email address.</response>
    [HttpGet("guest-prefill")]
    [ProducesResponseType(typeof(GuestPrefillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuestPrefill([FromQuery] string email)
    {
        try
        {
            var response = await authService.GetGuestPrefillAsync(email);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
