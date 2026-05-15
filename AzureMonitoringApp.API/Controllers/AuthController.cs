using AzureMoniteringApp.Services.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureMonitoringApp.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAzureInsightsService _azureInsightsService;

        public AuthController(IAzureInsightsService azureInsightsService)
        {
            _azureInsightsService = azureInsightsService;
        }

        [HttpGet("gettokken")]
        public async Task<IActionResult> GetTokken(string appId, string? duration)
        {
            return Ok(await _azureInsightsService.GetAccessTokenAsync());
        }
    }
}
