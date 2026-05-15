using AzureMoniteringApp.Services.Insights;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Default AzureAd tenant/client/secret from AzureMonitor when omitted (single app registration).
var authOverrides = new List<KeyValuePair<string, string?>>();
var azureAdSecret = builder.Configuration["AzureAd:ClientSecret"];
var monitorSecret = builder.Configuration["AzureMonitor:ClientSecret"];
var effectiveSecret = string.IsNullOrWhiteSpace(azureAdSecret) ? monitorSecret : azureAdSecret;
if (!string.IsNullOrWhiteSpace(effectiveSecret))
{
    authOverrides.Add(new KeyValuePair<string, string?>("AzureAd:ClientSecret", effectiveSecret));
}

if (string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:TenantId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["AzureMonitor:TenantId"]))
{
    authOverrides.Add(new KeyValuePair<string, string?>("AzureAd:TenantId", builder.Configuration["AzureMonitor:TenantId"]));
}

if (string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["AzureMonitor:ClientId"]))
{
    authOverrides.Add(new KeyValuePair<string, string?>("AzureAd:ClientId", builder.Configuration["AzureMonitor:ClientId"]));
}

if (authOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(authOverrides);
}

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddHttpClient<IAzureInsightsService, AzureInsightsService>();
builder.Services.AddScoped<IAppInsightsInventoryService, AppInsightsInventoryService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
