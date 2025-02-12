using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class HealthChecksModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthChecksModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppLifeTimeService _appLifetimeService;
        private readonly IUserRoleService _userRoleService;

        public List<HealthCheckSummary> HealthCheckSummaries { get; set; }

        public HealthChecksModel(IConfiguration configuration, ILogger<HealthChecksModel> logger, IHttpClientFactory httpClientFactory, AppLifeTimeService appLifetimeService, IUserRoleService userRoleService)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appLifetimeService = appLifetimeService;
            HealthCheckSummaries = new List<HealthCheckSummary>();
            _userRoleService = userRoleService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var roles = new List<string> { "System Administrator" };
            bool? isAGMAdministrator = await _userRoleService.IsUserInRoleAsync(roles);
            if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
            {
                var microservices = _configuration.GetSection("Microservices").Get<Dictionary<string, string>>();

                foreach (var microservice in microservices)
                {
                    var url = microservice.Value;
                    var client = _httpClientFactory.CreateClient();

                    var summary = new HealthCheckSummary
                    {
                        Key = microservice.Key,
                        Status = "Unavailable",
                        Duration = "N/A"
                    };

                    try
                    {
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var json = JObject.Parse(content);
                            var results = new List<HealthCheckResult>();

                            var details = json["details"];
                            if (details != null && details.HasValues)
                            {
                                foreach (var detail in details)
                                {
                                    var healthCheckResult = new HealthCheckResult
                                    {
                                        Timestamp = DateTime.UtcNow,
                                        Status = detail["status"].ToString(),
                                        TotalDuration = json["totalDuration"]?.ToString(),
                                        Key = detail["key"].ToString(),
                                        Description = detail["description"]?.ToString(),
                                        Data = detail["data"]?.ToObject<Dictionary<string, object>>()
                                    };
                                    results.Add(healthCheckResult);
                                }
                            }

                            summary.Status = json["status"]?.ToString();
                            summary.Duration = CalculateDuration(json["status"]?.ToString(), microservice.Key);
                            summary.HealthCheckResults = results;
                        }
                        else
                        {
                            _logger.LogError($"Failed to fetch health check data for {microservice.Key}: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Exception occurred while fetching health check data for {microservice.Key}: {ex.Message}");
                    }

                    HealthCheckSummaries.Add(summary);
                }

                return Page();
            }

            return RedirectToPage("/Error/403");
        }

        private string CalculateDuration(string status, string key)
        {
            if (status == "Healthy")
            {
                var duration = DateTime.UtcNow - _appLifetimeService.StartupTime;
                return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
            }

            return "Just now";
        }

        public class HealthCheckResult
        {
            public DateTime Timestamp { get; set; }
            public string Status { get; set; }
            public string TotalDuration { get; set; }
            public string Key { get; set; }
            public string Description { get; set; }
            public Dictionary<string, object> Data { get; set; }
        }

        public class HealthCheckSummary
        {
            public string Key { get; set; }
            public string Status { get; set; }
            public string Duration { get; set; }
            public DateTime LastChecked { get; set; } = DateTime.UtcNow;
            public List<HealthCheckResult> HealthCheckResults { get; set; } = new List<HealthCheckResult>();
        }
    }
}
