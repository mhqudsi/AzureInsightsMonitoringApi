using AzureMoniteringApp.Services.Models;

namespace AzureMoniteringApp.Services.Insights;

public interface IAppInsightsInventoryService
{
    Task<IReadOnlyList<AppInsightsResourceSummary>> ListComponentsAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AzureSubscription>> GetSubscriptions(CancellationToken cancellationToken = default);
}
