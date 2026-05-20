using System;
using System.Collections.Generic;
using System.Text;

namespace AzureMoniteringApp.Services.Models
{
    public class TelemetryLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string TelemetryType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool? Success { get; set; }
        public string? ResultCode { get; set; }
        public double? DurationMs { get; set; }
        public string? OperationId { get; set; }
        public string? Url { get; set; }
    }
}
