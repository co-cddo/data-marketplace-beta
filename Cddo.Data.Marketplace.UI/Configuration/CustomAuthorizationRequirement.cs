namespace Cddo.Data.Marketplace.UI.Configuration
{
    using Cddo.Data.Marketplace.Logic.Services.Interfaces;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Threading.Tasks;

    public class CustomAuthorizationRequirement : IAuthorizationRequirement
    {
    }

    public class CustomAuthorizationHandler : AuthorizationHandler<CustomAuthorizationRequirement>
    {
        private readonly IUserRoleService _userRoleService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthorizationHandler(IUserRoleService userRoleService, IHttpContextAccessor httpContextAccessor)
        {
            _userRoleService = userRoleService;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomAuthorizationRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Check if the current endpoint has an [AllowAnonymous] attribute
                var endpoint = httpContext.GetEndpoint();
                if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
                {
                    context.Succeed(requirement);
                    return;  // Stop processing if [AllowAnonymous]
                }

                // Check token expiration date
                var expiresAt = await httpContext.GetTokenAsync("expires_at");
                if (DateTime.TryParse(expiresAt, out var expiresAtDate))
                {
                    if (expiresAtDate < DateTime.UtcNow)
                    {
                        context.Fail();
                        return;
                    }
                }

                var issuedAt = httpContext.User.FindFirst("iat")?.Value;
                if (issuedAt != null && long.TryParse(issuedAt, out var issuedAtUnix))
                {
                    var issuedAtDate = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix).UtcDateTime;

                    if (issuedAtDate.Date != DateTime.UtcNow.Date)
                    {
                        context.Fail();
                        httpContext.Response.Redirect("/auth/signin");
                        return;
                    }
                }

                // Check user domain
                bool isDomainEnabled = await _userRoleService.IsUserDomainEnabledAsync();
                if (isDomainEnabled)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }

                List<string> roles = new List<string>
                {
                    "Organisation Administrator",
                    "Metadata Publisher",
                    "Data Explorer",
                    "System Administrator",
                    "Data Request Approver"
                };

                // Step 2: Check if the user is in any of these roles
                
                bool isUserInRole = await _userRoleService.IsUserInRoleAsync(roles);

                // Now you can use isUserInRole to determine if the user has any of the roles
                if (isUserInRole)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
        }
    }
}
