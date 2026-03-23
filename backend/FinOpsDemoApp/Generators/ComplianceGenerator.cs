using System.Diagnostics.Metrics;
using FinOpsDemoApp.Models;
using FinOpsDemoApp.Services;

namespace FinOpsDemoApp.Generators;

public class ComplianceGenerator : IGenerator
{
    // Starting compliance % per team (procurement starts lowest)
    private static readonly Dictionary<string, double> StartCompliance = new()
    {
        ["platform"] = 82,
        ["data-eng"] = 80,
        ["backend"] = 81,
        ["procurement"] = 74,
    };

    // End compliance % per team after 90 days
    private static readonly Dictionary<string, double> EndCompliance = new()
    {
        ["platform"] = 96,
        ["data-eng"] = 95,
        ["backend"] = 97,
        ["procurement"] = 93,
    };

    private readonly Dictionary<string, double> _liveValues = new();

    public IEnumerable<string> GenerateOpenMetricsLines()
    {
        var rng = new Random(DemoDataConfig.RandomSeed + 2);
        var lines = new List<string>();

        lines.Add("# HELP finops_demo_tagging_compliance_percent Percentage of spend with complete required tags");
        lines.Add("# TYPE finops_demo_tagging_compliance_percent gauge");
        lines.Add("# HELP finops_demo_untagged_cost_eur Daily cost exposure of untagged resources per team");
        lines.Add("# TYPE finops_demo_untagged_cost_eur gauge");

        var start = DemoDataConfig.StartDate;

        for (int day = 0; day <= DemoDataConfig.DaysOfHistory; day++)
        {
            var ts = ToTimestampSec(start.AddDays(day));
            double t = day / (double)DemoDataConfig.DaysOfHistory;

            foreach (var team in DemoDataConfig.Teams)
            {
                var compliance = StartCompliance[team] + (EndCompliance[team] - StartCompliance[team]) * t
                                 + (rng.NextDouble() - 0.5) * 3;
                compliance = Math.Clamp(compliance, 0, 99.5);

                var untaggedFraction = (100 - compliance) / 100.0;
                var untaggedCost = untaggedFraction * (800 + rng.NextDouble() * 1200);

                lines.Add($"finops_demo_tagging_compliance_percent{{team=\"{team}\"}} {compliance:F1} {ts}");
                lines.Add($"finops_demo_untagged_cost_eur{{team=\"{team}\"}} {untaggedCost:F2} {ts}");
            }
        }

        return lines;
    }

    public void RegisterMetrics(Meter meter)
    {
        foreach (var team in DemoDataConfig.Teams)
        {
            _liveValues[$"{team}_compliance"] = EndCompliance[team];
            _liveValues[$"{team}_untagged"] = 50;
            var capturedTeam = team;
            meter.CreateObservableGauge("finops_demo_tagging_compliance_percent",
                () => new Measurement<double>(_liveValues[$"{capturedTeam}_compliance"],
                    new KeyValuePair<string, object?>("team", capturedTeam)),
                "", "Tagging compliance");
            meter.CreateObservableGauge("finops_demo_untagged_cost_eur",
                () => new Measurement<double>(_liveValues[$"{capturedTeam}_untagged"],
                    new KeyValuePair<string, object?>("team", capturedTeam)),
                "", "Untagged cost exposure");
        }
    }

    public void UpdateLiveMetrics()
    {
        var rng = new Random();
        foreach (var team in DemoDataConfig.Teams)
        {
            _liveValues[$"{team}_compliance"] = Math.Clamp(
                _liveValues[$"{team}_compliance"] + (rng.NextDouble() - 0.5) * 0.5, 0, 99.5);
            _liveValues[$"{team}_untagged"] = Math.Max(0,
                _liveValues[$"{team}_untagged"] + (rng.NextDouble() - 0.5) * 10);
        }
    }

    private static long ToTimestampSec(DateTime dt) =>
        (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}
