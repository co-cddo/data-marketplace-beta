using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.Error
{
    public class Error403Model : PageModel
    {
        public IUserRoleService UserRoleService { get; }
        private readonly AppInsightsLogger _logger;

        public Error403Model(IUserRoleService userRoleService, AppInsightsLogger logger)
        {
            UserRoleService = userRoleService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            Response.StatusCode = 403;
            if (User.Identity.IsAuthenticated)
            {
                UserProfile profile = await UserRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(profile);
                string referrerUrl = HttpContext.Request.Headers["Referer"].ToString();
                userEventProperties["ReferrerUrl"] = referrerUrl;
                _logger.LogUserEvent(UserEvent.UserAccessDenied, "AccessDenied", "CDDO", userEventProperties);
            }
            else
            {
                var userEventProperties = AuditUtility.GetPlaceholderUserProfileDictionary();
                string referrerUrl = HttpContext.Request.Headers["Referer"].ToString();
                userEventProperties["ReferrerUrl"] = referrerUrl;
                _logger.LogUserEvent(UserEvent.UserAccessDenied, "AccessDenied", "CDDO", userEventProperties);

            }
        }
    }

}
