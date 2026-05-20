using AzureMoniteringApp.Services.Insights;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

var authOverrides = new List<KeyValuePair<string, string?>>();

var azureAdSecret = builder.Configuration["AzureAd:ClientSecret"];
var monitorSecret = builder.Configuration["AzureMonitor:ClientSecret"];

var effectiveSecret = string.IsNullOrWhiteSpace(azureAdSecret)
    ? monitorSecret
    : azureAdSecret;

if (!string.IsNullOrWhiteSpace(effectiveSecret))
{
    authOverrides.Add(new KeyValuePair<string, string?>(
        "AzureAd:ClientSecret",
        effectiveSecret));
}

if (string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:TenantId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["AzureMonitor:TenantId"]))
{
    authOverrides.Add(new KeyValuePair<string, string?>(
        "AzureAd:TenantId",
        builder.Configuration["AzureMonitor:TenantId"]));
}

if (string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["AzureMonitor:ClientId"]))
{
    authOverrides.Add(new KeyValuePair<string, string?>(
        "AzureAd:ClientId",
        builder.Configuration["AzureMonitor:ClientId"]));
}

if (authOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(authOverrides);
}

// Authentication for Web API using JWT Bearer tokens
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        builder.Configuration.GetSection("AzureAd"));

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddHttpClient<IAzureInsightsService, AzureInsightsService>();
builder.Services.AddScoped<IAppInsightsInventoryService, AppInsightsInventoryService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();

app.Run();
