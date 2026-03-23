using System.Diagnostics.Metrics;
using FinOpsDemoApp.Models;
using FinOpsDemoApp.Services;

namespace FinOpsDemoApp.Generators;

public class BudgetGenerator : IGenerator
{
    private static readonly Dictionary<string, double> MonthlyBudgets = new()
    {
        ["engineering"] = 85_000,
        ["procurement"] = 50_000,
        ["finance"] = 40_000,
        ["operations"] = 35_000,
    };

    private readonly Dictionary<string, double> _mtdValues = new();

    public IEnumerable<string> GenerateOpenMetricsLines()
    {
        var rng = new Random(DemoDataConfig.RandomSeed + 1);
        var lines = new List<string>();

        lines.Add("# HELP finops_demo_budget_monthly_eur Finance-approved monthly budget per cost centre");
        lines.Add("# TYPE finops_demo_budget_monthly_eur gauge");
        lines.Add("# HELP finops_demo_budget_spend_mtd_eur Month-to-date actual spend per cost centre");
        lines.Add("# TYPE finops_demo_budget_spend_mtd_eur gauge");

        var start = DemoDataConfig.StartDate;

        // Track MTD per month per cost centre
        var mtdAccumulators = DemoDataConfig.CostCenters.ToDictionary(cc => cc, _ => 0.0);
        int currentMonth = -1;

        for (int day = 0; day <= DemoDataConfig.DaysOfHistory; day++)
        {
            var date = start.AddDays(day);
            var ts = ToTimestampSec(date);

            if (date.Month != currentMonth)
            {
                currentMonth = date.Month;
                foreach (var cc in DemoDataConfig.CostCenters)
                    mtdAccumulators[cc] = 0;
            }

            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            foreach (var cc in DemoDataConfig.CostCenters)
            {
                var budget = MonthlyBudgets[cc];
                // Procurement overshoots to 105% by end of demo
                double targetRatio = cc == "procurement" && day > 60 ? 1.05 : cc == "engineering" ? 0.78 : 0.72;
                var dailySpend = (budget * targetRatio / daysInMonth) * (1 + (rng.NextDouble() - 0.5) * 0.1);
                mtdAccumulators[cc] += dailySpend;

                lines.Add($"finops_demo_budget_monthly_eur{{cost_center=\"{cc}\"}} {budget:F2} {ts}");
                lines.Add($"finops_demo_budget_spend_mtd_eur{{cost_center=\"{cc}\"}} {mtdAccumulators[cc]:F2} {ts}");
            }
        }

        return lines;
    }

    public void RegisterMetrics(Meter meter)
    {
        foreach (var cc in DemoDataConfig.CostCenters)
        {
            _mtdValues[cc] = MonthlyBudgets[cc] * 0.5;
            var capturedCc = cc;
            meter.CreateObservableGauge("finops_demo_budget_monthly_eur",
                () => new Measurement<double>(MonthlyBudgets[capturedCc],
                    new KeyValuePair<string, object?>("cost_center", capturedCc)),
                "", "Monthly budget");
            meter.CreateObservableGauge("finops_demo_budget_spend_mtd_eur",
                () => new Measurement<double>(_mtdValues[capturedCc],
                    new KeyValuePair<string, object?>("cost_center", capturedCc)),
                "", "Month-to-date spend");
        }
    }

    public void UpdateLiveMetrics()
    {
        var rng = new Random();
        foreach (var cc in DemoDataConfig.CostCenters)
        {
            var dailyRate = MonthlyBudgets[cc] / 30.0 / (24 * 4); // approx per 15s
            _mtdValues[cc] += dailyRate * (1 + (rng.NextDouble() - 0.5) * 0.1);
        }
    }

    private static long ToTimestampSec(DateTime dt) =>
        (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}
