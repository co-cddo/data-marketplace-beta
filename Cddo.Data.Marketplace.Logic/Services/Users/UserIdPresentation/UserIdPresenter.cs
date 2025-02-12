using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Cddo.Data.Marketplace.Logic.Services.Users.UserIdPresentation;

internal class UserIdPresenter(
    IHttpContextAccessor httpContextAccessor) : IUserIdPresenter
{
    async Task<string?> IUserIdPresenter.GetInitiatingUserIdToken()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Try to get the id_token from the Authorization header
        var idToken = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(idToken))
        {
            idToken = await httpContext.GetTokenAsync("id_token");
        }
        
        if (string.IsNullOrEmpty(idToken))
        {
            idToken = httpContext.User?.FindFirst("id_token")?.Value;
        }

        // If id_token is still null, return null
        if (idToken == null)
        {
            return null;
        }

        const string bearerTokenPrefix = "Bearer ";

        // If the id_token starts with "Bearer ", remove the prefix
        if (idToken.StartsWith(bearerTokenPrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            idToken = idToken[bearerTokenPrefix.Length..];
        }

        return idToken;
    }

}