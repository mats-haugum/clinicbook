using System.Net;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Specialities;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Specialities;

public class SpecialitiesControllerTests(CustomWebApplicationFactory factory)
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
    // GET /specialities
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_ReturnsOkWithSeededSpecialities()
    {
        var response = await _client.GetAsync("/specialities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<SpecialityResponse>>();
        body.Should().Contain(s => s.Name == "General Practice");
        body.Should().Contain(s => s.Name == "Cardiology");
        body.Should().Contain(s => s.Name == "Dermatology");
    }

    // -------------------------------------------------------------------------
    // GET /specialities/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetById_ExistingId_ReturnsSpeciality()
    {
        var response = await _client.GetAsync("/specialities/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SpecialityResponse>();
        body!.Name.Should().Be("General Practice");
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/specialities/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /specialities
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedWithBody()
    {
        var response = await _client.PostAsJsonAsync("/specialities", new CreateSpecialityRequest { Name = "Neurology" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SpecialityResponse>();
        body!.Id.Should().BeGreaterThan(0);
        body.Name.Should().Be("Neurology");
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/specialities", new CreateSpecialityRequest { Name = "Oncology" });
        var response = await _client.PostAsJsonAsync("/specialities", new CreateSpecialityRequest { Name = "Oncology" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /specialities/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_ExistingSpeciality_ReturnsUpdatedBody()
    {
        var response = await _client.PutAsJsonAsync("/specialities/3", new CreateSpecialityRequest { Name = "Advanced Dermatology" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SpecialityResponse>();
        body!.Name.Should().Be("Advanced Dermatology");
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/specialities/999", new CreateSpecialityRequest { Name = "Something" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /specialities/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_WithNoDependencies_ReturnsNoContent()
    {
        var created = await _client.PostAsJsonAsync("/specialities", new CreateSpecialityRequest { Name = "ToDelete" });
        var speciality = await created.Content.ReadFromJsonAsync<SpecialityResponse>();

        var response = await _client.DeleteAsync($"/specialities/{speciality!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithDoctorsAssigned_ReturnsConflict()
    {
        // Speciality 1 (General Practice) has doctors 1 and 4 assigned from seed data
        var response = await _client.DeleteAsync("/specialities/1");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/specialities/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
