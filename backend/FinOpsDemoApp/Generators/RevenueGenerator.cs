using System.Diagnostics.Metrics;
using FinOpsDemoApp.Models;
using FinOpsDemoApp.Services;

namespace FinOpsDemoApp.Generators;

public class RevenueGenerator : IGenerator
{
    // Deployment cost and billing (EUR/month)
    private static readonly Dictionary<string, (string partner, string product, double cost, double billed)> Deployments = new()
    {
        ["dep-001"] = ("partner-x", "iot-gateway", 950, 1_750),
        ["dep-002"] = ("partner-x", "iot-gateway", 920, 1_700),
        ["dep-003"] = ("partner-x", "edge-compute", 1_100, 2_100),
        ["dep-004"] = ("partner-x", "edge-compute", 1_080, 2_050),
        ["dep-005"] = ("partner-y", "iot-gateway", 960, 1_800),
        ["dep-006"] = ("partner-y", "iot-gateway", 940, 1_750),
        ["dep-007"] = ("partner-y", "edge-compute", 1_120, 2_200),
        ["dep-008"] = ("partner-y", "edge-compute", 1_090, 2_100),
        // Added after day 45:
        ["dep-009"] = ("partner-x", "iot-gateway", 970, 1_780),
        ["dep-010"] = ("partner-x", "edge-compute", 1_050, 2_000),
        ["dep-011"] = ("partner-y", "iot-gateway", 980, 1_820),
        ["dep-012"] = ("partner-y", "edge-compute", 1_070, 2_050),
    };

    private static readonly Dictionary<string, double> CustomerGrossMargin = new()
    {
        ["customer-alpha"] = 38.0,
        ["customer-beta"] = 45.0,
        ["customer-gamma"] = 62.0,
    };

    private readonly Dictionary<string, double> _liveCost = new();
    private readonly Dictionary<string, double> _liveBilled = new();

    public IEnumerable<string> GenerateOpenMetricsLines()
    {
        var rng = new Random(DemoDataConfig.RandomSeed + 5);
        var lines = new List<string>();

        lines.Add("# HELP finops_demo_deployment_cost_eur Monthly cost per partner deployment instance");
        lines.Add("# TYPE finops_demo_deployment_cost_eur gauge");
        lines.Add("# HELP finops_demo_deployment_billed_eur Monthly billed amount per partner deployment instance");
        lines.Add("# TYPE finops_demo_deployment_billed_eur gauge");
        lines.Add("# HELP finops_demo_gross_margin_percent Gross margin percentage per customer");
        lines.Add("# TYPE finops_demo_gross_margin_percent gauge");

        var start = DemoDataConfig.StartDate;

        for (int day = 0; day <= DemoDataConfig.DaysOfHistory; day++)
        {
            var date = start.AddDays(day);
            var ts = ToTimestampSec(date);
            var monthSeed = date.Year * 100 + date.Month;
            var noise = 1.0 + (new Random(DemoDataConfig.RandomSeed + 5 + monthSeed).NextDouble() - 0.5) * 0.05;

            foreach (var (depId, (partner, product, cost, billed)) in Deployments)
            {
                // Late deployments only appear after day 45
                if (int.Parse(depId.Split('-')[1]) > 8 && day < 45) continue;

                lines.Add($"finops_demo_deployment_cost_eur{{partner=\"{partner}\",product=\"{product}\",deployment_id=\"{depId}\"}} {cost * noise:F2} {ts}");
                lines.Add($"finops_demo_deployment_billed_eur{{partner=\"{partner}\",product=\"{product}\",deployment_id=\"{depId}\"}} {billed * noise:F2} {ts}");
            }

            foreach (var (customer, margin) in CustomerGrossMargin)
            {
                var m = margin + (rng.NextDouble() - 0.5) * 2;
                lines.Add($"finops_demo_gross_margin_percent{{customer=\"{customer}\"}} {m:F1} {ts}");
            }
        }

        return lines;
    }

    public void RegisterMetrics(Meter meter)
    {
        foreach (var (depId, (partner, product, cost, billed)) in Deployments)
        {
            _liveCost[depId] = cost;
            _liveBilled[depId] = billed;
            var capturedDep = depId;
            var capturedPartner = partner;
            var capturedProduct = product;
            meter.CreateObservableGauge("finops_demo_deployment_cost_eur",
                () => new Measurement<double>(_liveCost[capturedDep],
                    new KeyValuePair<string, object?>("partner", capturedPartner),
                    new KeyValuePair<string, object?>("product", capturedProduct),
                    new KeyValuePair<string, object?>("deployment_id", capturedDep)),
                "", "Deployment cost");
            meter.CreateObservableGauge("finops_demo_deployment_billed_eur",
                () => new Measurement<double>(_liveBilled[capturedDep],
                    new KeyValuePair<string, object?>("partner", capturedPartner),
                    new KeyValuePair<string, object?>("product", capturedProduct),
                    new KeyValuePair<string, object?>("deployment_id", capturedDep)),
                "", "Deployment billed");
        }

        foreach (var (customer, margin) in CustomerGrossMargin)
        {
            var capturedCustomer = customer;
            var capturedMargin = margin;
            meter.CreateObservableGauge("finops_demo_gross_margin_percent",
                () => new Measurement<double>(capturedMargin,
                    new KeyValuePair<string, object?>("customer", capturedCustomer)),
                "", "Gross margin");
        }
    }

    public void UpdateLiveMetrics() { }

    private static long ToTimestampSec(DateTime dt) =>
        (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}
