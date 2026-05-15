using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using Azure.ResourceManager.Resources;
using AzureMoniteringApp.Services.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AzureMoniteringApp.Services.Insights;

public sealed class AppInsightsInventoryService : IAppInsightsInventoryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppInsightsInventoryService> _logger;

    public AppInsightsInventoryService(IConfiguration configuration, ILogger<AppInsightsInventoryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AppInsightsResourceSummary>> ListComponentsAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        subscriptionId = ResolveSubscriptionId(subscriptionId);
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new InvalidOperationException(
                "Configure AzureMonitor:SubscriptionId or AzureMonitor:ResourceUri containing /subscriptions/{guid}/.");
        }

        var tenantId = _configuration["AzureMonitor:TenantId"];
        var clientId = _configuration["AzureMonitor:ClientId"];
        var clientSecret = _configuration["AzureMonitor:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("AzureMonitor:TenantId, ClientId, and ClientSecret must be configured.");
        }

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var armClient = new ArmClient(credential);

        var subscription = armClient.GetSubscriptionResource(
            SubscriptionResource.CreateResourceIdentifier(subscriptionId.Trim()));

        var results = new List<AppInsightsResourceSummary>();

        await foreach (var component in subscription.GetApplicationInsightsComponentsAsync(cancellationToken))
        {
            var data = component.Data;
            var name = data.Name;
            var rgName = component.Id.ResourceGroupName ?? string.Empty;
            var appId = data.AppId?.ToString() ?? data.ApplicationId;

            var portalUrl =
                $"https://portal.azure.com/#resource/subscriptions/{subscriptionId.Trim()}/resourceGroups/{Uri.EscapeDataString(rgName)}/providers/microsoft.insights/components/{Uri.EscapeDataString(name)}";

            results.Add(new AppInsightsResourceSummary
            {
                Name = name,
                ResourceGroupName = rgName,
                SubscriptionId = subscriptionId.Trim(),
                Location = data.Location.ToString(),
                ApplicationId = appId,
                AzurePortalUrl = portalUrl
            });
        }

        results.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        _logger.LogInformation("Listed {Count} Application Insights components in subscription {SubscriptionId}.", results.Count, subscriptionId);

        return results;
    }

    public async Task<IReadOnlyList<AzureSubscription>> GetSubscriptions(CancellationToken cancellationToken = default)
    {

        // Authenticate using DefaultAzureCredential
        // This works if you are logged in via Azure CLI or using environment variables
        //var credential = new DefaultAzureCredential();
        var tenantId = _configuration["AzureMonitor:TenantId"];
        var clientId = _configuration["AzureMonitor:ClientId"];
        var clientSecret = _configuration["AzureMonitor:ClientSecret"];
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        var armClient = new ArmClient(credential);

        var subscriptions = new List<AzureSubscription>();

        await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync())
        {
            subscriptions.Add(new AzureSubscription
            {
                Id = subscription.Id.ToString(),
                DisplayName = subscription.Data?.DisplayName,
                State = subscription.Data?.State?.ToString()
            });
        }

        return subscriptions;

    }

    private string? ResolveSubscriptionId(string subscriptionId)
    {
        string pattern = @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b";

        MatchCollection matches = Regex.Matches(subscriptionId, pattern);

        foreach (Match match in matches)
        {
            return match.Value;
        }

        return string.Empty;
    }
}
