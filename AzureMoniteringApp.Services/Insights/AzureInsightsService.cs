using Azure.Core;
using Azure.Identity;
using AzureMoniteringApp.Services.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AzureMoniteringApp.Services.Insights
{
    public class AzureInsightsService : IAzureInsightsService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AzureInsightsService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }
        public async Task<string> ExecuteKqlQueryAsync(string appId, string query)
        {
            var token = await GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            var apiKey = _config["AzureMonitor:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }

            var content = new StringContent(
                JsonSerializer.Serialize(new { query }),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"https://api.applicationinsights.io/v1/apps/{appId}/query",
                content);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<EndpointInsight>> GetEndpointInsightsAsync(string appId, DateTime fromUtc, DateTime toUtc)
        {
            var (fromKql, toKql) = ToKqlDateRange(fromUtc, toUtc);
            string query = $@"
let fromDate = datetime({fromKql});
let toDate = datetime({toKql});
requests
| where timestamp >= fromDate and timestamp <= toDate
| summarize
    TotalRequests = count(),
    SuccessRequests = countif(success == true),
    FailedRequests = countif(success == false),
    AverageDurationMs = avg(duration),
    MaxDurationMs = max(duration),
    LastCalled = max(timestamp)
  by name
| order by TotalRequests desc";

            var json = await ExecuteKqlQueryAsync(appId, query);
            using var doc = JsonDocument.Parse(json);
            var rows = doc.RootElement.GetProperty("tables")[0].GetProperty("rows");

            var endpoints = new List<EndpointInsight>();

            foreach (var row in rows.EnumerateArray())
            {
                endpoints.Add(new EndpointInsight
                {
                    EndpointName = row[0].GetString() ?? "Unknown",
                    TotalRequests = row[1].GetInt32(),
                    SuccessRequests = row[2].GetInt32(),
                    FailedRequests = row[3].GetInt32(),
                    AverageDurationMs = row[4].GetDouble(),
                    MaxDurationMs = row[5].GetDouble(),
                    LastCalled = row[6].GetDateTime()
                });
            }

            return endpoints;
        }

        public async Task<InsightsSummary> GetSummaryAsync(string appId, DateTime fromUtc, DateTime toUtc)
        {
            var (fromKql, toKql) = ToKqlDateRange(fromUtc, toUtc);
            string query = $@"
let fromDate = datetime({fromKql});
let toDate = datetime({toKql});
requests
| where timestamp >= fromDate and timestamp <= toDate
| summarize
    TotalRequests = count(),
    SuccessRequests = countif(success == true),
    FailedRequests = countif(success == false),
    AverageResponseMs = avg(duration),
    MinResponseMs = min(duration),
    MaxResponseMs = max(duration)";

            var json = await ExecuteKqlQueryAsync(appId, query);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("tables", out var tables) || tables.ValueKind != JsonValueKind.Array || tables.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No tables found in KQL response.");
            }

            var firstTable = tables[0];

            if (!firstTable.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No rows found in KQL response.");
            }

            var row = rows[0];

            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 6)
            {
                throw new InvalidOperationException("Unexpected row format in KQL response.");
            }

            return new InsightsSummary
            {
                TotalRequests = GetSafeInt(row[0]),
                SuccessRequests = GetSafeInt(row[1]),
                FailedRequests = GetSafeInt(row[2]),
                AverageResponseMs = GetSafeDouble(row[3]),
                MinResponseMs = GetSafeDouble(row[4]),
                MaxResponseMs = GetSafeDouble(row[5])
            };
        }

        private static (string FromKql, string ToKql) ToKqlDateRange(DateTime fromUtc, DateTime toUtc)
        {
            var from = fromUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc)
                : fromUtc.ToUniversalTime();
            var to = toUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(toUtc, DateTimeKind.Utc)
                : toUtc.ToUniversalTime();

            if (from > to)
            {
                throw new ArgumentException("fromDate must be before or equal to toDate.");
            }

            static string Format(DateTime utc) =>
                utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

            return (Format(from), Format(to));
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var credential = new ClientSecretCredential(
                _config["AzureMonitor:TenantId"],
                _config["AzureMonitor:ClientId"],
                _config["AzureMonitor:ClientSecret"]);

            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://api.applicationinsights.io/.default" }));

            return token.Token;
        }

        private int GetSafeInt(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var i) => i,
                JsonValueKind.Number when element.TryGetDouble(out var d) => Convert.ToInt32(d),
                JsonValueKind.String when int.TryParse(element.GetString(), out var i) => i,
                JsonValueKind.String when double.TryParse(
                    element.GetString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var d) => Convert.ToInt32(d),
                _ => 0
            };
        }

        private double GetSafeDouble(JsonElement element)
        {
            double value = element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetDouble(out var d) => d,
                JsonValueKind.String when double.TryParse(
                    element.GetString(),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var d) => d,
                _ => 0d
            };

            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0d;

            return value;
        }
    }
}
