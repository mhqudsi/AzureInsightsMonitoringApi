using AzureMoniteringApp.Services.Insights;

namespace AzureMotinertingApp.UI.Models;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<AppInsightsResourceSummary> ApplicationInsightsResources { get; init; } =
        Array.Empty<AppInsightsResourceSummary>();

    public string? InventoryError { get; init; }
}
