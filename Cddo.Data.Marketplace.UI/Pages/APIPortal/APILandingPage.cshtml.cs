using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cddo.Data.Marketplace.Api.Dto.Models;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.API
{
    public class ApiLandingPageModel : PageModel
    {
        private readonly AppInsightsLogger _logger;
        private IUserRoleService UserRoleService { get; }

        public ApiLandingPageModel(AppInsightsLogger logger, IUserRoleService userRoleService)
        {
            _logger = logger;
            UserRoleService = userRoleService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity!.IsAuthenticated)
            {
                var isOrgAdmin = await UserRoleService.IsUserRoleAdmin();
                bool isSupplier = await UserRoleService.IsUserRoleSupplier();

                if (isOrgAdmin || isSupplier)
                {
                    UserProfile response = await UserRoleService.GetUserProfileAsync();
                    var userProperties = AuditUtility.ConvertUserProfileToJSONDictionary(response);
                    _logger.LogUserEvent(UserEvent.UserPageNavigation, "ApiPortal", "CDDO", userProperties);
                    _logger.LogUserEvent(UserEvent.UserManagementPageAccess, "ApiPortal", "CDDO", userProperties);
                    _logger.LogUserEvent(UserEvent.UserAccessAllowed, "ApiPortal", "CDDO", userProperties);
                    ViewData["IsOrgAdmin"] = true;
                    return Redirect("~/developer/api-keys");
                }
                else
                {
                    return Page();
                }
            }
            UserProfile profile = await UserRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(profile);
            _logger.LogUserEvent(UserEvent.UserPageNavigation, "ApiPortal", "CDDO", userEventProperties);
            _logger.LogUserEvent(UserEvent.UserManagementPageAccess, "ApiPortal", "CDDO", userEventProperties);
            _logger.LogUserEvent(UserEvent.UserAccessDenied, "ApiPortal", "CDDO", userEventProperties);
            return RedirectToPage("/Error/403");
        }
    }
}
