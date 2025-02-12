using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.Audit
{
    public class AppInsightsLogger : IAppInsightsLogger
    {
        private readonly TelemetryClient _telemetryClient;

        public AppInsightsLogger(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void LogEvent<TEnum>(TEnum eventType, Dictionary<string, string> properties = null, Dictionary<string, double> metrics = null) where TEnum : Enum
        {
            try
            {
                var eventName = Enum.GetName(typeof(TEnum), eventType);
                _telemetryClient.TrackEvent(eventName, properties, metrics);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        // Method to log errors with exceptions and a custom message
        public void LogError(string format, Exception ex, params object[] args)
        {
            // Ensure args are provided for formatting or use a default message if not
            string message = args.Length > 0 ? string.Format(format, args) : format;

            // Create a dictionary to hold error details
            var properties = new Dictionary<string, string>
            {
                {"Message", message},
                {"ExceptionMessage", ex.Message},
                {"StackTrace", ex.StackTrace}
            };

            // Track the error as a custom event in Application Insights
            _telemetryClient.TrackEvent("Error", properties);
        }

        public void LogInformation(string message)
        {
            _telemetryClient.TrackTrace(message, SeverityLevel.Information);
        }

        public void LogWarning(string message)
        {
            _telemetryClient.TrackTrace(message, SeverityLevel.Warning);
        }

        public void LogEventMainBase<TEnum>(TEnum eventType, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null) where TEnum : Enum
        {
            LoggerExtensions.LogEventMain(this, eventType, pageName, client, action, subject, setting, additionalProperties);
        }

        public void LogAdminEventBase(AdminAuditEvent adminEvent, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null)
        {
            LoggerExtensions.LogEventMain(this, adminEvent, pageName, client, action, subject, setting, additionalProperties);
        }

        public void LogDataAssetValidationErrorBase(
            CatalogAssetField catalogAssetField,
            DataAssetActionSourceEnum actionSource,
            string dataAssetPropertyName,
            string errorMessage,
            DataAssetValidationPropertyErrorSeverity errorSeverity,
            DataAssetPropertyValidationErrorType errorType,
            IUserDetails initiatingUserDetails)
        {
            LoggerExtensions.LogDataAssetValidationError(this, catalogAssetField, actionSource, dataAssetPropertyName, errorMessage, errorSeverity, errorType, initiatingUserDetails);
        }
        public void LogCritical(string message)
        {
            _telemetryClient.TrackTrace(message, SeverityLevel.Critical);
        }
    }
}
