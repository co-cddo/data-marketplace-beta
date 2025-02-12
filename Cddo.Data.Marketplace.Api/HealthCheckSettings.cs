using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cddo.Data.Marketplace.Api
{
    public class HealthCheckSettings
    {
        public string HealthCheckUrl { get; set; }
    }
    public class CatalogDataStoreHealthCheck : IHealthCheck
    {
        private readonly string _healthCheckUrl;
        private readonly HttpClient _httpClient;

        public CatalogDataStoreHealthCheck(IOptions<HealthCheckSettings> options, HttpClient httpClient)
        {
            _healthCheckUrl = options.Value.HealthCheckUrl;
            _httpClient = httpClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(_healthCheckUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("The service is healthy.");
                }

                return HealthCheckResult.Unhealthy("The service is unhealthy.");
            }
            catch
            {
                return HealthCheckResult.Unhealthy("The service is unreachable.");
            }
        }
    }
}
