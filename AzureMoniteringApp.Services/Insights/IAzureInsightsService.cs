using AzureMoniteringApp.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureMoniteringApp.Services.Insights
{
    public interface IAzureInsightsService
    {
        Task<InsightsSummary> GetSummaryAsync(string appId, DateTime fromUtc, DateTime toUtc);
        Task<List<EndpointInsight>> GetEndpointInsightsAsync(string appId, DateTime fromUtc, DateTime toUtc);
        Task<string> ExecuteKqlQueryAsync(string appId, string query);
        Task<string> GetAccessTokenAsync();
        Task<List<TelemetryLogEntry>> GetEndpointLogsAsync(
            string appId,
            string endpointName,
            DateTime fromUtc,
            DateTime toUtc);
    }
}
