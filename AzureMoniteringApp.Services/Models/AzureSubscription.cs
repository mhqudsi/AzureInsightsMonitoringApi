using System;
using System.Collections.Generic;
using System.Text;

namespace AzureMoniteringApp.Services.Models
{
    public class AzureSubscription
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }
}
