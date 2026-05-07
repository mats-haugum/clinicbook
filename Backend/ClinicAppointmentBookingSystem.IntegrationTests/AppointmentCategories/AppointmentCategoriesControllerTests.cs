using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.AppointmentCategories;
using ClinicAppointmentBookingSystem.Models.DTOs.Appointments;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.AppointmentCategories;

public class AppointmentCategoriesControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync()
    {
        factory.ResetDatabase();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /appointment-categories
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsOkWithSeededCategories()
    {
        var response = await _client.GetAsync("/appointment-categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<AppointmentCategoryResponse>>();
        body.Should().Contain(c => c.Name == "General Checkup");
        body.Should().Contain(c => c.Name == "Follow-up");
        body.Should().Contain(c => c.Name == "Specialist Consultation");
        body.Should().Contain(c => c.Name == "Urgent Care");
    }

    // -------------------------------------------------------------------------
    // GET /appointment-categories/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingId_ReturnsCategory()
    {
        var response = await _client.GetAsync("/appointment-categories/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentCategoryResponse>();
        body!.Name.Should().Be("General Checkup");
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/appointment-categories/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /appointment-categories
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedWithBody()
    {
        var response = await _client.PostAsJsonAsync("/appointment-categories",
            new CreateAppointmentCategoryRequest { Name = "Vaccination" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AppointmentCategoryResponse>();
        body!.Id.Should().BeGreaterThan(0);
        body.Name.Should().Be("Vaccination");
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/appointment-categories", new CreateAppointmentCategoryRequest { Name = "DuplicateCategory" });
        var response = await _client.PostAsJsonAsync("/appointment-categories", new CreateAppointmentCategoryRequest { Name = "DuplicateCategory" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /appointment-categories/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingCategory_ReturnsUpdatedBody()
    {
        var response = await _client.PutAsJsonAsync("/appointment-categories/2",
            new CreateAppointmentCategoryRequest { Name = "Extended Follow-up" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AppointmentCategoryResponse>();
        body!.Name.Should().Be("Extended Follow-up");
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/appointment-categories/999",
            new CreateAppointmentCategoryRequest { Name = "Something" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /appointment-categories/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_WithNoDependencies_ReturnsNoContent()
    {
        var created = await _client.PostAsJsonAsync("/appointment-categories",
            new CreateAppointmentCategoryRequest { Name = "ToDelete" });
        var category = await created.Content.ReadFromJsonAsync<AppointmentCategoryResponse>();

        var response = await _client.DeleteAsync($"/appointment-categories/{category!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithAppointmentsAssigned_ReturnsConflict()
    {
        // Book a guest appointment using category 4 (Urgent Care) to create a dependency
        await _client.PostAsJsonAsync("/appointments/book/guest", new GuestBookAppointmentRequest
        {
            FirstName = "Guest",
            LastName = "User",
            Email = "guest.cat@example.com",
            Birthdate = new DateTime(1990, 1, 1),
            Gender = "Female",
            DoctorId = 1,
            ClinicId = 1,
            CategoryId = 4,
            StartTime = new DateTime(2030, 3, 1, 9, 0, 0),
            EndTime = new DateTime(2030, 3, 1, 10, 0, 0)
        });

        var response = await _client.DeleteAsync("/appointment-categories/4");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/appointment-categories/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
