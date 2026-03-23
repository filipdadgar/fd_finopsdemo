using System.Diagnostics.Metrics;

namespace FinOpsDemoApp.Services;

public class LiveExporterService(
    IEnumerable<IGenerator> generators,
    ILogger<LiveExporterService> logger) : BackgroundService
{
    private static readonly Meter DemoMeter = new("finops.demo", "1.0.0");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Live exporter initialising metrics");

        foreach (var generator in generators)
            generator.RegisterMetrics(DemoMeter);

        logger.LogInformation("Live exporter running — updating every 15 seconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var generator in generators)
                generator.UpdateLiveMetrics();

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
