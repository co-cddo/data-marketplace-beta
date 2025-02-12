using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class IndexModel : PageModel
    {
        private readonly AppInsightsLogger _logger;
        private IUserRoleService UserRoleService { get; }

        public IndexModel(AppInsightsLogger logger, IUserRoleService userRoleService)
        {
            _logger = logger;
            UserRoleService = userRoleService;
        }

        private const string ManagementEvent = "Management";

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await UserRoleService.IsUserRoleSystemAdmin();
                bool isSupplier = await UserRoleService.IsUserRoleSupplier();

                if (isAGMAdministrator || isSupplier)
                {
                    UserProfile response = await UserRoleService.GetUserProfileAsync();
                    var userProperties = AuditUtility.ConvertUserProfileToJSONDictionary(response);
                    _logger.LogUserEvent(UserEvent.UserPageNavigation, ManagementEvent, "CDDO", userProperties);
                    _logger.LogUserEvent(UserEvent.UserManagementPageAccess, ManagementEvent, "CDDO", userProperties);
                    _logger.LogUserEvent(UserEvent.UserAccessAllowed, ManagementEvent, "CDDO", userProperties);
                    ViewData["IsOrgAdmin"] = true;
                    return Page();
                }
                else
                {
                    var isOrgAdmin = await UserRoleService.IsUserRoleAdmin();
                    if (!isOrgAdmin)
                    {
                        return RedirectToPage("/Error/403");
                    }
                    ViewData["IsOrgAdmin"] = true;
                    return Page();
                }
            }
            UserProfile profile = await UserRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(profile);
            _logger.LogUserEvent(UserEvent.UserPageNavigation, ManagementEvent, "CDDO", userEventProperties);
            _logger.LogUserEvent(UserEvent.UserManagementPageAccess, ManagementEvent, "CDDO", userEventProperties);
            _logger.LogUserEvent(UserEvent.UserAccessDenied, ManagementEvent, "CDDO", userEventProperties);
            return RedirectToPage("/Error/403");
        }
    }
}
