using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class RevokeRoleModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public UserRoleApprovalDetail ApprovalDetails { get; set; }
        public UserProfile Subject { get; set; }
        public Role UserRole { get; set; }
        private readonly IUserRoleService _userRoleService;
        public string idToken { get; set; }
        public string RevokeReason { get; set; }

        public bool RevokeConfirmation { get; set; }

        public RevokeRoleModel(IHttpClientFactory clientFactory, IConfiguration configuration, IUserRoleService userRoleService)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
            _userRoleService = userRoleService;
        }

        public async Task<IActionResult> OnGet(int userId, int roleId)
        {
            //Get the subject user
            Subject = await _userRoleService.GetUserByIdAsync(userId.ToString());

            if (Subject != null && Subject.Roles != null && Subject.Roles.Any())
            {
                UserRole = Subject.Roles.FirstOrDefault(r => r.RoleId == roleId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRevokeAsync(int userId, int roleId, string revokeReason)
        {
            if (User.Identity.IsAuthenticated)
            {

                if (string.IsNullOrEmpty(revokeReason))
                {
                    ViewData["NoReasonProvided"] = true;
                    Subject = await _userRoleService.GetUserByIdAsync(userId.ToString());

                    if (Subject != null && Subject.Roles != null && Subject.Roles.Any())
                    {
                        UserRole = Subject.Roles.FirstOrDefault(r => r.RoleId == roleId);
                    }
                    return Page();
                }

                RevokeReason = revokeReason;
                RevokeConfirmation = true;

                //Get the subject user
                Subject = await _userRoleService.GetUserByIdAsync(userId.ToString());

                if (Subject != null && Subject.Roles != null && Subject.Roles.Any())
                {
                    UserRole = Subject.Roles.FirstOrDefault(r => r.RoleId == roleId);
                }

                return Page();
            }
            return RedirectToPage("/Error/403");
        }

        public async Task<IActionResult> OnPostConfirmRevokeAsync(int userId, int roleId, string revokeReason, string confirmrevoke, string cancelrevoke)
        {
            if (!string.IsNullOrEmpty(cancelrevoke))
            {
                //RevokeReason = revokeReason;
                //RevokeConfirmation = false;
                ////Get the subject user
                //Subject = await _userRoleService.GetUserByIdAsync(userId.ToString());

                //if (Subject != null && Subject.Roles != null && Subject.Roles.Any())
                //{
                //    UserRole = Subject.Roles.FirstOrDefault(r => r.RoleId == roleId);
                //}

                //return Page();
                return RedirectToPage("/Manage/User", new { userId });
            }


            var roles = new List<string> { "Organisation Administrator", "System Administrator" };
            bool? isAGMAdministrator = await _userRoleService.IsUserInRoleAsync(roles);
            if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
            {
                Subject = await _userRoleService.GetUserByIdAsync(userId.ToString());

                var loggedInUser = await _userRoleService.GetUserProfileAsync();

                // We nned to create an approval of Revoked
                UserRoleApproval approval = new UserRoleApproval()
                {
                    UserID = Subject.User.UserId,
                    DomainID = Subject.Domain.DomainId,
                    OrganisationID = Subject.Organisation.OrganisationId,
                    RoleID = roleId,
                    ApprovalStatus = Model.Enum.ApprovalStatus.Revoked,
                    RejectionComment = revokeReason,
                    ApprovedByUserID = loggedInUser.User.UserId,
                };

                // Create a new HttpClient instance
                var httpClient = _clientFactory.CreateClient();
                // Retrieve the ID token stored in the current user's context
                idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                if (string.IsNullOrEmpty(idToken))
                {
                    idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                }
                if (string.IsNullOrEmpty(idToken))
                {
                    // Handle the case where the ID token is not available
                    return RedirectToPage("/Error");
                }
                // Set the Authorization header with the ID token as a Bearer token
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                try
                {
                    await httpClient.PostAsJsonAsync($"{_usersAPI}User/ApprovalRequest", approval);

                    await _userRoleService.RemoveUserFromRoleAsync(roleId.ToString(), userId.ToString());

                    return RedirectToPage("/Manage/User", new { userId });
                }
                catch (Exception ex)
                {

                    return RedirectToPage("/Error/403");
                }

            }
            return RedirectToPage("/Error/403");
        }
    }
}
