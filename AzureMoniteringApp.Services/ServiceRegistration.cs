using AzureMoniteringApp.Services.Insights;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureMoniteringApp.Services
{
    public static class ServiceRegistration
    {
        public static void ServiceDescriptors(this IServiceCollection services)
        {
            services.AddScoped<IAzureInsightsService, AzureInsightsService>();
        }
    }
}
