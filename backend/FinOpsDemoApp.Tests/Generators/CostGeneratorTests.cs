using FinOpsDemoApp.Generators;

namespace FinOpsDemoApp.Tests.Generators;

public class CostGeneratorTests
{
    [Fact]
    public void GenerateOpenMetricsLines_ProducesExpectedLineCount()
    {
        var generator = new CostGenerator();
        var lines = generator.GenerateOpenMetricsLines().ToList();

        // 3 HELP/TYPE headers + (192 cost_eur + 1 platform_total + 1 overall_compliance) × 90 days
        // 192 = 6 services × 4 teams × 2 environments × 4 cost_centers
        var dataLines = lines.Where(l => !l.StartsWith("#")).ToList();
        Assert.Equal((192 + 1 + 1) * 90, dataLines.Count);
    }

    [Fact]
    public void GenerateOpenMetricsLines_NoNegativeValues()
    {
        var generator = new CostGenerator();
        var lines = generator.GenerateOpenMetricsLines()
            .Where(l => !l.StartsWith("#"))
            .ToList();

        foreach (var line in lines)
        {
            var parts = line.Split(' ');
            Assert.True(parts.Length >= 2, $"Malformed line: {line}");
            var value = double.Parse(parts[^2]);
            Assert.True(value >= 0, $"Negative value in line: {line}");
        }
    }

    [Fact]
    public void GenerateOpenMetricsLines_AllCostCentersPresent()
    {
        var generator = new CostGenerator();
        var lines = generator.GenerateOpenMetricsLines()
            .Where(l => l.Contains("finops_demo_cost_eur{"))
            .ToList();

        var expectedCostCenters = new[] { "engineering", "procurement", "finance", "operations" };
        foreach (var cc in expectedCostCenters)
        {
            Assert.Contains(lines, l => l.Contains($"cost_center=\"{cc}\""));
        }
    }

    [Fact]
    public void GenerateOpenMetricsLines_IsDeterministic()
    {
        var gen1 = new CostGenerator();
        var gen2 = new CostGenerator();

        var lines1 = gen1.GenerateOpenMetricsLines().ToList();
        var lines2 = gen2.GenerateOpenMetricsLines().ToList();

        Assert.Equal(lines1.Count, lines2.Count);
        for (int i = 0; i < lines1.Count; i++)
        {
            Assert.Equal(lines1[i], lines2[i]);
        }
    }
}
