using AzureMoniteringApp.Services.Insights;
using AzureMotinertingApp.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AzureMotinertingApp.UI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IAppInsightsInventoryService _appInsightsInventory;

        public HomeController(IAppInsightsInventoryService appInsightsInventory)
        {
            _appInsightsInventory = appInsightsInventory;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            try
            {
                var resources = await _appInsightsInventory.ListComponentsAsync(cancellationToken);
                return View(new HomeIndexViewModel { ApplicationInsightsResources = resources });
            }
            catch (Exception ex)
            {
                return View(new HomeIndexViewModel
                {
                    ApplicationInsightsResources = Array.Empty<AppInsightsResourceSummary>(),
                    InventoryError = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult Insight(string endpoint)
        {
            ViewBag.Endpoint = endpoint ?? string.Empty;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
