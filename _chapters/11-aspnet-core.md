---
layout: chapter
title: "ASP.NET Core"
number: 11
part: 4
examples: Examples.Web
---

ASP.NET Core is the cross platform web framework for .NET. The same runtime serves web pages, json APIs, and background services, and it runs on linux, windows, and mac.

Every ASP.NET Core application starts from a `Program.cs` that builds a host, registers services, configures the request pipeline, and runs.

```cs
var builder = WebApplication.CreateBuilder(args);

// 1. register services
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 2. configure the middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapDefaultControllerRoute();

// 3. run
app.Run();
```

Middleware order is significant. Each `Use...` call wraps the ones after it, so authentication must be registered before authorization, and routing before either. Getting the order wrong produces confusing behaviour rather than a compile error.

## Dependency Injection

[Structuring an Application](03-structuring-an-application.html#ioc) covers the concepts. ASP.NET Core builds them in: the container is already there, and the framework resolves your controllers, pages, and hosted services through it.

Register the repository and business classes from the **Repository Pattern** section on `builder.Services`.

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<MajorSilence.DataAccess.ITestRepo>(sp =>
    new MajorSilence.DataAccess.TestRepo(
        builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<MajorSilence.BusinessStuff.TestStuff>();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

A controller then declares what it needs in its constructor and the framework supplies it.

```cs
[ApiController]
[Route("api/[controller]")]
public class ShowsController : ControllerBase
{
    private readonly MajorSilence.BusinessStuff.TestStuff _stuff;

    public ShowsController(MajorSilence.BusinessStuff.TestStuff stuff)
    {
        _stuff = stuff;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _stuff.DoStuff();
        return Ok();
    }
}
```

**Scoped** is the right lifetime for anything touching a database, because a scope is one http request. Two classes used in the same request share the connection and transaction; a later request gets fresh ones.

Do not resolve services by calling `provider.GetRequiredService` from inside your own code. That is the service locator pattern, and it hides dependencies that a constructor would have made obvious.

Connection strings belong in configuration rather than in code. `appsettings.json` holds the development value, and environment variables or a secret store override it in production.

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=SqlPlayground;Trusted_Connection=True;"
  }
}
```

## MVC

MVC splits a request into three parts: a **model** holding the data, a **view** rendering it, and a **controller** deciding what happens. It suits applications that serve rendered html pages.

Create a project.

```bash
dotnet new mvc -o YourWebApp
```

The rest of this chapter uses an `IShowRepo`, which is the same idea as chapter 3's
`ITestRepo` but with methods that return shows: `GetShowsAsync`, `GetShowAsync`,
`InsertAsync` and `DeleteAsync`.

A controller action returns a view along with the model it should render.

```cs
using Microsoft.AspNetCore.Mvc;

public class ShowsController : Controller
{
    private readonly IShowRepo _repo;

    public ShowsController(IShowRepo repo)
    {
        _repo = repo;
    }

    // GET /Shows
    public async Task<IActionResult> Index()
    {
        var shows = await _repo.GetShowsAsync();
        return View(shows);
    }

    // POST /Shows/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TvShow show)
    {
        if (!ModelState.IsValid)
        {
            return View(show);
        }

        await _repo.InsertAsync(show);
        return RedirectToAction(nameof(Index));
    }
}
```

By convention the view for `Index` lives at `Views/Shows/Index.cshtml`. `@model` declares what the view was handed, and the razor syntax mixes c# into html.

```html
@model IEnumerable<TvShow>

<h1>TV Shows</h1>

<table class="table">
    <thead>
        <tr><th>Name</th><th>Episode</th><th>Rating</th></tr>
    </thead>
    <tbody>
    @foreach (var show in Model)
    {
        <tr>
            <td>@show.ShowName</td>
            <td>@show.Episode</td>
            <td>@show.Rating</td>
        </tr>
    }
    </tbody>
</table>
```

Razor html encodes anything written with `@` by default, so user supplied values cannot inject script. Only `@Html.Raw` bypasses that, which is why it should be rare and deliberate.

Validation attributes on the model drive both the client side and server side checks. `ModelState.IsValid` is the server side half and must never be skipped, because client side validation is trivially bypassed.

```cs
public class TvShow
{
    [Required]
    [StringLength(50)]
    public string ShowName { get; set; }

    [Range(0, 5)]
    public decimal Rating { get; set; }
}
```

**Razor Pages** is a lighter alternative that pairs each page with its own handler class instead of routing through controllers. For a site that is mostly pages rather than shared logic it is usually the simpler choice.

## Minimal API

Minimal APIs express an http endpoint as a lambda, with no controller class. They suit small json services and are measurably faster to start.

```bash
dotnet new web -o YourApi
```

An entire service can fit in one file.

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IShowRepo>(sp =>
    new ShowRepo(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.MapGet("/shows", async (IShowRepo repo) =>
    Results.Ok(await repo.GetShowsAsync()));

app.MapGet("/shows/{id:long}", async (long id, IShowRepo repo) =>
{
    var show = await repo.GetShowAsync(id);
    return show is null ? Results.NotFound() : Results.Ok(show);
});

app.MapPost("/shows", async (TvShow show, IShowRepo repo) =>
{
    var id = await repo.InsertAsync(show);
    return Results.Created($"/shows/{id}", show);
});

app.Run();
```

Parameters are bound by source without any attributes: route values by name (`id`), registered services from the container (`IShowRepo`), and a complex type from the json body (`TvShow`).

Group related endpoints so shared configuration is written once.

```cs
var shows = app.MapGroup("/shows").RequireAuthorization();

shows.MapGet("/", async (IShowRepo repo) => await repo.GetShowsAsync());
shows.MapDelete("/{id:long}", async (long id, IShowRepo repo) =>
{
    await repo.DeleteAsync(id);
    return Results.NoContent();
});
```

Minimal APIs and controllers can coexist in one application. Reach for controllers when the endpoint count grows, when filters and model binding conventions start being worth it, or when the team simply prefers the structure.

When they do coexist, keep them on separate path prefixes. Route matching is case insensitive, so a minimal API mapped at `/shows` and a `ShowsController` whose conventional route is `/Shows` are the same route. There is no error at startup; the minimal API simply wins and the controller action is never reached. Putting the json endpoints under `/api` avoids the whole class of problem.

Use controllers or Minimal APIs, but be consistent within a project. Mixing both for the same resource makes the routing hard to follow.

## Blazor

Blazor builds interactive web UI in c# instead of javascript. Components are `.razor` files that combine markup, state, and event handlers.

```bash
dotnet new blazor -o YourBlazorApp
```

A component is a class with markup attached.

```html
@page "/shows"
@inject IShowRepo Repo

<h1>TV Shows</h1>

@if (_shows is null)
{
    <p>Loading...</p>
}
else
{
    <ul>
        @foreach (var show in _shows)
        {
            <li>@show.ShowName (@show.Rating)</li>
        }
    </ul>
}

<input @bind="_newName" placeholder="Show name" />
<button @onclick="AddShow">Add</button>

@code {
    private List<TvShow> _shows;
    private string _newName = "";

    protected override async Task OnInitializedAsync()
    {
        _shows = (await Repo.GetShowsAsync()).ToList();
    }

    private async Task AddShow()
    {
        if (string.IsNullOrWhiteSpace(_newName))
        {
            return;
        }

        await Repo.InsertAsync(new TvShow { ShowName = _newName });
        _shows = (await Repo.GetShowsAsync()).ToList();
        _newName = "";
    }
}
```

The part that decides everything else is the **render mode**, which controls where the component actually executes.

- **Static server rendering** - html is rendered once on the server and sent. No interactivity. Fastest, and the default in a new project.
- **Interactive Server** - the component runs on the server, and UI updates travel over a SignalR connection. Small download, but every user holds an open connection and all state lives in server memory.
- **Interactive WebAssembly** - the component runs in the browser on a .NET runtime. No connection to maintain and it works offline, at the cost of a larger initial download.
- **Interactive Auto** - server rendering on the first visit while the WebAssembly runtime downloads in the background, then WebAssembly afterwards.

Set the mode per component, so only the parts that need interactivity pay for it.

```html
@rendermode InteractiveServer
```

The catch worth knowing up front: with WebAssembly, the component runs on the user's machine. It cannot open a database connection, and any secret it holds is readable. Components running in the browser must go through an http API, exactly as a javascript frontend would. The `@inject IShowRepo` shown above only works under server rendering.
