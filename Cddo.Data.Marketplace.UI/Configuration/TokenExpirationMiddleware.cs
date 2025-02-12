using System.IdentityModel.Tokens.Jwt;

namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class TokenExpirationMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenExpirationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the custom JWT token exists in the cookie
            if (context.Request.Cookies.TryGetValue("CO-Datamarketplace", out var token))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                try
                {
                    // Validate the token (without checking signature, just parse it)
                    var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
                    if (jwtToken != null)
                    {
                        var expirationDate = jwtToken.ValidTo;

                        // Check if the token has expired
                        if (DateTime.UtcNow >= expirationDate)
                        {
                            // Remove the cookie if the token is expired
                            context.Response.Cookies.Delete("CO-Datamarketplace");

                            // Redirect the user to re-authenticate
                            context.Response.Redirect("/auth/signin");
                            return; // Short-circuit the pipeline
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any token parsing errors
                    context.Response.Cookies.Delete("CO-Datamarketplace");
                    context.Response.Redirect("/auth/signin");
                    return;
                }
            }

            // If everything is fine, proceed to the next middleware
            await _next(context);
        }
    }

}
