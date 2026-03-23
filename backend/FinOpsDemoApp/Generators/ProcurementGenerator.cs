using System.Diagnostics.Metrics;
using FinOpsDemoApp.Models;
using FinOpsDemoApp.Services;

namespace FinOpsDemoApp.Generators;

public class ProcurementGenerator : IGenerator
{
    // Monthly supplier costs (EUR): supplier → cost_type → base monthly cost
    private static readonly Dictionary<(string supplier, string costType), double> SupplierCosts = new()
    {
        [("supplier-a", "services")] = 45_000,
        [("supplier-b", "hardware")] = 28_000,
        [("supplier-c", "licence")] = 15_000,
    };

    // Monthly customer revenue (EUR): customer → product_line → base monthly revenue
    private static readonly Dictionary<(string customer, string product), double> CustomerRevenue = new()
    {
        [("customer-alpha", "managed-cloud")] = 72_000,
        [("customer-beta", "data-platform")] = 51_000,
        [("customer-gamma", "analytics")] = 38_000,
    };

    private readonly Dictionary<string, double> _liveSupplier = new();
    private readonly Dictionary<string, double> _liveRevenue = new();

    public IEnumerable<string> GenerateOpenMetricsLines()
    {
        var lines = new List<string>();

        lines.Add("# HELP finops_demo_supplier_cost_eur Monthly cost paid by Coretura to suppliers");
        lines.Add("# TYPE finops_demo_supplier_cost_eur gauge");
        lines.Add("# HELP finops_demo_customer_revenue_eur Monthly revenue received from customers");
        lines.Add("# TYPE finops_demo_customer_revenue_eur gauge");

        var start = DemoDataConfig.StartDate;

        for (int day = 0; day <= DemoDataConfig.DaysOfHistory; day++)
        {
            var date = start.AddDays(day);
            var ts = ToTimestampSec(date);
            // Use same monthly noise factor so values are stable within a month
            var monthSeed = date.Year * 100 + date.Month;
            var noise = 1.0 + (new Random(DemoDataConfig.RandomSeed + 4 + monthSeed).NextDouble() - 0.5) * 0.1;

            foreach (var ((supplier, costType), baseCost) in SupplierCosts)
                lines.Add($"finops_demo_supplier_cost_eur{{supplier=\"{supplier}\",cost_type=\"{costType}\"}} {baseCost * noise:F2} {ts}");

            foreach (var ((customer, product), baseRevenue) in CustomerRevenue)
                lines.Add($"finops_demo_customer_revenue_eur{{customer=\"{customer}\",product_line=\"{product}\"}} {baseRevenue * noise:F2} {ts}");
        }

        return lines;
    }

    public void RegisterMetrics(Meter meter)
    {
        foreach (var ((supplier, costType), baseCost) in SupplierCosts)
        {
            var key = $"{supplier}|{costType}";
            _liveSupplier[key] = baseCost;
            var capturedSupplier = supplier;
            var capturedType = costType;
            meter.CreateObservableGauge("finops_demo_supplier_cost_eur",
                () => new Measurement<double>(_liveSupplier[key],
                    new KeyValuePair<string, object?>("supplier", capturedSupplier),
                    new KeyValuePair<string, object?>("cost_type", capturedType)),
                "", "Monthly supplier cost");
        }

        foreach (var ((customer, product), baseRevenue) in CustomerRevenue)
        {
            var key = $"{customer}|{product}";
            _liveRevenue[key] = baseRevenue;
            var capturedCustomer = customer;
            var capturedProduct = product;
            meter.CreateObservableGauge("finops_demo_customer_revenue_eur",
                () => new Measurement<double>(_liveRevenue[key],
                    new KeyValuePair<string, object?>("customer", capturedCustomer),
                    new KeyValuePair<string, object?>("product_line", capturedProduct)),
                "", "Monthly customer revenue");
        }
    }

    public void UpdateLiveMetrics() { } // Monthly figures — no live drift needed

    private static long ToTimestampSec(DateTime dt) =>
        (long)(dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
}
