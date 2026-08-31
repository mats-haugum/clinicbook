using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicAppointmentBookingSystem.Models.DTOs.Admin;
using ClinicAppointmentBookingSystem.Models.DTOs.Doctors;
using FluentAssertions;

namespace ClinicAppointmentBookingSystem.IntegrationTests.Doctors;

// [OutputCache(PolicyName = "short")] on DoctorsController.Search caches the
// whole HTTP response for 30s, keyed per distinct query string. A separate
// class (own CustomWebApplicationFactory, own cache) keeps that 30s window
// from ever leaking a stale response into DoctorsControllerTests' own
// functional Search tests, which reuse similar query strings.
public class DoctorsOutputCacheTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private string _adminToken = string.Empty;

    public async Task InitializeAsync()
    {
        factory.ResetDatabase();
        var response = await _client.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest
        {
            Email = "admin@clinicbook.com",
            Password = "Admin@123"
        });
        var body = await response.Content.ReadFromJsonAsync<AdminAuthResponse>();
        _adminToken = body!.Token;

        // Deliberately NOT set as _client.DefaultRequestHeaders.Authorization
        // here, unlike DoctorsControllerTests: ASP.NET Core's output cache
        // skips caching by default whenever the request carries an
        // Authorization header (so one user's cached response can never leak
        // to another), and Search itself doesn't require auth at all - so
        // sending a token on every request in this class would silently
        // defeat the very thing these tests are checking. The token is
        // attached only to the one request that actually needs it.
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpRequestMessage AuthenticatedRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return request;
    }

    [Fact]
    public async Task Search_SecondIdenticalRequestWithinCacheWindow_ReturnsCachedStaleData()
    {
        // Sarah Connor (seeded doctor 2) is assigned to only one clinic, so
        // "connor" starts out matching exactly one row - unlike "wilson",
        // which would match two (James Wilson is assigned to both seeded
        // clinics, one search result row per clinic assignment).
        var first = await _client.GetFromJsonAsync<List<DoctorSearchResponse>>("/doctors/search?name=connor");
        first.Should().ContainSingle();

        // Add a second doctor whose last name would also match "connor" -
        // if Search actually hit the database again, the second response
        // would contain two results. Needs the admin token (Create is
        // [Authorize(Roles = "Admin")]), attached only to this one request.
        var createRequest = AuthenticatedRequest(HttpMethod.Post, "/doctors");
        createRequest.Content = JsonContent.Create(new CreateDoctorRequest
        {
            FirstName = "Owen",
            LastName = "Connor",
            SpecialityId = 1,
            ClinicIds = [1]
        });
        await _client.SendAsync(createRequest);

        var second = await _client.GetFromJsonAsync<List<DoctorSearchResponse>>("/doctors/search?name=connor");

        // Still just the one result - served from the 30s cache, not a fresh query.
        second.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_DifferentQueryStrings_AreCachedSeparately()
    {
        var wilson = await _client.GetFromJsonAsync<List<DoctorSearchResponse>>("/doctors/search?name=wilson");
        var connor = await _client.GetFromJsonAsync<List<DoctorSearchResponse>>("/doctors/search?name=connor");

        // SetVaryByQuery("*") means each distinct query string is its own cache
        // entry - a "connor" search must never return the "wilson" result just
        // because both were requested from the same cached endpoint. James
        // Wilson has two rows here (one per clinic assignment - see seed data
        // in ClinicBookingDbContext.OnModelCreating), Sarah Connor has one.
        wilson.Should().OnlyContain(d => d.FullName == "James Wilson");
        wilson.Should().HaveCount(2);
        connor.Should().ContainSingle(d => d.FullName == "Sarah Connor");
    }
}
