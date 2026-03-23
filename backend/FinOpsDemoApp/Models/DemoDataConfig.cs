namespace FinOpsDemoApp.Models;

public static class DemoDataConfig
{
    public static int RandomSeed =>
        int.TryParse(Environment.GetEnvironmentVariable("DEMO_RANDOM_SEED"), out var seed) ? seed : 42;

    public static int DaysOfHistory => 90;
    public static DateTime StartDate => DateTime.UtcNow.Date.AddDays(-DaysOfHistory);

    public static readonly string[] Services =
        ["api-gateway", "data-pipeline", "auth-service", "reporting", "procurement-portal", "billing"];

    public static readonly string[] Teams =
        ["platform", "data-eng", "backend", "procurement"];

    public static readonly string[] Environments =
        ["prod", "staging"];

    public static readonly string[] CostCenters =
        ["engineering", "procurement", "finance", "operations"];

    public static readonly string[] Departments =
        ["engineering", "data", "procurement", "finance"];

    public static readonly string[] RoleCategories =
        ["engineer", "analyst", "lead", "manager"];

    public static readonly string[] Suppliers =
        ["supplier-a", "supplier-b", "supplier-c"];

    public static readonly string[] CostTypes =
        ["hardware", "licence", "services"];

    public static readonly string[] Customers =
        ["customer-alpha", "customer-beta", "customer-gamma"];

    public static readonly string[] ProductLines =
        ["managed-cloud", "data-platform", "analytics"];

    public static readonly string[] Partners =
        ["partner-x", "partner-y"];

    public static readonly string[] Products =
        ["iot-gateway", "edge-compute"];

    // dep-001 to dep-008 from day 0; dep-009 to dep-012 appear after day 45
    public static readonly string[] AllDeployments =
        ["dep-001", "dep-002", "dep-003", "dep-004", "dep-005", "dep-006",
         "dep-007", "dep-008", "dep-009", "dep-010", "dep-011", "dep-012"];

    public static readonly string[] EarlyDeployments =
        ["dep-001", "dep-002", "dep-003", "dep-004", "dep-005", "dep-006", "dep-007", "dep-008"];
}
