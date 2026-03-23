using System.Diagnostics.Metrics;
using System.Text;
using FinOpsDemoApp.Models;
using FinOpsDemoApp.Services;

namespace FinOpsDemoApp.Generators;

public class CostGenerator : IGenerator
{
    private readonly Random _rng = new(DemoDataConfig.RandomSeed);
    private readonly Dictionary<string, ObservableGauge<double>> _gauges = new();
    private readonly Dictionary<string, double> _currentValues = new();

    public IEnumerable<string> GenerateOpenMetricsLines()
    {
        var rng = new Random(DemoDataConfig.RandomSeed);
        var lines = new List<string>();

        lines.Add("# HELP finops_demo_cost_eur Daily cloud infrastructure cost in EUR");
        lines.Add("# TYPE finops_demo_cost_eur gauge");
        lines.Add("# HELP finops_demo_platform_cost_total_eur Total daily platform cost across all cost centres");
        lines.Add("# TYPE finops_demo_platform_cost_total_eur gauge");
        lines.Add("# HELP finops_demo_overall_compliance_percent Overall tagging compliance across all teams (percent)");
        lines.Add("# TYPE finops_demo_overall_compliance_percent gauge");

        var start = DemoDataConfig.StartDate;

        for (int day = 0; day <= DemoDataConfig.DaysOfHistory; day++)
        {
            var ts = ToTimestampSec(start.AddDays(day));
            double dayTotal = 0;

            foreach (var service in DemoDataConfig.Services)
            foreach (var team in DemoDataConfig.Teams)
            foreach (var env in DemoDataConfig.Environments)
            foreach (var cc in DemoDataConfig.CostCenters)
            {
                var baseCost = 200 + rng.NextDouble() * 2800;
                var trend = 1.0 + (day / (double)DemoDataConfig.DaysOfHistory * 0.15);
                var noise = 1.0 + (rng.NextDouble() - 0.5) * 0.2;
                var cost = baseCost * trend * noise;

                // Procurement portal goes in procurement cost centre
                var effectiveCc = service == "procurement-portal" ? "procurement" : cc;

                lines.Add($"finops_demo_cost_eur{{service=\"{service}\",team=\"{team}\",environment=\"{env}\",cost_center=\"{effectiveCc}\"}} {cost:F2} {ts}");
                dayTotal += cost;
            }

            // Platform total
            lines.Add($"finops_demo_platform_cost_total_eur {dayTotal:F2} {ts}");

            // Overall compliance: starts ~79%, rises to ~95%
            var compliance = 79.0 + (day / (double)DemoDataConfig.DaysOfHistory * 16.0) + (rng.NextDouble() - 0.5) * 2;
            compliance = Math.Clamp(compliance, 0, 100);
            lines.Add($"finops_demo_overall_compliance_percent {compliance:F1} {ts}");
        }

        return lines;
    }

    public void RegisterMetrics(Meter meter)
    {
        foreach (var service in DemoDataConfig.Services)
        foreach (var team in DemoDataConfig.Teams)
        foreach (var env in DemoDataConfig.Environments)
        foreach (var cc in DemoDataConfig.CostCenters)
        {
            var key = $"{service}|{team}|{env}|{cc}";
            _currentValues[key] = 500 + _rng.NextDouble() * 2000;
            meter.CreateObservableGauge("finops_demo_cost_eur",
                () => _currentValues.TryGetValue(key, out var v) ? new Measurement<double>(v,
                    new KeyValuePair<string, object?>("service", service),
                    new KeyValuePair<string, object?>("team", team),
                    new KeyValuePair<string, object?>("environment", env),
                    new KeyValuePair<string, object?>("cost_center", cc)) : new Measurement<double>(0),
                "", "Daily cloud infrastructure cost");
        }
    }

    public void UpdateLiveMetrics()
    {
        var rng = new Random();
        foreach (var key in _currentValues.Keys.ToList())
        {
            var current = _currentValues[key];
            _currentValues[key] = current * (1.0 + (rng.NextDouble() - 0.5) * 0.05);
        }
    }

    private static long ToTimestampSec(DateTime dt) =>
        (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}
