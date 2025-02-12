using Cddo.Data.Marketplace.Audit;
using System.Text.Json;

namespace Cddo.Data.Marketplace.Api.CustomErrors
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAppInsightsLogger _appInsightsLogger;

        public ErrorHandlingMiddleware(RequestDelegate next, IAppInsightsLogger appInsightsLogger)
        {
            _next = next;
            _appInsightsLogger = appInsightsLogger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/datamarketplaceapi/") ||
                context.Request.Path.StartsWithSegments("/cataloged-resources"))
            {
                try
                {
                    await _next(context);
                }
                catch (JsonException ex)
                {
                    // Log the error as a Validation Error
                    var additionalProps = new Dictionary<string, string>
                    {
                        { "ErrorDetails", ex.Message }
                    };

                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, "Middleware", "CDDO", "Error", "IngestionApiError", "", additionalProps);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
