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
    // Guest email uniqueness
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BookAsGuest_WithRegisteredPatientEmail_ReturnsConflict()
    {
        // Register a patient so their email exists as a registered account
        var registerRes = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            FirstName = "Registered", LastName = "Patient",
            Email = "patient.taken@example.com",
            Password = "Password123!",
            Birthdate = new DateTime(1990, 1, 1),
            Gender = "Male"
        });
        registerRes.EnsureSuccessStatusCode();

        // A guest trying to book with that same email must be rejected
        var request = GuestRequest(slot: 70);
        request.Email = "patient.taken@example.com";

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task BookAsGuest_ReturningGuestWithSameEmail_ReturnsCreated()
    {
        // First booking — creates the guest patient record
        var first = GuestRequest(slot: 71);
        first.Email = "returning.guest@example.com";
        await _client.PostAsJsonAsync("/appointments/book/guest", first);

        // Second booking with the same email at a different slot — must reuse the record
        var second = GuestRequest(slot: 72);
        second.Email = "returning.guest@example.com";
        var response = await _client.PostAsJsonAsync("/appointments/book/guest", second);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // -------------------------------------------------------------------------
    // Working-hours validation (guest booking)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task BookAsGuest_StartBeforeWorkdayStart_ReturnsBadRequest()
    {
        var request = GuestRequest(slot: 50);
        // Override to 07:30 — one half-hour before the 08:00 open
        request.StartTime = new DateTime(2030, 6, 1, 7, 30, 0);
        request.EndTime   = new DateTime(2030, 6, 1, 8, 0, 0);

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookAsGuest_EndAfterWorkdayEnd_ReturnsBadRequest()
    {
        var request = GuestRequest(slot: 51);
        // Override to 16:30–17:30 — end goes past the 17:00 close
        request.StartTime = new DateTime(2030, 6, 1, 16, 30, 0);
        request.EndTime   = new DateTime(2030, 6, 1, 17, 30, 0);

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookAsGuest_SpansMultipleDays_ReturnsBadRequest()
    {
        var request = GuestRequest(slot: 52);
        request.StartTime = new DateTime(2030, 6, 1, 16, 0, 0);
        request.EndTime   = new DateTime(2030, 6, 2, 9, 0, 0);

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookAsGuest_ExactlyAtBoundaryTimes_ReturnsCreated()
    {
        // 08:00–17:00 is the inclusive boundary — this must succeed
        var request = GuestRequest(slot: 53);
        request.StartTime = new DateTime(2030, 6, 1, 8, 0, 0);
        request.EndTime   = new DateTime(2030, 6, 1, 17, 0, 0);

        var response = await _client.PostAsJsonAsync("/appointments/book/guest", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
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

    [Fact]
    public async Task GetMyAppointments_WithNoBookings_ReturnsEmptyList()
    {
        // Register a fresh patient who has not booked any appointments
        var freshToken = await RegisterAndGetTokenAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "/appointments/my");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<AppointmentResponse>>();
        body.Should().BeEmpty();
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
    // Working-hours validation (authenticated booking + reschedule)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Book_StartBeforeWorkdayStart_ReturnsBadRequest()
    {
        var req = AuthorizedPost("/appointments/book", new BookAppointmentRequest
        {
            DoctorId = 1, ClinicId = 1, CategoryId = 1,
            StartTime = new DateTime(2030, 7, 1, 7, 0, 0),
            EndTime   = new DateTime(2030, 7, 1, 7, 30, 0)
        });

        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Book_EndAfterWorkdayEnd_ReturnsBadRequest()
    {
        var req = AuthorizedPost("/appointments/book", new BookAppointmentRequest
        {
            DoctorId = 1, ClinicId = 1, CategoryId = 1,
            StartTime = new DateTime(2030, 7, 1, 16, 30, 0),
            EndTime   = new DateTime(2030, 7, 1, 17, 30, 0)
        });

        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reschedule_ToOutsideWorkingHours_ReturnsBadRequest()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 60)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var req = new HttpRequestMessage(HttpMethod.Put, $"/appointments/{appointment!.Id}/reschedule");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = JsonContent.Create(new RescheduleAppointmentRequest
        {
            StartTime = new DateTime(2030, 8, 1, 6, 0, 0),
            EndTime   = new DateTime(2030, 8, 1, 6, 30, 0)
        });

        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PUT /appointments/{id}/reschedule — change doctor / clinic / category
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Reschedule_WithNewDoctor_UpdatesDoctorInResponse()
    {
        // Book with doctor 1 (James Wilson) at clinic 1
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 30)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Reschedule to a new slot and switch to doctor 4 (Michael Brown, also at clinic 1)
        var req = RescheduleRequest(appointment!.Id, slot: 31, doctorId: 4);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        body!.DoctorFullName.Should().Be("Michael Brown");
        body.DoctorId.Should().Be(4);
    }

    [Fact]
    public async Task Reschedule_WithNewClinic_UpdatesClinicInResponse()
    {
        // Book with doctor 1 at clinic 1 — doctor 1 (James Wilson) is also assigned to clinic 2
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 32)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Reschedule to the same slot but at clinic 2
        var req = RescheduleRequest(appointment!.Id, slot: 33, clinicId: 2);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        body!.ClinicName.Should().Be("Westside Health Clinic");
        body.ClinicId.Should().Be(2);
    }

    [Fact]
    public async Task Reschedule_WithNewCategory_UpdatesCategoryInResponse()
    {
        // Book with category 1 (General Checkup)
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 34)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Reschedule and change the category to 2 (Follow-up)
        var req = RescheduleRequest(appointment!.Id, slot: 35, categoryId: 2);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        body!.CategoryName.Should().Be("Follow-up");
        body.CategoryId.Should().Be(2);
    }

    [Fact]
    public async Task Reschedule_WithInvalidDoctorId_ReturnsNotFound()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 36)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var req = RescheduleRequest(appointment!.Id, slot: 37, doctorId: 999);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reschedule_WithInvalidClinicId_ReturnsNotFound()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 38)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var req = RescheduleRequest(appointment!.Id, slot: 39, clinicId: 999);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reschedule_WithInvalidCategoryId_ReturnsNotFound()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 40)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        var req = RescheduleRequest(appointment!.Id, slot: 41, categoryId: 999);
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reschedule_ToNewDoctor_WhoIsAlreadyBooked_ReturnsConflict()
    {
        // First book doctor 4 (Michael Brown) at slot 42 using a guest, so that slot is taken
        await _client.PostAsJsonAsync("/appointments/book/guest", new GuestBookAppointmentRequest
        {
            FirstName = "Guest", LastName = "Blocker", Email = "blocker.doctor4@example.com",
            Birthdate = new DateTime(1990, 1, 1), Gender = "Male",
            DoctorId = 4, ClinicId = 1, CategoryId = 1,
            StartTime = Slot(42).start, EndTime = Slot(42).end
        });

        // Now book doctor 1 at slot 43 as the registered patient
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 43)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Try to reschedule to slot 42 and switch to doctor 4 — doctor 4 is already booked there
        var req = RescheduleRequest(appointment!.Id, slot: 42, doctorId: 4);
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
    // Soft delete verification
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cancel_SoftDeletes_AppointmentNoLongerInMyList()
    {
        var booked = await _client.SendAsync(AuthorizedPost("/appointments/book", BookRequest(slot: 15)));
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentResponse>();

        // Cancel the appointment
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, $"/appointments/{appointment!.Id}/cancel");
        cancelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        await _client.SendAsync(cancelReq);

        // The appointment must not appear in the patient's list — the global query filter hides it
        var listReq = new HttpRequestMessage(HttpMethod.Get, "/appointments/my");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        var listResponse = await _client.SendAsync(listReq);
        var appointments = await listResponse.Content.ReadFromJsonAsync<List<AppointmentResponse>>();

        appointments!.Should().NotContain(a => a.Id == appointment.Id);
    }

    // -------------------------------------------------------------------------
    // Cross-clinic conflict validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Book_ConflictingSlot_AtDifferentClinic_ReturnsConflict()
    {
        // Book the patient at clinic 1
        await _client.SendAsync(AuthorizedPost("/appointments/book", new BookAppointmentRequest
        {
            DoctorId = 1, ClinicId = 1, CategoryId = 1,
            StartTime = Slot(20).start, EndTime = Slot(20).end
        }));

        // Same patient, overlapping time — but at clinic 2.
        var response = await _client.SendAsync(AuthorizedPost("/appointments/book", new BookAppointmentRequest
        {
            DoctorId = 1, ClinicId = 2, CategoryId = 1,
            StartTime = Slot(20).start, EndTime = Slot(20).end
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Book_DoctorAlreadyBooked_AtDifferentClinic_ReturnsConflict()
    {
        // Patient 1 books the doctor at clinic 1
        await _client.SendAsync(AuthorizedPost("/appointments/book", new BookAppointmentRequest
        {
            DoctorId = 1, ClinicId = 1, CategoryId = 1,
            StartTime = Slot(21).start, EndTime = Slot(21).end
        }));

        // A completely different patient tries to book the same doctor at clinic 2
        // at the same time — the doctor is physically only one person.
        var token2 = await RegisterAndGetTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, "/appointments/book");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        req.Content = JsonContent.Create(new BookAppointmentRequest
        {
            DoctorId = 1, ClinicId = 2, CategoryId = 1,
            StartTime = Slot(21).start, EndTime = Slot(21).end
        });
        var response = await _client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

    // Builds an authorized PUT /appointments/{id}/reschedule request.
    // doctorId, clinicId, categoryId are optional — omitting them leaves those fields unchanged.
    private HttpRequestMessage RescheduleRequest(
        int appointmentId, int slot,
        int? doctorId = null, int? clinicId = null, int? categoryId = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"/appointments/{appointmentId}/reschedule");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Content = JsonContent.Create(new RescheduleAppointmentRequest
        {
            DoctorId   = doctorId,
            ClinicId   = clinicId,
            CategoryId = categoryId,
            StartTime  = Slot(slot).start,
            EndTime    = Slot(slot).end
        });
        return req;
    }

    // Each slot is a unique day at 09:00–10:00 so all slots are within working
    // hours. Conflicts still work because two bookings with the same slot number
    // land on the same day and time.
    private static (DateTime start, DateTime end) Slot(int n) =>
        (new DateTime(2030, 1, 1, 9, 0, 0).AddDays(n),
         new DateTime(2030, 1, 1, 10, 0, 0).AddDays(n));
}
