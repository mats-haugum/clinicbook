using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using ClinicAppointmentBookingSystem.Models.DTOs.Doctors;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Doctors;

public class DoctorsControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        factory.ResetDatabase();
        var response = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            FirstName = "Test", LastName = "User",
            Email = $"test.{Guid.NewGuid()}@example.com",
            Password = "Password123!",
            Birthdate = new DateTime(1990, 1, 1),
            Gender = "Male"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /doctors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsOkWithSeededDoctors()
    {
        var response = await _client.GetAsync("/doctors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DoctorResponse>>();
        body.Should().Contain(d => d.FirstName == "James" && d.LastName == "Wilson");
        body.Should().Contain(d => d.FirstName == "Sarah" && d.LastName == "Connor");
        body.Should().Contain(d => d.FirstName == "Emily" && d.LastName == "Chen");
        body.Should().Contain(d => d.FirstName == "Michael" && d.LastName == "Brown");
    }

    // -------------------------------------------------------------------------
    // GET /doctors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingId_ReturnsDoctorWithSpecialityAndClinics()
    {
        // Doctor 1 (James Wilson) is assigned to both clinics
        var response = await _client.GetAsync("/doctors/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        body!.FirstName.Should().Be("James");
        body.LastName.Should().Be("Wilson");
        body.SpecialityName.Should().Be("General Practice");
        body.ClinicNames.Should().Contain("City Medical Center");
        body.ClinicNames.Should().Contain("Westside Health Clinic");
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/doctors/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GET /doctors/search?name=
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Search_ByFirstName_ReturnsMatchingResults()
    {
        var response = await _client.GetAsync("/doctors/search?name=James");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DoctorSearchResponse>>();
        body.Should().Contain(r => r.FullName == "James Wilson");
    }

    [Fact]
    public async Task Search_ByLastName_ReturnsMatchingResults()
    {
        var response = await _client.GetAsync("/doctors/search?name=Connor");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DoctorSearchResponse>>();
        body.Should().Contain(r => r.FullName == "Sarah Connor");
    }

    [Fact]
    public async Task Search_ReturnsClinicAndSpecialityInfo()
    {
        var response = await _client.GetAsync("/doctors/search?name=Wilson");
        var body = await response.Content.ReadFromJsonAsync<List<DoctorSearchResponse>>();

        // Doctor 1 is at two clinics so there are two results
        body!.Should().AllSatisfy(r => r.Speciality.Should().Be("General Practice"));
        body.Should().Contain(r => r.ClinicName == "City Medical Center");
        body.Should().Contain(r => r.ClinicName == "Westside Health Clinic");
    }

    [Fact]
    public async Task Search_WithNoMatch_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/doctors/search?name=Nobody");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WithEmptyName_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/doctors/search?name=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // POST /doctors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedWithClinicAndSpeciality()
    {
        var response = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "Anna",
            LastName = "Smith",
            SpecialityId = 2,
            ClinicIds = [1]
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        body!.FirstName.Should().Be("Anna");
        body.SpecialityName.Should().Be("Cardiology");
        body.ClinicNames.Should().Contain("City Medical Center");
    }

    [Fact]
    public async Task Create_WithInvalidSpecialityId_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "Test",
            LastName = "Doctor",
            SpecialityId = 999,
            ClinicIds = [1]
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithInvalidClinicId_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "Test",
            LastName = "Doctor",
            SpecialityId = 1,
            ClinicIds = [999]
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // PUT /doctors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingDoctor_ReturnsUpdatedBody()
    {
        var response = await _client.PutAsJsonAsync("/doctors/4", new UpdateDoctorRequest
        {
            FirstName = "Michael",
            LastName = "Brown-Updated",
            SpecialityId = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        body!.LastName.Should().Be("Brown-Updated");
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/doctors/999", new UpdateDoctorRequest
        {
            FirstName = "Ghost",
            LastName = "Doctor",
            SpecialityId = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /doctors/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_WithNoDependencies_ReturnsNoContent()
    {
        var created = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "To",
            LastName = "Delete",
            SpecialityId = 1,
            ClinicIds = [1]
        });
        var doctor = await created.Content.ReadFromJsonAsync<DoctorResponse>();

        var response = await _client.DeleteAsync($"/doctors/{doctor!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAppointmentsAssigned_ReturnsConflict()
    {
        // Doctor 3 (Emily Chen) has no appointments by default; book one first
        await _client.PostAsJsonAsync("/appointments/book/guest", new GuestBookAppointmentRequest
        {
            FirstName = "Guest",
            LastName = "User",
            Email = "guest.doctordelete@example.com",
            Birthdate = new DateTime(1990, 1, 1),
            Gender = "Male",
            DoctorId = 3,
            ClinicId = 2,
            CategoryId = 1,
            StartTime = new DateTime(2030, 5, 1, 9, 0, 0),
            EndTime = new DateTime(2030, 5, 1, 10, 0, 0)
        });

        var response = await _client.DeleteAsync("/doctors/3");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/doctors/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Soft delete verification
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_SoftDeletes_DoctorNoLongerInGetAll()
    {
        var created = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "Soft", LastName = "DeletedDoc", SpecialityId = 1, ClinicIds = [1]
        });
        var doctor = await created.Content.ReadFromJsonAsync<DoctorResponse>();
        await _client.DeleteAsync($"/doctors/{doctor!.Id}");

        var body = await (await _client.GetAsync("/doctors"))
            .Content.ReadFromJsonAsync<List<DoctorResponse>>();

        body!.Should().NotContain(d => d.Id == doctor.Id);
    }

    [Fact]
    public async Task Delete_SoftDeletes_DoctorGetByIdReturnsNotFound()
    {
        var created = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "Soft", LastName = "DeletedDoc2", SpecialityId = 1, ClinicIds = [1]
        });
        var doctor = await created.Content.ReadFromJsonAsync<DoctorResponse>();
        await _client.DeleteAsync($"/doctors/{doctor!.Id}");

        var response = await _client.GetAsync($"/doctors/{doctor.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SoftDeletes_DoctorNoLongerInSearch()
    {
        var created = await _client.PostAsJsonAsync("/doctors", new CreateDoctorRequest
        {
            FirstName = "Vanishing", LastName = "DoctorSearch", SpecialityId = 1, ClinicIds = [1]
        });
        var doctor = await created.Content.ReadFromJsonAsync<DoctorResponse>();
        await _client.DeleteAsync($"/doctors/{doctor!.Id}");

        var response = await _client.GetAsync("/doctors/search?name=Vanishing");

        // Soft-deleted doctor must not appear in search results
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
