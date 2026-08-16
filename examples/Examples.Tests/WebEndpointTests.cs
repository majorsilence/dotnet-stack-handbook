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
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

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

        Assert.That(shows, Is.Not.Null);
        Assert.That(shows.Select(x => x.ShowName), Does.Contain("Star Trek"));
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

        var response = await _client.DeleteAsync($"/api/shows/{show.Id}");

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
