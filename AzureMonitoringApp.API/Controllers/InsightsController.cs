using AzureMoniteringApp.Services.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureMonitoringApp.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class InsightsController : ControllerBase
    {
        private readonly IAzureInsightsService _azureInsightsService;
        private readonly IAppInsightsInventoryService _azureInsightsInventoryService;

        public InsightsController(IAzureInsightsService azureInsightsService, IAppInsightsInventoryService azureInsightsInventoryService)
        {
            _azureInsightsService = azureInsightsService;
            _azureInsightsInventoryService = azureInsightsInventoryService;
        }
        [HttpGet("summary")]
        public async Task<IActionResult> Summary(string appId, DateTime? fromDate, DateTime? toDate)
        {
            var validation = ValidateDateRange(fromDate, toDate);
            if (validation != null)
            {
                return validation;
            }

            return Ok(await _azureInsightsService.GetSummaryAsync(appId, fromDate!.Value, toDate!.Value));
        }

        [HttpGet("endpoints")]
        public async Task<IActionResult> Endpoints(string appId, DateTime? fromDate, DateTime? toDate)
        {
            var validation = ValidateDateRange(fromDate, toDate);
            if (validation != null)
            {
                return validation;
            }

            return Ok(await _azureInsightsService.GetEndpointInsightsAsync(appId, fromDate!.Value, toDate!.Value));
        }

        private static BadRequestObjectResult? ValidateDateRange(DateTime? fromDate, DateTime? toDate)
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                return new BadRequestObjectResult("fromDate and toDate query parameters are required.");
            }

            if (fromDate.Value > toDate.Value)
            {
                return new BadRequestObjectResult("fromDate must be before or equal to toDate.");
            }

            return null;
        }
        [HttpGet("allinsights")]
        public async Task<IActionResult> AllInsights(string subscriptionId)
        {
            return Ok(await _azureInsightsInventoryService.ListComponentsAsync(subscriptionId));
        }
        [HttpGet("allsubscription")]
        public async Task<IActionResult> AllSubscription(string? duration)
        {
            return Ok(await _azureInsightsInventoryService.GetSubscriptions());
        }
    }
}
