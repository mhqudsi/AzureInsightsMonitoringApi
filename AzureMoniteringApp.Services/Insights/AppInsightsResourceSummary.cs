namespace AzureMoniteringApp.Services.Insights;

/// <summary>
/// Lightweight snapshot of an Application Insights component from Azure Resource Manager.
/// </summary>
public sealed class AppInsightsResourceSummary
{
    public required string Name { get; init; }
    public required string ResourceGroupName { get; init; }
    public required string SubscriptionId { get; init; }
    public string? Location { get; init; }
    /// <summary>Application (instrumentation) GUID used by the Logs / Query API.</summary>
    public string? ApplicationId { get; init; }
    public required string AzurePortalUrl { get; init; }
}
