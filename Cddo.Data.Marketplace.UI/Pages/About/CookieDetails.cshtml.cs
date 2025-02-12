using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.About
{
    public class CookieDetailsModel : PageModel
    {
        private readonly AppInsightsLogger _logger;
        private IUserRoleService UserRoleService { get; }

        public CookieDetailsModel(AppInsightsLogger logger, IUserRoleService userRoleService)
        {
            _logger = logger;
            UserRoleService = userRoleService;
        }

        public async void OnGet()
        {
            if (User.Identity!.IsAuthenticated)
            {
                var response = await UserRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(response);
                _logger.LogUserEvent(UserEvent.UserPageNavigation, "CookieDetails", "CDDO", userEventProperties);
            }
            else
            {
                var userEventProperties = AuditUtility.GetPlaceholderUserProfileDictionary();
                _logger.LogUserEvent(UserEvent.UserPageNavigation, "CookieDetails", "CDDO", userEventProperties);
            }
        }
    }
}
