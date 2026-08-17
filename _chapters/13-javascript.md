---
layout: chapter
title: "JavaScript in the Browser"
number: 13
part: 4
---

The browser has caught up. The things jQuery was indispensable for - selecting elements, ajax, animation, papering over browser differences - are all in the platform now, and have been for years. A server rendered ASP.NET Core application needs a modest amount of JavaScript to feel alive, and that amount is small enough to write by hand.

This chapter is plain JavaScript: no framework, no build step, no TypeScript. Everything here runs as written in any current browser. That is a deliberate position rather than a limitation - the code that survives longest in a .NET shop is usually the code with nothing underneath it to go stale. Where you genuinely need a component framework, [Blazor](11-aspnet-core.html) is already in the stack and is a better answer than bolting a second ecosystem onto the side.

## Where the script goes {#where-the-script-goes}

Static files live under `wwwroot`, served by `app.UseStaticFiles()`. Put scripts in `wwwroot/js` and reference them from the layout.

```html
<!-- type="module" gives you imports, strict mode, and deferred execution.
     It is the default worth using for anything past a couple of lines. -->
<script type="module" src="~/js/shows.js" asp-append-version="true"></script>
```

`asp-append-version="true"` appends a content hash to the URL, so a changed file is a different URL and the browser cannot serve a stale copy. Without it you will spend an afternoon debugging a fix that shipped fine and was cached.

Two rules cover most of the mistakes:

- **Never inline data into a script tag by string concatenation.** Rendering `var id = @Model.Id;` into JavaScript is how you get script injection. Put values in `data-` attributes on an element and read them from there.
- **`type="module"` scripts are deferred automatically.** They run after the document is parsed, so there is no need for a `DOMContentLoaded` wrapper and no need to put the tag at the bottom of the body.

```html
<div id="shows" data-api="/api/shows" data-page-size="20"></div>
```

```js
const el = document.querySelector("#shows");
const api = el.dataset.api;                    // "/api/shows"
const pageSize = Number(el.dataset.pageSize);  // 20
```

## Modules

Files are modules. Each has its own scope, exports what it wants to share, and imports what it needs. There are no globals to collide and no load order to get right.

```js
// wwwroot/js/api.js
export async function getShows() {
  return await getJson("/api/shows");
}

// A single place where every call's error handling lives.
async function getJson(url) {
  const response = await fetch(url, { headers: { Accept: "application/json" } });
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
  }
  return await response.json();
}
```

```js
// wwwroot/js/shows.js
import { getShows } from "./api.js";

const shows = await getShows();
console.log(shows.length);
```

Note the file extension in the import. Browsers resolve module specifiers as URLs, not the way Node does, so `./api.js` is required and `./api` will 404. Top level `await` works in a module, which is why the last listing needs no wrapper function.

## The DOM

Four methods cover almost everything.

```js
const el = document.querySelector("#shows");        // first match, or null
const all = document.querySelectorAll(".show-row"); // every match, a NodeList
const row = document.createElement("tr");
el.append(row);                                     // also accepts plain strings
```

`querySelectorAll` returns a `NodeList`. It is iterable with `for...of` and has `forEach`, but it is not an array; `Array.from(all)` when you want `map` or `filter`.

Class and attribute changes are direct:

```js
row.classList.add("is-selected");
row.classList.toggle("is-open", isOpen);   // second argument forces the state
row.hidden = true;                          // better than a display:none class
row.setAttribute("aria-expanded", "true");
```

### Rendering without an injection bug {#rendering-safely}

This is the one place where a careless line becomes a security bug, so it gets its own section.

**`textContent` is safe. `innerHTML` is not.** Anything assigned to `textContent` is text, always. Anything assigned to `innerHTML` is parsed as markup, so a show name of `<img src=x onerror=alert(1)>` stored by one user runs in the next user's browser. That is stored XSS, and no amount of encoding on the server saves you if the browser is handed the raw value and told to parse it.

```js
// Safe: the value is text no matter what it contains.
cell.textContent = show.showName;

// Unsafe with any value that came from a user, a database, or an api.
cell.innerHTML = show.showName;
```

For a row of several fields, build elements and let the browser do the escaping:

```js
function renderRow(show) {
  const tr = document.createElement("tr");
  for (const value of [show.showName, show.episode ?? "", show.rating]) {
    const td = document.createElement("td");
    td.textContent = value;
    tr.append(td);
  }
  return tr;
}
```

When the markup is more than a couple of elements, use a `<template>`. The browser parses it once, `cloneNode` is cheap, and the structure stays in the HTML file where it is easy to read.

```html
<template id="show-row">
  <tr>
    <td class="name"></td>
    <td class="episode"></td>
    <td class="rating"></td>
  </tr>
</template>
```

```js
const template = document.querySelector("#show-row");

function renderRow(show) {
  const row = template.content.firstElementChild.cloneNode(true);
  row.querySelector(".name").textContent = show.showName;
  row.querySelector(".episode").textContent = show.episode ?? "";
  row.querySelector(".rating").textContent = show.rating;
  return row;
}
```

Build the whole list into a `DocumentFragment` and append once. Appending inside the loop makes the browser recalculate layout on every iteration.

```js
const fragment = document.createDocumentFragment();
for (const show of shows) {
  fragment.append(renderRow(show));
}
document.querySelector("#show-body").replaceChildren(fragment);
```

`replaceChildren` empties the element and fills it in one call, which is the modern replacement for `innerHTML = ""` followed by a loop.

## Events

`addEventListener` on the element, and nothing else. Inline `onclick` attributes mix behaviour into markup and are blocked by a strict Content Security Policy anyway.

```js
document.querySelector("#refresh").addEventListener("click", async () => {
  await load();
});
```

For a list whose rows come and go, attach one listener to the container instead of one per row. The event bubbles up and `closest` finds which row it came from. This is **event delegation**, and it keeps working for rows added after the listener was attached.

```js
document.querySelector("#show-body").addEventListener("click", (event) => {
  const button = event.target.closest("button[data-delete-id]");
  if (!button) return;   // the click was somewhere else in the table

  remove(Number(button.dataset.deleteId));
});
```

For forms, listen for `submit` on the form rather than `click` on the button, so the keyboard path works too.

```js
form.addEventListener("submit", async (event) => {
  event.preventDefault();   // stop the normal navigation
  await save(new FormData(form));
});
```

## fetch

`fetch` returns a promise for a `Response`. Two things about it catch people out on the first day:

- **A 404 or a 500 is not a rejection.** The promise resolves; only a network failure rejects. You have to check `response.ok` yourself, which is why every listing here does.
- **The body is read separately.** `await response.json()` or `await response.text()` is a second await, because the headers arrive before the body does.

### GET

```js
async function getShows() {
  const response = await fetch("/api/shows", {
    headers: { Accept: "application/json" },
  });

  if (!response.ok) {
    throw new Error(`GET /api/shows failed: ${response.status}`);
  }

  return await response.json();
}
```

Query strings are built with `URLSearchParams`, which handles the encoding.

```js
const query = new URLSearchParams({ page: 2, search: "star trek" });
const response = await fetch(`/api/shows?${query}`);   // ?page=2&search=star+trek
```

### POST json

This is what a minimal API endpoint taking a `TvShow` expects.

```js
async function addShow(show) {
  const response = await fetch("/api/shows", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(show),
  });

  if (!response.ok) {
    throw new Error(`POST /api/shows failed: ${response.status}`);
  }

  return await response.json();
}

await addShow({ showName: "Rick and morty", episode: "3x14", rating: 3.8 });
```

The property names are camel case because that is what `System.Text.Json` produces and accepts by default, matching the `TvShow` class in [ASP.NET Core](11-aspnet-core.html).

### POST a form {#post-a-form}

`URLSearchParams` also serialises to `application/x-www-form-urlencoded`, so the hand written serialise helper that used to live in this chapter is unnecessary.

```js
const body = new URLSearchParams({
  showName: "Star Trek",
  episode: "1x12",
});

const response = await fetch("/shows/create", { method: "POST", body });
```

Setting the body to a `URLSearchParams` sets the content type for you. Do not set it by hand.

For a form that already exists in the page, `FormData` reads it in one line - including file inputs, which `URLSearchParams` cannot carry.

```js
const response = await fetch("/shows/create", {
  method: "POST",
  body: new FormData(form),   // multipart/form-data, boundary and all
});
```

### The antiforgery token {#antiforgery}

An MVC action marked `[ValidateAntiForgeryToken]` will reject a fetch POST with a 400 unless the token goes with it. `FormData` built from a razor `<form>` carries the hidden field automatically. A json POST does not, so send it as a header instead.

```cshtml
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery
<div id="app" data-antiforgery="@Antiforgery.GetAndStoreTokens(Context).RequestToken"></div>
```

```js
const token = document.querySelector("#app").dataset.antiforgery;

await fetch("/api/shows", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "RequestVerificationToken": token,
  },
  body: JSON.stringify(show),
});
```

```cs
// Program.cs - tell the framework to look at that header.
builder.Services.AddAntiforgery(options =>
    options.HeaderName = "RequestVerificationToken");
```

### Timeouts and cancellation {#timeouts}

`fetch` has no timeout of its own. A request against a hung server waits forever, which in a browser tab means a spinner that never stops.

```js
const response = await fetch("/api/shows", {
  signal: AbortSignal.timeout(10_000),   // throws TimeoutError after 10s
});
```

For a search box, keep the signal so the previous request can be cancelled when the next keystroke arrives. Otherwise responses race and the slower, older one can land last and win.

```js
let inFlight;

async function search(term) {
  inFlight?.abort();
  inFlight = new AbortController();

  try {
    const response = await fetch(`/api/shows?${new URLSearchParams({ term })}`, {
      signal: inFlight.signal,
    });
    render(await response.json());
  } catch (error) {
    if (error.name === "AbortError") return;   // superseded, not a failure
    throw error;
  }
}
```

## Putting it together {#putting-it-together}

The whole client for the `/api/shows` endpoints from [ASP.NET Core](11-aspnet-core.html): load, render, add, delete. This is the amount of JavaScript a server rendered page usually needs, and it is roughly sixty lines with no dependencies.

```js
// wwwroot/js/shows.js
const table = document.querySelector("#show-body");
const form = document.querySelector("#add-show");
const status = document.querySelector("#status");
const template = document.querySelector("#show-row");

async function send(url, options = {}) {
  const response = await fetch(url, {
    headers: { Accept: "application/json", ...options.headers },
    signal: AbortSignal.timeout(10_000),
    ...options,
  });

  if (!response.ok) {
    throw new Error(`${options.method ?? "GET"} ${url}: ${response.status}`);
  }

  return response.status === 204 ? null : await response.json();
}

function renderRow(show) {
  const row = template.content.firstElementChild.cloneNode(true);
  row.querySelector(".name").textContent = show.showName;
  row.querySelector(".episode").textContent = show.episode ?? "";
  row.querySelector(".rating").textContent = show.rating;
  row.querySelector("button").dataset.deleteId = show.id;
  return row;
}

async function load() {
  status.textContent = "Loading...";
  try {
    const shows = await send("/api/shows");
    const fragment = document.createDocumentFragment();
    for (const show of shows) {
      fragment.append(renderRow(show));
    }
    table.replaceChildren(fragment);
    status.textContent = `${shows.length} show(s)`;
  } catch (error) {
    status.textContent = `Could not load shows: ${error.message}`;
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const data = Object.fromEntries(new FormData(form));

  await send("/api/shows", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...data, rating: Number(data.rating) }),
  });

  form.reset();
  await load();
});

table.addEventListener("click", async (event) => {
  const button = event.target.closest("button[data-delete-id]");
  if (!button) return;

  await send(`/api/shows/${button.dataset.deleteId}`, { method: "DELETE" });
  await load();
});

await load();
```

Two habits in there are worth naming. Every failure path writes to `#status`, because a fetch that silently does nothing is the most common bug in hand written frontends. And every mutation reloads the list rather than patching the row it changed - slower in theory, but it keeps the page and the database from drifting apart, which for a table of tens or hundreds of rows is the right trade.

## htmx, when you would rather not write any of this {#htmx}

[htmx](https://htmx.org/) puts the ajax in the HTML. The server keeps rendering markup, and attributes say which element to swap when a request comes back. There is no JSON, no client side rendering, and nothing to keep in sync.

```html
<button hx-delete="/shows/12" hx-target="closest tr" hx-swap="outerHTML">
    Delete
</button>

<input type="search" name="term"
       hx-get="/shows/search" hx-trigger="keyup changed delay:300ms"
       hx-target="#show-body">
```

That second listing is the debounced, cancel-the-previous-request search from earlier, in three attributes. Razor partials are exactly the "return a fragment of html" endpoint it wants, so the fit with ASP.NET Core is unusually good.

Reach for htmx when the page is server rendered and you want interactivity without a client side state model. Write the plain JavaScript above when the interaction is local to the page - a chart, a drag and drop reorder, a form that validates as you type - and there is no server round trip to hang it on. The two combine fine in one page.

## What to skip {#what-to-skip}

**jQuery.** Do not add it to new work. `$("#id")` is `document.querySelector("#id")`, `$.ajax` is `fetch`, `$(el).addClass` is `el.classList.add`, and `$(document).ready` is what `type="module"` already does for you. It is still perfectly good code and there is no reason to rip it out of an application that works - but every line of it in a new page is a dependency and a lookup in someone's memory that the platform no longer needs.

**Kendo UI and the other commercial control suites.** They are worth the licence for one specific thing: a grid with virtualised scrolling, grouping, frozen columns and Excel export, on a deadline. That is a real requirement and building it yourself is weeks. Everything else in the suite - buttons, dropdowns, date pickers, tabs - is a heavy way to get what `<dialog>`, `<details>`, `<input type="date">` and thirty lines of CSS already do, and it dictates the styling of a page for as long as the page exists.

**A build step, until something forces it.** No bundler, no transpiler, no `node_modules` in a .NET repository, until a real dependency needs one. Modules load fine unbundled over HTTP/2, and the moment you add a build you have added a second toolchain that must be installed on every developer machine and in every CI job.

**TypeScript, in this context specifically.** It is a good language and the argument for it is real. But it needs the build step above, and in an ASP.NET Core application the types that matter - the shape of the API contract - are already declared in C#. If the frontend grows to where you want types across a few thousand lines of it, that is the signal to reconsider the whole approach and use Blazor, not to add a compiler to the JavaScript.
