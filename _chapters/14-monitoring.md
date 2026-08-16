---
layout: chapter
title: "Monitoring and Observability"
number: 14
part: 5
---

Effective monitoring is essential for Site Reliability Engineering (SRE) and DevOps teams to ensure the health, performance, and reliability of applications and infrastructure. Modern monitoring solutions provide real-time visibility, alerting, and analytics for both system-level and application-level metrics.

## Prometheus Ecosystem

[Prometheus](https://prometheus.io/) is a leading open-source monitoring and alerting toolkit designed for reliability and scalability. It excels at collecting time-series metrics from targets via HTTP endpoints, supports flexible queries, and integrates seamlessly with cloud-native environments.

### Key Features

- **Metrics Collection:** Scrapes metrics from applications, services, and infrastructure.
- **OpenTelemetry Support:** Integrates with [OpenTelemetry](https://opentelemetry.io/) for standardized observability data (metrics, traces, logs).
- **Blackbox Exporter:** Performs external health checks (HTTP, TCP, ICMP) to monitor service availability from the outside.
- **Alerting:** Built-in alert manager for notifications based on custom rules.

### Service Discovery

Prometheus uses **service discovery** to automatically find and monitor targets (applications, services, or infrastructure) without manual configuration. This enables dynamic environments—such as Kubernetes, cloud platforms, or virtual machines—to be monitored as they scale up or down. Prometheus supports various service discovery mechanisms, including static configuration, DNS, file-based discovery, and integrations with cloud providers and orchestration systems.

**Exporters** are lightweight services that expose metrics from third-party systems (like databases, hardware, or messaging queues) in a format Prometheus can scrape. There are many official and community-supported exporters for popular technologies (e.g., node_exporter for system metrics, blackbox_exporter for endpoint probing, mysqld_exporter for MySQL).

**Integrations** refer to the broad ecosystem of tools and exporters that allow Prometheus to collect metrics from virtually any system, making it highly extensible and adaptable to diverse monitoring needs.

- [Prometheus Configuration](https://prometheus.io/docs/prometheus/latest/configuration/configuration/)
- [Exporters and integrations](https://prometheus.io/docs/instrumenting/exporters/)
- [Writing HTTP Service Discovery](https://prometheus.io/docs/prometheus/latest/http_sd/)
    - custom targets SD
- [Use file-based service discovery to discover scrape targets](https://prometheus.io/docs/guides/file-sd/)
    - custom targets SD

### Example: Exposing Metrics in .NET

Add the [prometheus-net](https://github.com/prometheus-net/prometheus-net) NuGet package to your ASP.NET Core app:

```cs
using Prometheus;

app.UseMetricServer(); // Exposes /metrics endpoint
app.UseHttpMetrics();  // Collects HTTP request metrics
```

Prometheus can then scrape metrics from `http://your-service/metrics`.

### Example: Blackbox Exporter Configuration

Monitor an external HTTP endpoint:

```yaml
# prometheus.yml
scrape_configs:
    - job_name: 'blackbox'
        metrics_path: /probe
        params:
            module: [http_2xx]
        static_configs:
            - targets:
                - https://your-service.example.com
        relabel_configs:
            - source_labels: [__address__]
                target_label: __param_target
            - target_label: __address__
                replacement: blackbox-exporter:9115
```

## Grafana for Visualization

[Grafana](https://grafana.com/) is a powerful open-source analytics and monitoring platform. It connects to Prometheus and other data sources to create interactive dashboards, visualizations, and alerts.

Ready to use dashboards for prometheus can be downloaded from [Grafana dashboards page](https://grafana.com/grafana/dashboards/?dataSource=prometheus%2Cnobl9agent%2Cvictorialogs-datasource).

### Example: Prometheus Data Source in Grafana

1. Add Prometheus as a data source in Grafana (URL: `http://prometheus:9090`).
2. Create dashboards using queries like:

```text
http_requests_total{job="myapp"}
up{job="blackbox"}
```

3. Set up alerts for key metrics (e.g., service downtime, high latency).
