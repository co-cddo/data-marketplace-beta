using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class UserModel : PageModel
    {
        private readonly IUserRoleService _userService;
        private readonly IUserRoleService _userRoleService;
        private readonly IHttpClientFactory _clientFactory;
        public string idToken { get; set; }
        private readonly string _usersAPI;

        public UserModel(IUserRoleService userService, IUserRoleService userRoleService, IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _userService = userService;
            _userRoleService = userRoleService;
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
        }

        [BindProperty(SupportsGet = true)]
        public string UserId { get; set; }

        [BindProperty]
        public UserProfile Subject { get; set; }

        public List<Role> AvailableRoles { get; set; }

        public class RoleViewModel
        {
            public Role Role { get; set; }
            public bool IsAssigned { get; set; }
            public UserRoleApprovalDetail? UserRoleApproval { get; set; } = new UserRoleApprovalDetail();
        }

        public List<RoleViewModel> Roles { get; set; }

        public async Task OnGetAsync(string userId)
        {
            var loggedInUser = await _userRoleService.GetUserProfileAsync();
            UserId = userId;
            Subject = await _userService.GetUserByIdAsync(userId);
            var allRoles = await GetAllRoles();

            var defaultRoles = allRoles.Where(x=>x.RoleId == 7 || x.RoleId == 5 || x.RoleId == 4).ToList();

            bool isAdminUser = loggedInUser.Roles != null &&
                    loggedInUser.Roles.Any(role => role.RoleId == 6) ||
                    loggedInUser.Roles.Any(role => role.RoleId == 1) ||
                    loggedInUser.Roles.Any(role => role.RoleId == 2);

            

            if (isAdminUser && allRoles != null && allRoles.Any())
            {
                var systAdminrole = allRoles.Where(x => x.RoleId == 6).FirstOrDefault();
                
                if (systAdminrole != null)
                {
                    defaultRoles.Add(systAdminrole);
                }
            }

            bool isOrgAdminUser = loggedInUser.Roles != null &&
                   loggedInUser.Roles.Any(role => role.RoleId == 3);

            if ((isAdminUser || isOrgAdminUser) && allRoles != null && allRoles.Any())
            {
                var orgAdminrole = allRoles.Where(x => x.RoleId == 3).FirstOrDefault();
                if (orgAdminrole != null)
                {
                    defaultRoles.Add(orgAdminrole);
                }
            }

            var userApprovals = await GetUserApprovals(userId);

            // Determine which roles are assigned
            Roles = defaultRoles.Select(role => new RoleViewModel
            {
                Role = role,
                IsAssigned = Subject.Roles.Any(userRole => userRole.RoleId == role.RoleId),
                UserRoleApproval = userApprovals.FirstOrDefault(x=>x.RoleID == role.RoleId)
            })
            .OrderByDescending(r => r.IsAssigned) // This ensures that assigned roles come first
            .ThenBy(r => r.Role.RoleName) // Then order alphabetically by role name
            .ToList();
        }

        private async Task<List<UserRoleApprovalDetail>> GetUserApprovals(string userId)
        {
            // Retrieve the ID token stored in the current user's context
            idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
            if (string.IsNullOrEmpty(idToken))
            {
                idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
            }
            if (string.IsNullOrEmpty(idToken))
            {
                // Handle the case where the ID token is not available
                return new List<UserRoleApprovalDetail>();
            }

            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value;

            // Create a new HttpClient instance
            var httpClient = _clientFactory.CreateClient();

            // Set the Authorization header with the ID token as a Bearer token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var userApprovals = await httpClient.GetFromJsonAsync<List<UserRoleApprovalDetail>>($"{_usersAPI}User/myapprovals/{userId}");


            return userApprovals;
        }

        public async Task<IActionResult> OnPostAddRoleAsync(string roleId, string userId)
        {
            Subject = await _userService.AddUserToRoleAsync(roleId, userId);
            return RedirectToPage(new { userId = userId });
        }

        public async Task<IActionResult> OnPostRemoveRoleAsync(string roleId, string userId)
        {
            Subject = await _userService.RemoveUserFromRoleAsync(roleId, userId);
            return RedirectToPage(new { userId = userId });
        }

        private async Task<List<Role>> GetAllRoles()
        {
            return await _userService.GetAllRolesAsync();
        }
    }
}
