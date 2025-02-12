using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Logic.Services.Audit
{
    public class ApplicationInsightsHealthCheckPublisher : IHealthCheckPublisher
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsHealthCheckPublisher(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var healthCheckEvent = new EventTelemetry("HealthCheckReport")
            {
                Properties =
            {
                { "Status", report.Status.ToString() },
                { "TotalDuration", report.TotalDuration.ToString() }
            }
            };

            foreach (var entry in report.Entries)
            {
                healthCheckEvent.Metrics.Add(entry.Key, entry.Value.Status == HealthStatus.Healthy ? 1 : 0);
                healthCheckEvent.Properties.Add($"{entry.Key}_Duration", entry.Value.Duration.ToString());
                healthCheckEvent.Properties.Add($"{entry.Key}_Description", entry.Value.Description ?? "N/A");

                foreach (var data in entry.Value.Data)
                {
                    healthCheckEvent.Properties.Add($"{entry.Key}_{data.Key}", data.Value?.ToString());
                }
            }

            _telemetryClient.TrackEvent(healthCheckEvent);
            _telemetryClient.Flush();

            return Task.CompletedTask;
        }
    }
}
