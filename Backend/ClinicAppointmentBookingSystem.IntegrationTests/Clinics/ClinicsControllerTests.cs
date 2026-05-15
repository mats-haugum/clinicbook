using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using ClinicAppointmentBookingSystem.Models.DTOs.Auth;
using ClinicAppointmentBookingSystem.Models.DTOs.Clinics;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Clinics;

public class ClinicsControllerTests(CustomWebApplicationFactory factory)
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
    // GET /clinics
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsOkWithSeededClinics()
    {
        var response = await _client.GetAsync("/clinics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ClinicResponse>>();
        body.Should().Contain(c => c.Name == "City Medical Center");
        body.Should().Contain(c => c.Name == "Westside Health Clinic");
    }

    // -------------------------------------------------------------------------
    // GET /clinics/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingId_ReturnsClinic()
    {
        var response = await _client.GetAsync("/clinics/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClinicResponse>();
        body!.Name.Should().Be("City Medical Center");
        body.Address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/clinics/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /clinics
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedWithBody()
    {
        var response = await _client.PostAsJsonAsync("/clinics", new CreateClinicRequest
        {
            Name = "Northside Clinic",
            Address = "789 Pine Road, Durban"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ClinicResponse>();
        body!.Id.Should().BeGreaterThan(0);
        body.Name.Should().Be("Northside Clinic");
        body.Address.Should().Be("789 Pine Road, Durban");
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/clinics", new CreateClinicRequest { Name = "DuplicateClinic", Address = "1 Test St" });
        var response = await _client.PostAsJsonAsync("/clinics", new CreateClinicRequest { Name = "DuplicateClinic", Address = "2 Test St" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /clinics/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingClinic_ReturnsUpdatedBody()
    {
        var response = await _client.PutAsJsonAsync("/clinics/2", new UpdateClinicRequest
        {
            Name = "Westside Health Clinic Updated",
            Address = "456 Oak Avenue Updated, Johannesburg"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClinicResponse>();
        body!.Name.Should().Be("Westside Health Clinic Updated");
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/clinics/999", new UpdateClinicRequest
        {
            Name = "Ghost Clinic",
            Address = "Nowhere"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /clinics/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_WithNoDependencies_ReturnsNoContent()
    {
        var created = await _client.PostAsJsonAsync("/clinics", new CreateClinicRequest
        {
            Name = "ToDeleteClinic",
            Address = "1 Delete Street"
        });
        var clinic = await created.Content.ReadFromJsonAsync<ClinicResponse>();

        var response = await _client.DeleteAsync($"/clinics/{clinic!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAppointmentsAssigned_ReturnsConflict()
    {
        // Create a dedicated clinic so we don't disturb seeded data used by other tests
        var clinicRes = await _client.PostAsJsonAsync("/clinics", new CreateClinicRequest
        {
            Name = "TempClinicForDelete",
            Address = "1 Temp Street"
        });
        var clinic = await clinicRes.Content.ReadFromJsonAsync<ClinicResponse>();

        await _client.PostAsJsonAsync("/appointments/book/guest", new GuestBookAppointmentRequest
        {
            FirstName = "Guest",
            LastName = "User",
            Email = "guest.clinicdelete@example.com",
            Birthdate = new DateTime(1990, 1, 1),
            Gender = "Male",
            DoctorId = 1,
            ClinicId = clinic!.Id,
            CategoryId = 1,
            StartTime = new DateTime(2030, 4, 1, 9, 0, 0),
            EndTime = new DateTime(2030, 4, 1, 10, 0, 0)
        });

        var response = await _client.DeleteAsync($"/clinics/{clinic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/clinics/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
