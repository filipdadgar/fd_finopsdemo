# FinOps Platform POC — Demo Stack

A showcase demo for the enterprise FinOps strategy. Pre-seeded with 90 days of synthetic data across Finance, HR, Procurement, and Cloud Costs.

## Prerequisites

| Requirement | Minimum Version | Notes |
|-------------|----------------|-------|
| Docker | 24.0+ | Desktop or Engine |
| Docker Compose | v2.1+ | Plugin or standalone |
| RAM | 4 GB available | 6 GB recommended |
| Disk | 2 GB free | For Prometheus TSDB blocks |

## Start the Demo

```bash
docker compose -f poc/docker-compose.yml up --build
```

Wait ~60 seconds for all services to become healthy, then open Grafana:

**http://localhost:3000** — no login required

## Port Reference

| Port | Service | Purpose |
|------|---------|---------|
| 3000 | Grafana | Dashboards (open access) |
| 9090 | Prometheus | Metrics query UI |
| 8080 | FinOps Exporter | Health endpoint |
| 4317 | OTEL Collector | OTLP gRPC receiver |
| 8889 | OTEL Collector | Prometheus scrape endpoint |

## Dashboards

| # | Dashboard | Story |
|---|-----------|-------|
| 01 | Cost Overview | Cloud cost by service, team, environment, cost centre |
| 02 | Budget Tracking | MTD spend vs budget — procurement overshoot |
| 03 | Tagging Compliance | Compliance trend rising from 74% → 97% |
| 04 | HR Attribution | Headcount & people cost by department |
| 05 | Procurement | Supplier costs, customer revenue, gross profit |
| 06 | Revenue & Partner Billing | Per-deployment cost vs billed, customer margins |
| 07 | Operations | Platform total cost, all cost centres overview |

## Stop

```bash
docker compose -f poc/docker-compose.yml down
```

## Full Reset (wipe all data)

```bash
docker compose -f poc/docker-compose.yml down --volumes
```

After a full reset, re-run `up --build` to reseed Prometheus with 90 days of history.

## Restart (no rebuild)

```bash
docker compose -f poc/docker-compose.yml stop
docker compose -f poc/docker-compose.yml start
```

Dashboards repopulate within ~30 seconds.

## Configuration

| Environment Variable | Default | Description |
|---------------------|---------|-------------|
| `DEMO_RANDOM_SEED` | `42` | Seed for deterministic data generation |

Set in `docker-compose.yml` or override with `-e DEMO_RANDOM_SEED=123`.


## Architecture

```
C# App (seed mode) → OpenMetrics file → promtool → Prometheus TSDB blocks
C# App (exporter mode) → OTLP gRPC → OTEL Collector → Prometheus scrape → Grafana
```

The demo starts with 90 days of pre-seeded historical data. The exporter then emits live metrics every 15 seconds to show the "current" state.
