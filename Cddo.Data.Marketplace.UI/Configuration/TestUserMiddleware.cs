using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class TestUserMiddleware
    {
        private readonly RequestDelegate _next;

        public TestUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var config = context.RequestServices.GetRequiredService<IConfiguration>();
            if (config.GetValue<bool>("TestUser:Enabled"))
            {
                var testUsername = config.GetValue<string>("TestUser:Username");

                if (context.Request.Query.ContainsKey("testuser") &&
                    context.Request.Query["testuser"] == testUsername)
                {
                    var idToken = TokenHelper.GenerateMockToken(config, testUsername, config.GetValue<string>("TestUser:Email"));

                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, testUsername),
                    new Claim(ClaimTypes.Email, config.GetValue<string>("TestUser:Email")),
                    new Claim("display_name", config.GetValue<string>("TestUser:DisplayName")),
                    new Claim("id_token", idToken)
                };

                    var identity = new ClaimsIdentity(claims, "TestUser");
                    var principal = new ClaimsPrincipal(identity);

                    var authProperties = new AuthenticationProperties();
                    authProperties.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                    context.Response.Redirect("/");
                    return;
                }
            }

            await _next(context);
        }
    }
}
