using Examples.Web;

var builder = WebApplication.CreateBuilder(args);

// 1. register services
builder.Services.AddControllersWithViews();

// Scoped is the right lifetime for anything touching a database, because a scope
// is one http request.  The in memory repo is a singleton only because its list
// has to survive between requests for the example to be worth running.
builder.Services.AddSingleton<IShowRepo, InMemoryShowRepo>();

var app = builder.Build();

// 2. configure the middleware pipeline.  Order matters: each Use... wraps the
// ones after it.
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapDefaultControllerRoute();

// 3. minimal API endpoints, alongside the controllers.
//
// Note the /api prefix.  The chapter's listings map these at /shows, but this
// project also has a ShowsController, whose conventional route is /Shows - and
// route matching is case insensitive, so the two collide and the minimal API
// wins.  Endpoint groups apply the shared prefix once.  RequireAuthorization is
// left off so the example runs without an identity provider.
var shows = app.MapGroup("/api/shows");

shows.MapGet("/", async (IShowRepo repo) =>
    Results.Ok(await repo.GetShowsAsync()));

shows.MapGet("/{id:long}", async (long id, IShowRepo repo) =>
{
    var show = await repo.GetShowAsync(id);
    return show is null ? Results.NotFound() : Results.Ok(show);
});

shows.MapPost("/", async (TvShow show, IShowRepo repo) =>
{
    var id = await repo.InsertAsync(show);
    return Results.Created($"/api/shows/{id}", show);
});

shows.MapDelete("/{id:long}", async (long id, IShowRepo repo) =>
{
    await repo.DeleteAsync(id);
    return Results.NoContent();
});

app.Run();

// Needed so the integration tests in Examples.Tests can name this assembly's
// entry point.  Top level statements generate an internal Program class.
public partial class Program { }
