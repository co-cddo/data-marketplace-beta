using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.About
{
    public class PrivacyModel : PageModel
    {
        private readonly AppInsightsLogger _logger;
        private IUserRoleService UserRoleService { get; }

        public PrivacyModel(AppInsightsLogger logger, IUserRoleService userRoleService)
        {
            _logger = logger;
            UserRoleService = userRoleService;
        }
        public async void OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                UserProfile response = await UserRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(response);
                _logger.LogUserEvent(UserEvent.UserPageNavigation, "Privacy", "CDDO", userEventProperties);
            }
            else
            {
                var userEventProperties = AuditUtility.GetPlaceholderUserProfileDictionary();
                _logger.LogUserEvent(UserEvent.UserPageNavigation, "Privacy", "CDDO", userEventProperties);
            }
        }
    }
}
