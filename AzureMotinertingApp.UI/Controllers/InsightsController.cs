using AzureMoniteringApp.Services.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureMotinertingApp.UI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/insights")]
    public class InsightsController : ControllerBase
    {
        private readonly IAzureInsightsService _service;

        public InsightsController(IAzureInsightsService service)
        {
            _service = service;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(string appId, DateTime? fromDate, DateTime? toDate)
        {
            var validation = ValidateDateRange(fromDate, toDate);
            if (validation != null)
            {
                return validation;
            }

            return Ok(await _service.GetSummaryAsync(appId, fromDate!.Value, toDate!.Value));
        }

        [HttpGet("endpoints")]
        public async Task<IActionResult> Endpoints(string appId, DateTime? fromDate, DateTime? toDate)
        {
            var validation = ValidateDateRange(fromDate, toDate);
            if (validation != null)
            {
                return validation;
            }

            return Ok(await _service.GetEndpointInsightsAsync(appId, fromDate!.Value, toDate!.Value));
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
    }
}
