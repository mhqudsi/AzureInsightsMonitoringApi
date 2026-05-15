namespace AzureMoniteringApp.Services.Models
{
    public class InsightsSummary
    {
        public int TotalRequests { get; set; }
        public int SuccessRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageResponseMs { get; set; }
        public double MinResponseMs { get; set; }
        public double MaxResponseMs { get; set; }
    }
}
