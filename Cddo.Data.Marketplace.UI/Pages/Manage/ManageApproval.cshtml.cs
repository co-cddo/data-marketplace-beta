using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Cddo.Data.Marketplace.Audit.EventTypes;
using System.Net.Http.Headers;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.Api.Dto;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Cddo.Data.Marketplace.UI.Model.Enum;
using Microsoft.AspNetCore.Http;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class ManageApprovalModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public UserRoleApprovalDetail ApprovalDetails { get; set; }
        public IUserRoleService UserRoleService { get; }
        private readonly AppInsightsLogger _logger;
        private const string ApprovalEvent = "Approval";

        public ManageApprovalModel(IHttpClientFactory clientFactory, IConfiguration configuration, IUserRoleService userRoleService, AppInsightsLogger logger)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
            UserRoleService = userRoleService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (User.Identity.IsAuthenticated)
            {
                bool? isSystemAdmin = await UserRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.HasValue && isSystemAdmin.Value)
                {
                    var httpClient = _clientFactory.CreateClient();
                    string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                    var response = await httpClient.GetFromJsonAsync<UserRoleApprovalDetail>($"{_usersAPI}User/UserRoleApproval/{id}");

                    if (response == null)
                        return NotFound();
                    UserProfile usr = await UserRoleService.GetUserProfileAsync();

                    ApprovalDetails = response;
                   
                    var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                    _logger.LogUserEvent(UserEvent.UserPageNavigation, "ManageApprovals", "CDDO", userEventProperties);
                    _logger.LogUserEvent(UserEvent.UserAccessAllowed, "ManageApprovals", "CDDO", userEventProperties);
                    _logger.LogUserEvent(UserEvent.UserManageApprovals, "ManageApprovals", "CDDO", userEventProperties);
                    return Page();
                }
                else
                {
                    bool? isOrgAdmin = await UserRoleService.IsUserRoleAdmin();
                    if (isOrgAdmin.HasValue && isOrgAdmin.Value)
                    {
                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.GetFromJsonAsync<UserRoleApprovalDetail>($"{_usersAPI}User/UserRoleApproval/{id}");

                        if (response == null)
                            return NotFound();

                        ApprovalDetails = response;
                        UserProfile usr = await UserRoleService.GetUserProfileAsync();

                        if (usr.Organisation.OrganisationId != response.OrganisationID)
                        {
                            return RedirectToPage("/Error/403");
                        }
                        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                        _logger.LogUserEvent(UserEvent.UserPageNavigation, "ManageApprovals", "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserAccessAllowed, "ManageApprovals", "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserManageApprovals, "ManageApprovals", "CDDO", userEventProperties);
                        return Page();
                    }
                }
            }
            return RedirectToPage("/Error/403");
        }

        public async Task<IActionResult> OnPostApproveAsync(int approvalId, int roleId, int domainId, int organisationId, string rejectionComment, string approve, string reject)
        {
            if (User.Identity.IsAuthenticated)
            {
                var roles = new List<string> { "Organisation Administrator", "System Administrator" };
                bool? isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
                {
                    var status = ApprovalStatus.Rejected;
                    if(!string.IsNullOrEmpty(approve))
                    {
                        status = ApprovalStatus.Approved;
                    }

                    await UpdateApprovalStatus(approvalId, roleId, domainId, organisationId, status, rejectionComment);
                    return RedirectToPage(new { id = approvalId }); // Redirects to the same page to refresh the list
                }
            }
            return RedirectToPage("/Error/403");
        }

        // Method to handle the rejection action
        public async Task<IActionResult> OnPostRejectAsync(int approvalId, int roleId, int domainId, int organisationId, string rejectionComment)
        {
            if (User.Identity.IsAuthenticated)
            {
                var roles = new List<string> { "Organisation Administrator", "System Administrator" };
                bool? isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
                {
                    await UpdateApprovalStatus(approvalId, roleId, domainId, organisationId, ApprovalStatus.Rejected, rejectionComment);
                    return RedirectToPage(new { id = approvalId }); // Redirects to the same page to refresh the list
                }
            }
            return RedirectToPage("/Error/403");
        }

        private async Task UpdateApprovalStatus(int approvalId, int roleID, int domainId, int organisationId, ApprovalStatus status, string rejectionComment)
        {
            var httpClient = _clientFactory.CreateClient();
            string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            UserProfile profile = await UserRoleService.GetUserProfileAsync();

            UserRoleApproval approval = new UserRoleApproval();
            approval.ApprovalID = approvalId;
            approval.RoleID = roleID;
            approval.ApprovalStatus = status;
            approval.DomainID = domainId;
            approval.OrganisationID = organisationId;
            approval.ApprovedByUserID = profile.User.UserId;
            approval.RejectionComment = rejectionComment;

            await httpClient.PostAsJsonAsync($"{_usersAPI}User/RoleRequestDecision", approval);
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(profile);

            if(approval.ApprovalStatus == ApprovalStatus.Approved)
            {
                _logger.LogUserEvent(UserEvent.UserPageNavigation, ApprovalEvent, "CDDO", userEventProperties);
                _logger.LogUserEvent(UserEvent.UserManagementPageAccess, ApprovalEvent, "CDDO", userEventProperties);
                _logger.LogUserEvent(UserEvent.UserApprovalApproved, ApprovalEvent, "CDDO", userEventProperties);
            }
            if(approval.ApprovalStatus == ApprovalStatus.Rejected)
            {
                _logger.LogUserEvent(UserEvent.UserPageNavigation, ApprovalEvent, "CDDO", userEventProperties);
                _logger.LogUserEvent(UserEvent.UserManagementPageAccess, ApprovalEvent, "CDDO", userEventProperties);
                _logger.LogUserEvent(UserEvent.UserApprovalRejected, ApprovalEvent, "CDDO", userEventProperties);
            }
            
        }
    }
}
