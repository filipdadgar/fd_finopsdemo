namespace FinOpsDemoApp.Services;

public class SeedGeneratorService(IEnumerable<IGenerator> generators, ILogger<SeedGeneratorService> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        const string outputPath = "/tmp/demo-seed/demo-metrics.openmetrics";
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        logger.LogInformation("Generating demo seed data → {Path}", outputPath);

        await using var writer = new StreamWriter(outputPath);

        foreach (var generator in generators)
        {
            logger.LogInformation("Running generator: {Generator}", generator.GetType().Name);
            foreach (var line in generator.GenerateOpenMetricsLines())
                await writer.WriteLineAsync(line);
        }

        // OpenMetrics EOF marker required by promtool
        await writer.WriteLineAsync("# EOF");

        logger.LogInformation("Seed file written — {Lines} lines total", CountLines(outputPath));
    }

    private static int CountLines(string path)
    {
        int count = 0;
        using var r = new StreamReader(path);
        while (r.ReadLine() != null) count++;
        return count;
    }
}
