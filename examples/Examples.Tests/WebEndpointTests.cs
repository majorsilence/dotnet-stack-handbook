using System.Net;
using System.Net.Http.Json;
using Examples.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Examples.Tests;

// Starts the ASP.NET Core example in memory and calls it.  This is what makes the
// web chapter's listings more than a claim: the endpoints, the DI registration,
// the route constraints and the razor view all have to work for these to pass.
[TestFixture]
public class WebEndpointTests
{
    // null! because NUnit assigns these in OneTimeSetUp, which the compiler
    // cannot see.  This is the one place the ! operator is honest: the promise
    // is kept by the test framework rather than by the constructor.
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetShowsReturnsTheSeededShows()
    {
        var shows = await _client.GetFromJsonAsync<List<TvShow>>("/api/shows");

        // Is.Not.Null is a runtime check; the compiler does not learn anything
        // from it, hence the ! on the next line.
        Assert.That(shows, Is.Not.Null);
        Assert.That(shows!.Select(x => x.ShowName), Does.Contain("Star Trek"));
    }

    [Test]
    public async Task GetShowByIdReturnsNotFoundForAMissingId()
    {
        var response = await _client.GetAsync("/api/shows/9999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PostShowReturnsCreatedWithALocation()
    {
        var response = await _client.PostAsJsonAsync("/api/shows",
            new TvShow { ShowName = "Rick and morty", Episode = "3x14", Rating = 3.8m });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location?.ToString(), Does.StartWith("/api/shows/"));
    }

    [Test]
    public async Task DeleteShowReturnsNoContent()
    {
        var created = await _client.PostAsJsonAsync("/api/shows",
            new TvShow { ShowName = "Deleted Show", Rating = 1.0m });
        var show = await created.Content.ReadFromJsonAsync<TvShow>();

        Assert.That(show, Is.Not.Null);
        var response = await _client.DeleteAsync($"/api/shows/{show!.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task ShowsIndexRendersTheRazorView()
    {
        var html = await _client.GetStringAsync("/Shows");

        Assert.That(html, Does.Contain("<h1>TV Shows</h1>"));
        Assert.That(html, Does.Contain("Star Trek"));
    }
}
