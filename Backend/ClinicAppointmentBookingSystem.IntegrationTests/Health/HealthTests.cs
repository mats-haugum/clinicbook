using System.Net;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Health;

// CustomWebApplicationFactory only overrides the DbContext registration, not
// the health check's own SQL Server/Redis connection strings (those still
// come straight from appsettings.json). So this test needs the real local
// dev SQL Server AND Redis containers reachable at localhost,1433 and
// localhost:6379 - see deploy/how-to-run-locally-for-development.md - and in
// CI, the redis service container added to .github/workflows/ci.yml
// alongside the existing mssql one.
public class HealthTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthyWhenDependenciesAreUp()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // No custom ResponseWriter is configured in Program.cs, so
        // MapHealthChecks falls back to its default: the aggregate status as
        // plain text, not a JSON body.
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
