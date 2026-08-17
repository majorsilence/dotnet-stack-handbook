---
layout: chapter
title: "Monitoring and Observability"
number: 14
part: 5
---

The application is running behind nginx, in a container, on a machine you are not looking at. Monitoring is how you find out that it is unwell before a user tells you, and observability is whether you can then work out *why* without deploying new code to find out.

Those are different jobs and they need different things. A dashboard that goes red answers "is it broken". Answering "why is it broken, for whom, since when" needs the request that failed, the query it ran, and the log line it wrote, joined together. This chapter builds that from the inside out: health checks first, because they are ten minutes of work and Kubernetes needs them; then OpenTelemetry, which is how a .NET application emits metrics, traces and logs in 2026; then Prometheus and Grafana to store and draw them; then alerting, which is where most of the value and all of the pain lives.

The three signals, in the order they earn their keep:

- **Metrics** are cheap numbers over time - request rate, error rate, duration, queue depth. They tell you something is wrong and are what alerts fire on.
- **Traces** follow one request across every service and database call it touched. They tell you *where* the time went.
- **Logs** are the detail for one moment. They tell you what the code thought it was doing.

## Health checks {#health-checks}

Start here. A health check is an endpoint that says whether the process is alive and whether it is ready to take traffic, and it is what the container orchestrator in [Containers, nginx and Kubernetes](12-containers-and-hosting.html) polls to decide whether to restart or route to your pod.

```bash
dotnet add package AspNetCore.HealthChecks.NpgSql
dotnet add package AspNetCore.HealthChecks.Redis
```

```cs
builder.Services.AddHealthChecks()
    // Liveness: is this process wedged?  Nothing external, or a restart loop
    // is one slow database away.
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    // Readiness: can it actually serve?  Dependencies belong here.
    .AddNpgSql(builder.Configuration.GetConnectionString("Shows")!,
        name: "postgres", tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!,
        name: "redis", tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});
```

**The split matters more than it looks.** Liveness failing means "kill this process"; readiness failing means "stop sending it requests, but leave it alone". Put the database check in the liveness probe and a thirty second database blip restarts every replica you have, simultaneously, which turns a blip into an outage. The rule: liveness checks only things a restart could fix.

A custom check is one method.

```cs
public class QueueDepthCheck : IHealthCheck
{
    private readonly IWorkQueue _queue;

    public QueueDepthCheck(IWorkQueue queue) => _queue = queue;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var depth = _queue.Count;

        return Task.FromResult(depth switch
        {
            > 10_000 => HealthCheckResult.Unhealthy($"queue depth {depth}"),
            > 1_000 => HealthCheckResult.Degraded($"queue depth {depth}"),
            _ => HealthCheckResult.Healthy(),
        });
    }
}
```

Health endpoints should not need authentication - the thing polling them is a kubelet - but they should not leak either. The default response is the word `Healthy` and nothing else, which is right. If you add a detailed json writer that names every dependency and its connection string, put it on a separate, protected route.

## OpenTelemetry {#opentelemetry}

[OpenTelemetry](https://opentelemetry.io/) is the vendor neutral standard for emitting telemetry, and in .NET it is not a bolt-on: `ILogger`, `System.Diagnostics.Metrics` and `System.Diagnostics.Activity` are already the OpenTelemetry data model. The packages below are wiring and exporters, not a new logging framework.

This matters commercially as much as technically. Instrument once against OTel and the backend becomes a configuration value - Prometheus and Grafana today, a hosted vendor next year, both at once during a migration - with no change to application code.

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.Runtime
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

```cs
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    // Who is speaking.  Without this everything arrives labelled "unknown_service".
    .ConfigureResource(resource => resource.AddService(
        serviceName: "shows-api",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
        serviceInstanceId: Environment.MachineName))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()   // request rate, duration, active requests
        .AddHttpClientInstrumentation()   // outbound calls
        .AddRuntimeInstrumentation()      // GC, heap, thread pool, exceptions
        .AddMeter("Shows.Api")            // your own, below
        .AddOtlpExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Shows.Api")
        .AddOtlpExporter());

// Logs go through the same pipeline, which is what correlates them with traces.
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter();
});
```

The exporter reads standard environment variables, so the endpoint is deployment configuration rather than code:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_RESOURCE_ATTRIBUTES=deployment.environment=production
```

Point that at an **OpenTelemetry Collector** rather than directly at a backend. The collector is one process per host or per cluster that receives, batches, filters and fans out - so retention, sampling and "also send traces to this second system while we evaluate it" become collector config instead of a redeploy of every service.

Add `OpenTelemetry.Instrumentation.EntityFrameworkCore` and call `.AddEntityFrameworkCoreInstrumentation()` if you use EF Core; it puts every query on the trace as its own span, which is usually where the answer is. The equivalent for Npgsql, `StackExchange.Redis` and SQL Client all exist as separate instrumentation packages.

### The metrics you get for free {#free-metrics}

`AddAspNetCoreInstrumentation` and `AddRuntimeInstrumentation` emit the standard .NET meters. These are the ones worth knowing by name, because dashboards and alerts are built on them:

| Metric | What it tells you |
| --- | --- |
| `http.server.request.duration` | Histogram of request latency, tagged by route, method and status. The single most useful metric you have. |
| `http.server.active_requests` | How many are in flight. Rising while duration is flat means saturation. |
| `http.client.request.duration` | The same for calls your service makes outbound. |
| `kestrel.active_connections` | Connections, as distinct from requests. |
| `dotnet.gc.pause.time` | Time spent in GC. A latency problem with no matching database time is often here. |
| `dotnet.thread_pool.queue.length` | Work waiting for a thread. Sustained non-zero means blocking calls in async code - see [Asynchronous Work and Threads](02-async-and-threads.html). |

### Your own metrics {#custom-metrics}

Business metrics are what let you spot a broken deploy that is technically returning 200s. Create them through `IMeterFactory` so the meter is disposed with the container and is testable.

```cs
using System.Diagnostics.Metrics;

public sealed class ShowMetrics
{
    public const string MeterName = "Shows.Api";

    private readonly Counter<long> _created;
    private readonly Histogram<double> _importDuration;

    public ShowMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);

        _created = meter.CreateCounter<long>(
            "shows.created",
            unit: "{show}",
            description: "Shows added, by where the request came from.");

        _importDuration = meter.CreateHistogram<double>(
            "shows.import.duration",
            unit: "ms",
            description: "How long a catalogue import took.");
    }

    public void ShowCreated(string source) =>
        _created.Add(1, new KeyValuePair<string, object?>("source", source));

    public void ImportCompleted(TimeSpan elapsed) =>
        _importDuration.Record(elapsed.TotalMilliseconds);
}

// Program.cs
builder.Services.AddSingleton<ShowMetrics>();
```

**Watch the tags.** Every distinct combination of tag values is a separate time series stored forever. `source` with four values is fine. A tag holding a show id, a user id, a full URL or an error message is how a monitoring bill goes from tens of pounds to thousands, and how a Prometheus server runs out of memory. Tags are for things with a small, bounded set of values.

### Traces {#traces}

The instrumentation packages already create a span per incoming request, per outbound HTTP call and per database query, and propagate the trace id across service boundaries through the `traceparent` header. You add spans for the parts of your own code that are worth seeing separately.

```cs
using System.Diagnostics;

public sealed class ShowImporter
{
    // The name here is what AddSource("Shows.Api") above subscribes to.
    private static readonly ActivitySource Source = new("Shows.Api");

    public async Task ImportAsync(IReadOnlyList<TvShow> shows, CancellationToken ct)
    {
        using var activity = Source.StartActivity("import shows");
        activity?.SetTag("show.count", shows.Count);

        try
        {
            foreach (var show in shows)
            {
                using var _ = Source.StartActivity("import show");
                await _repo.InsertAsync(show, ct);
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            throw;
        }
    }
}
```

`StartActivity` returns null when nobody is listening, which is why every call is `activity?.`. That null check is the entire cost of instrumentation in a process with tracing turned off.

**Sampling** is how tracing stays affordable. Tracing every request at volume is expensive and mostly redundant; the default parent-based sampler keeps a decision consistent across a whole distributed trace, and head sampling by ratio is set with an environment variable.

```bash
OTEL_TRACES_SAMPLER=parentbased_traceidratio
OTEL_TRACES_SAMPLER_ARG=0.1     # 10% of traces
```

The refinement worth knowing about is **tail sampling** in the collector: buffer each trace briefly, then keep it if it was slow or errored and drop it otherwise. That keeps exactly the traces you would have wanted while discarding the boring 99%.

## Logs {#logs}

Logging is the signal people already have and use worst. Three changes fix most of it.

**Log structured, not interpolated.** The message template with named placeholders is not cosmetic - the placeholders become queryable fields.

```cs
// Yes: ShowId and User arrive as fields you can filter on.
logger.LogInformation("Show {ShowId} deleted by {User}", id, user);

// No: one opaque string, and a needless allocation on every call even when
// the level is disabled.
logger.LogInformation($"Show {id} deleted by {user}");
```

For hot paths, the source generator gives you the same thing with no boxing and no allocation:

```cs
public static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Show {ShowId} deleted by {User}")]
    public static partial void ShowDeleted(this ILogger logger, long showId, string user);
}

logger.ShowDeleted(id, user);
```

**Use scopes for context that spans several lines.** Anything logged inside the scope carries the fields, including from code further down the stack that knows nothing about them.

```cs
using (logger.BeginScope(new Dictionary<string, object>
{
    ["TenantId"] = tenantId,
    ["ImportBatch"] = batchId,
}))
{
    await importer.ImportAsync(shows, ct);
}
```

**Let the trace id do the joining.** With logs going through the OpenTelemetry pipeline, every log record carries the `trace_id` and `span_id` of the request that produced it. That is the link that turns three tools into one workflow: alert fires on a metric, dashboard shows which route, trace shows which call was slow, log lines for that exact trace explain why. Building that link by hand with a correlation id you thread through every method is the thing you no longer have to do.

Two things to get right and then stop thinking about:

- **Levels mean something.** `Information` for events an operator would want in normal running, `Warning` for something recovered from, `Error` for a request that failed, `Critical` for the process being in trouble. If everything is `Information` the level is not carrying information. Default the framework's own categories to `Warning` or your logs are 90% ASP.NET routing chatter.
- **Never log secrets or personal data.** Connection strings, tokens, card numbers, full request bodies, whole exception objects containing user rows. Logs get shipped to third parties, indexed, and kept for a year; treat every log line as public, and see the security notes in [ASP.NET Core](11-aspnet-core.html).

## Prometheus {#prometheus}

[Prometheus](https://prometheus.io/) stores metrics as time series and pulls them from targets over HTTP. It is the default open source metrics store, and it is what Grafana was built to draw.

Two ways to get .NET metrics into it:

1. **Through the collector.** The application speaks OTLP to the OpenTelemetry Collector, and the collector exposes a Prometheus endpoint. One instrumentation path for everything, and the application does not know Prometheus exists. This is the arrangement to prefer.
2. **Directly.** Add `OpenTelemetry.Exporter.Prometheus.AspNetCore` and call `.AddPrometheusExporter()` alongside the OTLP one, then `app.MapPrometheusScrapingEndpoint()` to serve `/metrics`. Simpler when Prometheus is all you have and all you plan to have.

```cs
.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter());

app.MapPrometheusScrapingEndpoint();   // GET /metrics
```

Do not expose `/metrics` to the internet. It is an inventory of your internals - route names, dependency versions, traffic volumes. Bind it to an internal port, or restrict it in nginx as shown in [Containers, nginx and Kubernetes](12-containers-and-hosting.html).

**Names change on the way in.** OpenTelemetry's `http.server.request.duration`, measured in seconds, arrives in Prometheus as `http_server_request_duration_seconds_bucket` and friends. Dots become underscores and the unit joins the name; this trips up every first PromQL query written from the OTel docs.

### Service discovery {#service-discovery}

Prometheus finds targets rather than being told about them, which is what makes it work in an environment where pods come and go. In Kubernetes it queries the API server; elsewhere there is DNS, file based discovery, and cloud provider integrations.

```yaml
scrape_configs:
  - job_name: 'shows-api'
    kubernetes_sd_configs:
      - role: pod
    relabel_configs:
      # Only scrape pods that opted in with an annotation.
      - source_labels: [__meta_kubernetes_pod_annotation_prometheus_io_scrape]
        action: keep
        regex: "true"
      # Carry the namespace and pod name through as labels.
      - source_labels: [__meta_kubernetes_namespace]
        target_label: namespace
      - source_labels: [__meta_kubernetes_pod_name]
        target_label: pod
```

**Exporters** do the same job for things that cannot expose metrics themselves: `node_exporter` for machine metrics, `postgres_exporter`, `redis_exporter`, and `blackbox_exporter` for probing an endpoint from outside.

That last one is worth having even when everything else is instrumented, because it is the only check that tests the whole path a user takes - DNS, TLS, proxy, application - from somewhere that is not the machine you are monitoring.

```yaml
scrape_configs:
  - job_name: 'blackbox'
    metrics_path: /probe
    params:
      module: [http_2xx]
    static_configs:
      - targets:
          - https://shows.example.com/healthz/ready
    relabel_configs:
      - source_labels: [__address__]
        target_label: __param_target
      - source_labels: [__param_target]
        target_label: instance
      - target_label: __address__
        replacement: blackbox-exporter:9115
```

## Grafana {#grafana}

[Grafana](https://grafana.com/) draws the dashboards, over Prometheus and most other stores. Add Prometheus as a data source at `http://prometheus:9090`, then either import a community dashboard from the [dashboards library](https://grafana.com/grafana/dashboards/) - there are good ones for ASP.NET Core and for .NET runtime metrics - or build the four panels that actually get looked at during an incident.

The **RED method** for a request driven service: rate, errors, duration. In PromQL, against the standard ASP.NET metric:

```text
# Rate: requests per second, by route
sum by (http_route) (rate(http_server_request_duration_seconds_count[5m]))

# Errors: proportion of 5xx
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]))
  / sum(rate(http_server_request_duration_seconds_count[5m]))

# Duration: 95th percentile latency, by route
histogram_quantile(0.95,
  sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))
```

Add saturation - CPU, memory, thread pool queue length, database connections in use - and you have the four golden signals on one screen. Resist the urge to add forty more panels. A dashboard nobody can read at 3am is decoration.

Grafana also reads logs from [Loki](https://grafana.com/oss/loki/) and traces from [Tempo](https://grafana.com/oss/tempo/), which is the free way to get the metric → trace → log workflow described above in a single UI. [Jaeger](https://www.jaegertracing.io/) for traces alone and [Seq](https://datalust.co/seq) for structured logs alone are both good and both simpler to run if you only need the one signal.

## Alerting {#alerting}

An alert should mean "a human must do something now". Everything else is a dashboard or a report. Get this wrong and the team learns to ignore the channel, at which point the entire monitoring stack has negative value.

**Alert on symptoms, not causes.** Users care that requests are failing or slow; they do not care that CPU is at 90%, and a rule that fires on CPU will page you during every harmless batch job while missing the outage caused by a deadlock at 5% CPU.

```yaml
groups:
  - name: shows-api
    rules:
      - alert: HighErrorRate
        expr: |
          sum(rate(http_server_request_duration_seconds_count{
                job="shows-api", http_response_status_code=~"5.."}[5m]))
            / sum(rate(http_server_request_duration_seconds_count{job="shows-api"}[5m]))
            > 0.05
        for: 10m
        labels:
          severity: page
        annotations:
          summary: "5% of requests failing for 10 minutes"
          runbook: "https://wiki.example.com/runbooks/shows-api-errors"

      - alert: LatencyRegression
        expr: |
          histogram_quantile(0.95, sum by (le) (
            rate(http_server_request_duration_seconds_bucket{job="shows-api"}[5m]))) > 1.5
        for: 15m
        labels:
          severity: ticket
```

Three details in there do most of the work:

- **`for:`** requires the condition to hold for a sustained period, which is what stops a ten second blip waking anyone.
- **`severity`** separates "wake someone" from "make a ticket". Alertmanager routes on that label. If everything is `page`, nothing is.
- **`runbook`** is a link to what to do about it. An alert without one is a puzzle handed to whoever is least equipped to solve it, at the worst possible hour.

Two rules that should exist on every service and usually do not: an alert for **no data at all** (a service that stopped reporting looks healthy to a rule that only checks error ratios), and an alert on the **certificate expiry** the blackbox exporter reports, because TLS expiry is the most predictable outage there is and still the most common.

Once the basics are in place, express the target as an **SLO** - "99.5% of requests succeed over 30 days" - and alert on the *burn rate* of the error budget rather than an instantaneous threshold. A fast burn pages immediately; a slow burn opens a ticket. It is more setup than the rules above and it is the difference between alerts that track user pain and alerts that track a number someone picked.

## When it is already broken {#live-diagnostics}

Telemetry tells you which process is unhappy. These tools tell you what it is doing right now, and they attach to a running process without a restart, in a container, in production.

```bash
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-dump
dotnet tool install -g dotnet-gcdump
```

```bash
# Live counters: GC, thread pool, exception rate, request rate.
dotnet-counters monitor -p 1 --counters System.Runtime,Microsoft.AspNetCore.Hosting

# 30 seconds of CPU samples, opened in Visual Studio or PerfView or speedscope.
dotnet-trace collect -p 1 --duration 00:00:30 --format speedscope

# A full dump, for the ones that only happen at 4am.
dotnet-dump collect -p 1
dotnet-dump analyze core_20260817

# Heap only, much smaller, for "why is memory climbing".
dotnet-gcdump collect -p 1
```

The three symptoms these answer fastest:

- **Memory climbing and never falling.** `dotnet-gcdump` twice, ten minutes apart, and compare object counts. The type whose count grew is the leak, and it is usually a static collection, an event handler never unsubscribed, or an `HttpClient` per request.
- **CPU pinned at 100%.** `dotnet-trace` for thirty seconds and read the flame graph. Regular expressions, JSON serialisation and accidental `O(n²)` loops account for most of it.
- **Everything slow, CPU idle.** Thread pool starvation: synchronous blocking (`.Result`, `.Wait()`) inside async code, so requests queue for threads that are all parked. `dotnet-counters` shows the queue length climbing while CPU sits idle, which is the signature.

Set `DOTNET_DiagnosticPorts` or run the tools in a sidecar to reach a process inside a container, and make sure the image is not so stripped down that it lacks the diagnostic socket. A production image you cannot attach to is a production image you cannot debug.
