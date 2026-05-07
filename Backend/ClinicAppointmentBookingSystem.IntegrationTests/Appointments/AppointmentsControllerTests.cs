using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Appointments;

public class AppointmentsControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private string _token = "";

    public async Task InitializeAsync()
    {
        factory.ResetDatabase();
        _token = await RegisterAndGetTokenAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // POST /appointments/book/guest
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BookAsGuest_WithValidData_ReturnsCreatedWithBody()
    {
        var response = await _client.PostAsJsonAsync("/appointments/book/guest", GuestRequest(slot: 1));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        body!.DoctorFullName.Should().Be("James Wilson");
        body.ClinicName.Should().Be("City Medical Center");
        body.CategoryName.Should().Be("General Checkup");
    }

    [Fact]
    public async Task BookAsGuest_EndBeforeStart_ReturnsBadRequest()
    {
        var request = GuestRequest(slot: 2);
        (request.StartTime, request.EndTime) = (request.EndTime, request.StartTime);

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookAsGuest_ConflictingSlotForSameDoctor_ReturnsConflict()
    {
        var first = GuestRequest(slot: 3);
        await _client.PostAsJsonAsync("/appointments/book/guest", first);

        var conflicting = GuestRequest(slot: 3);
        conflicting.Email = "other.guest3@example.com";

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", conflicting);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // POST /appointments/book
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Book_Authenticated_ReturnsCreatedWithBody()
    {
        var req = AuthorizedPost("/appointments/book", BookRequest(slot: 4));
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        body!.ClinicName.Should().Be("City Medical Center");
        body.DoctorFullName.Should().Be("James Wilson");
    }

    [Fact]
    public async Task Book_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/appointments/book", BookRequest(slot: 5));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Book_ConflictingSlot_ReturnsConflict()
    {
        await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 6)));

        var response = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 6)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // GET /appointments/my
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetMyAppointments_Authenticated_ReturnsListWithBookedAppointments()
    {
        await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 7)));

        var req = new HttpRequestMessage(HttpMethod.Get, "/appointments/my");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<AppointmentResponse>>();
        body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyAppointments_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/appointments/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // PUT /appointments/{id}/reschedule
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Reschedule_OwnAppointment_ReturnsOkWithUpdatedTime()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 8)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var newStart = new DateTime(2030, 8, 1, 14, 0, 0);
        var req = new HttpRequestMessage(HttpMethod.Put, $"/appointments/{appointment!.Id}/reschedule");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = JsonContent.Create(new RescheduleAppointmentRequest
        {
            StartTime = newStart,
            EndTime = newStart.AddHours(1)
        });
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        body!.StartTime.Should().BeCloseTo(newStart, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Reschedule_OtherPatientsAppointment_ReturnsForbidden()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 9)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var token2 = await RegisterAndGetTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Put, $"/appointments/{appointment!.Id}/reschedule");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        req.Content = JsonContent.Create(new RescheduleAppointmentRequest
        {
            StartTime = new DateTime(2030, 9, 1, 9, 0, 0),
            EndTime = new DateTime(2030, 9, 1, 10, 0, 0)
        });
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reschedule_NonExistingAppointment_ReturnsNotFound()
    {
        var req = new HttpRequestMessage(HttpMethod.Put, "/appointments/999/reschedule");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = JsonContent.Create(new RescheduleAppointmentRequest
        {
            StartTime = new DateTime(2030, 10, 1, 9, 0, 0),
            EndTime = new DateTime(2030, 10, 1, 10, 0, 0)
        });
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reschedule_ToConflictingSlot_ReturnsConflict()
    {
        // Book two appointments at different slots for the same patient
        var bookedA = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 11)));
        var appointmentA = await bookedA.Content.ReadFromJsonAsync<AppointmentResponse>();
        await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 12)));

        // Rescheduling A to slot 12 conflicts with B (same patient, same doctor, same clinic)
        var req = new HttpRequestMessage(HttpMethod.Put, $"/appointments/{appointmentA!.Id}/reschedule");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = JsonContent.Create(new RescheduleAppointmentRequest
        {
            StartTime = Slot(12).start,
            EndTime = Slot(12).end
        });
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // DELETE /appointments/{id}/cancel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cancel_OwnAppointment_ReturnsNoContent()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 13)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var req = new HttpRequestMessage(HttpMethod.Delete, $"/appointments/{appointment!.Id}/cancel");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_OtherPatientsAppointment_ReturnsForbidden()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 14)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var token2 = await RegisterAndGetTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/appointments/{appointment!.Id}/cancel");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_NonExistingAppointment_ReturnsNotFound()
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, "/appointments/999/cancel");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "Patient",
            Email = $"patient.{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            Birthdate = new DateTime(1990, 1, 1),
            Gender = "Male"
        };
        var response = await _client.PostAsJsonAsync("/auth/register", request);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private HttpRequestMessage AuthorizedPost(string url, object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = JsonContent.Create(body);
        return req;
    }

    private static GuestBookAppointmentRequest GuestRequest(int slot) => new()
    {
        FirstName = "Guest",
        LastName = "User",
        Email = $"guest.{slot}@example.com",
        Birthdate = new DateTime(1990, 1, 1),
        Gender = "Female",
        DoctorId = 1,
        ClinicId = 1,
        CategoryId = 1,
        StartTime = Slot(slot).start,
        EndTime = Slot(slot).end
    };

    private static BookAppointmentRequest BookRequest(int slot) => new()
    {
        DoctorId = 1,
        ClinicId = 1,
        CategoryId = 1,
        StartTime = Slot(slot).start,
        EndTime = Slot(slot).end
    };

    private static (DateTime start, DateTime end) Slot(int n) =>
        (new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(n),
         new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(n + 1));
}
