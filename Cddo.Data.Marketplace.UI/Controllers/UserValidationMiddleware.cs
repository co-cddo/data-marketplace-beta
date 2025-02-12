using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Cddo.Data.Marketplace.UI.Controllers
{
    public class UserValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserValidationMiddleware> _logger;

        public UserValidationMiddleware(RequestDelegate next, ILogger<UserValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserRoleService userRoleService)
        {
            if (IsAnonymousAccess(context) || IsExcludedPath(context))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity!.IsAuthenticated)
            {
                var validationResult = await ValidateUserProfileAsync(context, userRoleService);
                if (!validationResult)
                {
                    RedirectToNotAllowed(context);
                    return;
                }
            }
            else
            {
                _logger.LogInformation("Unauthenticated request.");
            }

            await _next(context);
        }

        private static bool IsAnonymousAccess(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            return endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
        }

        private static bool IsExcludedPath(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLowerInvariant();
            return path.StartsWith("/error") || path.Equals("/error/notallowedyet") ||
                   path.StartsWith("/support") || path.StartsWith("/organisationaccess");
        }

        private async Task<bool> ValidateUserProfileAsync(HttpContext context, IUserRoleService userRoleService)
        {
            try
            {
                var userProfile = await userRoleService.GetUserProfileAsync();

                if (userProfile == null || userProfile.Organisation == null || userProfile.Domain == null || userProfile.Roles == null || !userProfile.Roles.Any())
                {
                    _logger.LogWarning("User {UserName} has missing profile data.", context.User.Identity.Name);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling user role service.");
                return false;
            }
        }

        private void RedirectToNotAllowed(HttpContext context)
        {
            context.Response.StatusCode = 403;
            context.Response.Redirect("/Error/NotAllowedYet");
        }
    }
}
