namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class JwtToBearerMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtToBearerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Clear any pre-existing Authorization header
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Request.Headers.Remove("Authorization");
            }

            // Check if the custom JWT token exists in the cookie
            if (context.Request.Cookies.TryGetValue("CO-Datamarketplace", out var customToken))
            {
                // Add the JWT token to the Authorization header as a Bearer token
                context.Request.Headers.Add("Authorization", $"Bearer {customToken}");
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
