using FinOpsDemoApp.Generators;
using FinOpsDemoApp.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var mode = Environment.GetEnvironmentVariable("APP_MODE") ?? "exporter";
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Register all generators as IGenerator (multiple registrations)
builder.Services.AddSingleton<CostGenerator>();
builder.Services.AddSingleton<BudgetGenerator>();
builder.Services.AddSingleton<ComplianceGenerator>();
builder.Services.AddSingleton<HrGenerator>();
builder.Services.AddSingleton<ProcurementGenerator>();
builder.Services.AddSingleton<RevenueGenerator>();

builder.Services.AddSingleton<IEnumerable<IGenerator>>(sp => new IGenerator[]
{
    sp.GetRequiredService<CostGenerator>(),
    sp.GetRequiredService<BudgetGenerator>(),
    sp.GetRequiredService<ComplianceGenerator>(),
    sp.GetRequiredService<HrGenerator>(),
    sp.GetRequiredService<ProcurementGenerator>(),
    sp.GetRequiredService<RevenueGenerator>(),
});

builder.Services.AddSingleton<SeedGeneratorService>();

if (mode == "exporter")
{
    // Memory check
    var gcInfo = GC.GetGCMemoryInfo();
    var totalGb = gcInfo.TotalAvailableMemoryBytes / 1_073_741_824.0;
    if (totalGb < 6.0)
        Console.Error.WriteLine($"WARNING: Only {totalGb:F1} GB RAM available. Recommended minimum is 6 GB.");

    builder.Services.AddHostedService<LiveExporterService>();

    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                       ?? "http://otel-collector:4317";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("finops-demo"))
        .WithMetrics(m => m
            .AddMeter("finops.demo")
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", mode }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/", () => $"FinOps Demo App — mode: {mode}");

if (mode == "seed")
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var seedService = app.Services.GetRequiredService<SeedGeneratorService>();
    try
    {
        await seedService.ExecuteAsync(CancellationToken.None);
        logger.LogInformation("Seed complete. Exiting.");
        return 0;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seed failed");
        return 1;
    }
}

await app.RunAsync();
return 0;
