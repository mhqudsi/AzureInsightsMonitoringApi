namespace AzureMoniteringApp.Services.Models
{
    public class EndpointInsight
    {
        public string EndpointName { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int SuccessRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageDurationMs { get; set; }
        public double MaxDurationMs { get; set; }
        public DateTime LastCalled { get; set; }
    }
}
