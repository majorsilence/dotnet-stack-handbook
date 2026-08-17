# Modernization plan

What this book needs to be a credible full stack .NET reference in 2026, in the
order I would do it. Written 2026-08-17.

The book's strength is the half it inherited from the original post: language,
data access, structuring. Its weakness is everything that turns code into a
running service, which is the half the title promises. Chapter lengths show it —
Data Access 767 lines, Language 780, but ASP.NET Core 311 and Monitoring 86
before this plan started. Most of the work below is on the web and operations
side.

Each phase is independently shippable. Nothing later depends on anything earlier
except where stated.

---

## Phase 0 — done

- [x] **Nullable reference types on.** `<Nullable>enable</Nullable>` plus
      `<WarningsAsErrors>Nullable</WarningsAsErrors>` in
      `examples/Directory.Build.props`; every example fixed rather than
      suppressed; new "Nullable reference types" section in chapter 1; listings
      in chapters 1 and 3 updated to match (`required` members, `string?`
      returns); preface conventions and README updated.
- [x] **The repository / EF Core position stated explicitly.** New section in
      chapter 3: use repositories over Dapper and ADO.NET, do not put a generic
      repository and a unit of work over EF Core, with the reasoning and the
      cases where a hand written domain repository does earn its place. Cross
      referenced from chapter 10.
- [x] **Chapter 13 rewritten as vanilla JavaScript.** Modules, DOM, event
      delegation, `textContent` vs `innerHTML`, `fetch` with `URLSearchParams`
      and `FormData`, antiforgery from a fetch POST, `AbortSignal` timeouts, a
      complete sixty line client for the chapter 11 API, htmx, and an explicit
      "what to skip" on jQuery, Kendo, build steps and TypeScript.
- [x] **Chapters 14 and 15 rebuilt.** 14 is now health checks →
      OpenTelemetry → metrics/traces/logs → Prometheus → Grafana → alerting →
      live diagnostics. 15 is now the anatomy of a .NET pipeline, reproducible
      restore, matrix builds, versioning, NuGet and container publishing,
      Testcontainers, pipeline security and gated deployment.

---

## Phase 1 — the security chapter (highest priority)

There is currently no security chapter. Authentication appears twice in the
whole book: one `app.UseAuthorization()` line and one `[ValidateAntiForgeryToken]`
attribute. A full stack book that teaches a reader to build a login-less app and
then ship it behind nginx is incomplete in a way that has consequences.

New chapter in part 4, after ASP.NET Core.

- [ ] Authentication: cookies vs bearer tokens, and when each applies.
- [ ] ASP.NET Core Identity for a self contained app; external OIDC (Entra ID,
      Keycloak, Auth0) for everything else, and why the second is usually right.
- [ ] Authorization: roles, claims, policies, `IAuthorizationRequirement`,
      resource based checks. Policies over role string comparisons.
- [ ] **Data Protection key ring persistence.** Silently breaks the moment
      chapter 12 puts the app in a container with more than one replica, and
      nothing else in the book warns about it.
- [ ] Secrets: `dotnet user-secrets` in development, environment variables and a
      vault in production, and never `appsettings.json`. The book does not
      mention `user-secrets` at all today.
- [ ] CORS in ASP.NET Core — currently only mentioned from the browser side in
      chapter 13.
- [ ] HTTPS, HSTS, and a Content Security Policy that works with chapter 13's
      "no inline script" rule.
- [ ] An OWASP pass with .NET specifics: parameterised SQL (cross reference the
      Dapper listings), XSS and Razor's automatic encoding, CSRF, IDOR, mass
      assignment on model binding, password hashing.
- [ ] Rate limiting middleware, since brute force is the attack every login page
      gets.

**Acceptance:** a reader can add login, protect an endpoint, and store a secret
without leaving the book.

---

## Phase 2 — the cross-cutting plumbing chapter

Configuration and logging are assumed by every chapter after 3 and taught by
none. `IOptions` and `appsettings` appear nowhere in the book.

New chapter in part 1, after Structuring an Application.

- [ ] Configuration providers and their precedence: json, environment, command
      line, user secrets, vault.
- [ ] The options pattern: `IOptions`, `IOptionsSnapshot`, `IOptionsMonitor`,
      and validation on startup with `ValidateDataAnnotations().ValidateOnStart()`.
- [ ] Environments, and why `IHostEnvironment` beats an `#if DEBUG`.
- [ ] `ILogger`, structured templates, scopes, categories, levels. (Chapter 14
      now covers the observability half of this; keep them cross referenced and
      do not duplicate.)
- [ ] The generic host: `IHostedService` / `BackgroundService`, graceful
      shutdown, `IHostApplicationLifetime`. Chapter 2 hand rolls a work queue
      and never mentions either, nor `System.Threading.Channels`, which is what
      that queue should be.
- [ ] Resilience: `IHttpClientFactory`, `Microsoft.Extensions.Http.Resilience`,
      retries with jitter, timeouts, circuit breakers, and EF Core's
      `EnableRetryOnFailure`. Chapter 2 currently hands the reader a raw
      `HttpClient` with no mention of socket exhaustion.

**Acceptance:** the words "configuration", "options", "background service" and
"retry" all resolve to somewhere in the book.

---

## Phase 3 — bring the web chapter up to what people build

Chapter 11 is 311 lines for DI, MVC, minimal APIs and Blazor. It needs to roughly
double.

- [ ] OpenAPI with `Microsoft.AspNetCore.OpenApi` and Scalar — note that
      Swashbuckle left the templates in .NET 9, which is the single most common
      "why is my Swagger gone" question.
- [ ] Model validation, `ProblemDetails`, and a global exception handler.
- [ ] API versioning.
- [ ] Output caching, response compression, and the built-in rate limiter.
- [ ] Blazor render modes in depth — Server, WebAssembly, Auto, static SSR. The
      chapter names the concept but one paragraph is not enough for the decision
      it drives.
- [ ] SignalR for the cases htmx and polling do not cover.
- [ ] gRPC, briefly, for service to service.
- [ ] Split the chapter if it gets past ~700 lines: "ASP.NET Core" and "Building
      an HTTP API".

---

## Phase 4 — .NET Aspire and the cloud

The biggest single currency gap. Aspire is how Microsoft now expects multi
service local development, service discovery and OTel defaults to work, and the
book already teaches the exact topology it targets — web + worker + Postgres +
Redis — assembled by hand across four chapters.

- [ ] New chapter: Aspire app host, service defaults, the dashboard, service
      discovery, and how it composes the pieces from parts 3 and 4.
- [ ] Deployment: Azure Container Apps and App Service; one AWS path; managed
      identity instead of connection strings with passwords in them.
- [ ] Infrastructure as code, briefly — Bicep or Terraform, enough to make the
      point that the cluster should not be hand built.
- [ ] Keep the "plenty of applications should stop before Kubernetes" position
      from chapter 12. Aspire strengthens it rather than undermining it.

Also fill the gaps in chapter 12 while in the area: Helm or kustomize, probes and
resource limits, ConfigMaps and Secrets, HPA, and the non-container hosting path
(systemd unit, Windows Service).

---

## Phase 5 — finish the language chapter

Chapter 1 stops at variables, objects and interfaces. A reader who has read it
cannot write idiomatic C#.

- [ ] Collections and LINQ, as taught topics. LINQ currently appears only
      incidentally in chapters 3 and 10.
- [ ] Generics.
- [ ] Exceptions, and an error handling strategy: what to catch, what to let
      propagate, custom exception types, and why `catch (Exception)` at the top
      of a method is nearly always wrong.
- [ ] Delegates, lambdas, `Func`/`Action`.
- [ ] Records and value equality; pattern matching and switch expressions.
- [ ] `IDisposable`, `using`, and `IAsyncDisposable`.
- [ ] Extension methods, enums, tuples.
- [ ] Split into "The Language" and "Working with Data in Memory" if it passes
      ~1200 lines.

---

## Phase 6 — data, testing and the rest

**Data (chapters 6–10)**

- [ ] Caching as a strategy section: `IMemoryCache`, `IDistributedCache`,
      `HybridCache` (.NET 9), invalidation, and the stampede problem. The Redis
      chapter shows session and cache mechanics but never the strategy.
- [ ] EF Core performance: N+1, `AsSplitQuery`, compiled queries, `ExecuteUpdate`
      and `ExecuteDelete`, and how to see the SQL.
- [ ] Zero downtime migrations — expand and contract. Chapter 15 now names the
      constraint; chapter 10 should teach the technique.
- [ ] MySQL/MariaDB, at least a short section, for the LAMP-adjacent shops.
- [ ] A document store — MongoDB or Cosmos — and honest guidance on when
      relational is still the answer.
- [ ] pgvector, tied to the AI chapter below.

**Testing (chapter 3's last section)**

- [ ] `WebApplicationFactory` integration testing — the examples tree already
      does this; the book never explains it.
- [ ] Testcontainers, now that chapter 15 introduces it, and use it in the
      examples solution to actually run the SQL Server, PostgreSQL and Redis
      listings that CI currently only compiles. **This is the highest leverage
      engineering change left in the repo** — it removes the "compiled but not
      run" caveat from the README.
- [ ] Test data builders, and a paragraph on what not to mock.

**Messaging (chapter 9)**

- [ ] Beyond Redis pub/sub: RabbitMQ or Kafka, MassTransit, delivery semantics,
      dead lettering, the outbox pattern and idempotency.

**AI (new, short chapter)**

- [ ] `Microsoft.Extensions.AI`, chat completion, embeddings, RAG over pgvector,
      MCP servers in .NET, structured output — with sober cost, latency and
      failure mode caveats. A 2026 full stack .NET book with nothing here has a
      visible hole; a credulous chapter would be worse than none.

**Modernization (new chapter, and a differentiator)**

- [ ] The book covers VB and Crystal Reports, so its reader is disproportionately
      likely to be on .NET Framework. Upgrade Assistant,
      `Microsoft.Windows.Compatibility`, WCF → CoreWCF, WebForms and VB6 paths,
      strangler fig. Most 2026 .NET books will not touch this; it should own it.

**Reporting (chapter 5)**

- [ ] Balance Crystal Reports with modern reporting: Majorsilence Reporting,
      Majorsilence.PDF, QuestPDF. Part 2 currently reads a decade behind.

---

## Book craft, ongoing

- [ ] **State a version policy.** ".NET 10 unless a chapter says otherwise" needs
      a companion note on the support lifecycle and an update cadence — .NET 11
      lands around November 2026 and the book will be one release behind within
      months. Add a "what changed per release" appendix.
- [ ] **A capstone.** Every chapter is isolated snippets today. One small
      application built across the parts — the `TVShow` domain already recurs —
      would make it a book rather than a well organised reference.
- [ ] **Per chapter "when not to use this"** decision tables. The strongest
      passages in the book already do this informally; make it a convention.
- [ ] **Reference furniture:** glossary, troubleshooting and error message index,
      further reading, and site search (Pagefind or lunr). A 16 chapter reference
      without search is hard to use.
- [ ] **Even out the register.** Chapters 1, 3, 10, 12, and now 13–15, have a
      narrative voice; several others still read like the original blog post.
- [ ] `version: 1.0-draft` in `_config.yml` with no CHANGELOG and no
      CONTRIBUTING for a CC BY-SA community book.
