using System.Diagnostics.Metrics;
using FinOpsDemoApp.Models;
using FinOpsDemoApp.Services;

namespace FinOpsDemoApp.Generators;

public class HrGenerator : IGenerator
{
    // Base headcount: department → role_category → count
    private static readonly Dictionary<(string dept, string role), (double start, double end)> Headcount = new()
    {
        [("engineering", "engineer")] = (14, 15),
        [("engineering", "lead")] = (2, 2),
        [("engineering", "manager")] = (1, 1),
        [("engineering", "analyst")] = (1, 2),
        [("data", "engineer")] = (3, 4),
        [("data", "analyst")] = (4, 4),
        [("data", "lead")] = (1, 1),
        [("data", "manager")] = (1, 1),
        [("procurement", "analyst")] = (3, 4),
        [("procurement", "lead")] = (1, 1),
        [("procurement", "manager")] = (1, 1),
        [("procurement", "engineer")] = (0, 0),
        [("finance", "analyst")] = (4, 5),
        [("finance", "lead")] = (1, 1),
        [("finance", "manager")] = (1, 1),
        [("finance", "engineer")] = (0, 0),
    };

    // Monthly fully-loaded cost per role category (EUR)
    private static readonly Dictionary<string, double> CostPerRole = new()
    {
        ["engineer"] = 12_000,
        ["analyst"] = 9_500,
        ["lead"] = 13_500,
        ["manager"] = 14_000,
    };

    private readonly Dictionary<string, double> _liveHeadcount = new();
    private readonly Dictionary<string, double> _livePeopleCost = new();

    public IEnumerable<string> GenerateOpenMetricsLines()
    {
        var lines = new List<string>();

        lines.Add("# HELP finops_demo_headcount_active Active FTE headcount per department and role");
        lines.Add("# TYPE finops_demo_headcount_active gauge");
        lines.Add("# HELP finops_demo_people_cost_monthly_eur Fully-loaded monthly people cost per department");
        lines.Add("# TYPE finops_demo_people_cost_monthly_eur gauge");

        var start = DemoDataConfig.StartDate;

        for (int day = 0; day <= DemoDataConfig.DaysOfHistory; day++)
        {
            var ts = ToTimestampSec(start.AddDays(day));
            double t = day / (double)DemoDataConfig.DaysOfHistory;

            var deptCosts = DemoDataConfig.Departments.ToDictionary(d => d, _ => 0.0);

            foreach (var ((dept, role), (startHc, endHc)) in Headcount)
            {
                var hc = Math.Round(startHc + (endHc - startHc) * t);
                lines.Add($"finops_demo_headcount_active{{department=\"{dept}\",role_category=\"{role}\"}} {hc} {ts}");
                deptCosts[dept] += hc * CostPerRole[role];
            }

            foreach (var dept in DemoDataConfig.Departments)
                lines.Add($"finops_demo_people_cost_monthly_eur{{department=\"{dept}\"}} {deptCosts[dept]:F2} {ts}");
        }

        return lines;
    }

    public void RegisterMetrics(Meter meter)
    {
        foreach (var ((dept, role), (_, endHc)) in Headcount)
        {
            var key = $"{dept}|{role}";
            _liveHeadcount[key] = endHc;
            var capturedDept = dept;
            var capturedRole = role;
            meter.CreateObservableGauge("finops_demo_headcount_active",
                () => new Measurement<double>(_liveHeadcount[key],
                    new KeyValuePair<string, object?>("department", capturedDept),
                    new KeyValuePair<string, object?>("role_category", capturedRole)),
                "", "Active headcount");
        }

        foreach (var dept in DemoDataConfig.Departments)
        {
            _livePeopleCost[dept] = Headcount
                .Where(kv => kv.Key.dept == dept)
                .Sum(kv => kv.Value.end * CostPerRole[kv.Key.role]);
            var capturedDept = dept;
            meter.CreateObservableGauge("finops_demo_people_cost_monthly_eur",
                () => new Measurement<double>(_livePeopleCost[capturedDept],
                    new KeyValuePair<string, object?>("department", capturedDept)),
                "", "Monthly people cost");
        }
    }

    public void UpdateLiveMetrics() { } // Headcount doesn't change frequently

    private static long ToTimestampSec(DateTime dt) =>
        (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}
