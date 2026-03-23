using System.Diagnostics.Metrics;

namespace FinOpsDemoApp.Services;

public interface IGenerator
{
    IEnumerable<string> GenerateOpenMetricsLines();
    void RegisterMetrics(Meter meter);
    void UpdateLiveMetrics();
}
