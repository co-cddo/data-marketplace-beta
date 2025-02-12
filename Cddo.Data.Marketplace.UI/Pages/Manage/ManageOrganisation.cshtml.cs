using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class ManageOrganisationModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public bool? SavedYes { get; set; }
        public OrganisationDetail OrganisationDetails { get; set; }
        public IUserRoleService UserRoleService { get; }
        private readonly AppInsightsLogger _logger;
        private static readonly string AccessDeniedPage = "/Error/403";

        public ManageOrganisationModel(IHttpClientFactory clientFactory, IConfiguration configuration, IUserRoleService userRoleService, AppInsightsLogger logger)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"]!;
            UserRoleService = userRoleService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id, bool? savedYes)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var roles = new List<string> { "System Administrator" };
                bool? isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
                {
                    try
                    {
                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.GetFromJsonAsync<OrganisationDetail>($"{_usersAPI}Organisations/{id}");

                        if (response == null)
                        {
                            _logger.LogWarning($"Organisation {id} not found.");
                            return NotFound();
                        }

                        if(savedYes != null && savedYes == true)
                        {
                            SavedYes = true;
                        }

                        OrganisationDetails = response;
                        UserProfile usr = await UserRoleService.GetUserProfileAsync();
                        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                        _logger.LogUserEvent(UserEvent.UserPageNavigation, "ManageOrganisations", "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserAccessAllowed, "ManageOrganisations", "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserManageOrganisationPageAccess, "ManageOrganisations", "CDDO", userEventProperties);
                        return Page();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while fetching organisation details for {id}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(AccessDeniedPage);
        }

        public async Task<IActionResult> OnPostToggleAllowListAsync(int domainId, bool allow, int OrganisationId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var roles = new List<string> { "System Administrator" };
                bool? isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
                {
                    // Here, call your API to update the allowList status of the domain
                    var httpClient = _clientFactory.CreateClient();
                    string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                    var response = await httpClient.PatchAsJsonAsync($"{_usersAPI}Organisations/domains/{domainId}/allowList", allow);

                    if (response.IsSuccessStatusCode)
                    {
                        UserProfile usr = await UserRoleService.GetUserProfileAsync();
                        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                        _logger.LogUserEvent(UserEvent.UserChangedAllowList, "ManageOrganisations", "CDDO", userEventProperties);
                        _logger.LogAdminEvent(AdminAuditEvent.AdminUpdateOrganizationDetails, "ManageOrganisation", "CDDO", "ToggleAllow", domainId.ToString(), allow.ToString(), userEventProperties);
                        return RedirectToPage(new { id = OrganisationId });
                    }
                    else
                    {
                        // Handle error response
                        return Page();
                    }
                }
            }
            return RedirectToPage(AccessDeniedPage);
        }

        public async Task<IActionResult> OnPostDeleteDomainAsync(int domainId, int organisationId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var roles = new List<string> { "System Administrator" };
                bool? isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
                {
                    try
                    {
                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.DeleteAsync($"{_usersAPI}Organisations/domains/{domainId}");

                        if (response.IsSuccessStatusCode)
                        {
                            UserProfile usr = await UserRoleService.GetUserProfileAsync();
                            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                            _logger.LogUserEvent(UserEvent.UserManagementPageAccess, "DeleteDomain", "CDDO", userEventProperties);
                            _logger.LogAdminEvent(AdminAuditEvent.AdminUpdateOrganizationDetails, "DeleteDomain", "CDDO", "DeleteDomain", domainId.ToString(), "SoftDelete", userEventProperties);
                            return RedirectToPage(new { id = organisationId });
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to delete domain {domainId}: {response.ReasonPhrase}");
                            ModelState.AddModelError(string.Empty, "Failed to delete the domain. Please try again.");
                            return Page();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while deleting domain {domainId}");
                        ModelState.AddModelError(string.Empty, "An error occurred while deleting the domain. Please try again.");
                        return Page();
                    }
                }
            }
            return RedirectToPage(AccessDeniedPage);
        }

        public async Task<IActionResult> OnPostAddDomainAsync(int OrganisationId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var roles = new List<string> { "System Administrator" };
                bool? isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
                {
                    string domainName = Request.Form["DomainName"];
                    string organisationFormat = Request.Form["OrganisationFormat"];
                    string organisationType = Request.Form["OrganisationType"];

                    if (string.IsNullOrEmpty(domainName) || string.IsNullOrEmpty(organisationFormat) || string.IsNullOrEmpty(organisationType))
                    {
                        ModelState.AddModelError(string.Empty, "All form fields are required.");
                        await LoadOrganisationDetailsAsync(OrganisationId); // Reload organisation details
                        return Page();
                    }
                    Enum.TryParse(typeof(OrganisationType), organisationType, true, out var parsedValue);
                    var domainDetail = new DomainDetail
                    {
                        DomainName = domainName,
                        OrganisationFormat = organisationFormat,
                        OrganisationType = (OrganisationType)parsedValue,
                        AllowList = true
                    };

                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        };

                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.PostAsJsonAsync($"{_usersAPI}Organisations/{OrganisationId}/domains", domainDetail, options);

                        if (response.IsSuccessStatusCode)
                        {
                            UserProfile usr = await UserRoleService.GetUserProfileAsync();
                            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                            _logger.LogUserEvent(UserEvent.UserManagementPageAccess, "AddDomain", "CDDO", userEventProperties);
                            _logger.LogAdminEvent(AdminAuditEvent.AdminUpdateOrganizationDetails, "AddDomain", "CDDO", "AddDomain", OrganisationId.ToString(), domainDetail.DomainName, userEventProperties);

                            await LoadOrganisationDetailsAsync(OrganisationId); // Reload organisation details
                            return Page();
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            _logger.LogWarning($"Failed to add domain. Status code: {response.StatusCode}. Response: {errorContent}");
                            ModelState.AddModelError(string.Empty, "An error occurred while adding the domain. Please try again.");
                            await LoadOrganisationDetailsAsync(OrganisationId); // Reload organisation details
                            return Page();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("An error occurred while adding the domain.");
                        ModelState.AddModelError(string.Empty, "An error occurred while adding the domain. Please try again.");
                        await LoadOrganisationDetailsAsync(OrganisationId); // Reload organisation details
                        return Page();
                    }
                }
            }
            return RedirectToPage(AccessDeniedPage);
        }

        public async Task<IActionResult> OnPostManageDomain(int organisationId, int domainId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var routeValues = new
                {
                    organisationId = organisationId,
                    domainId = domainId
                };

                return RedirectToPage("/Manage/ManageDomain", routeValues);
            }
            return RedirectToPage(AccessDeniedPage);
        }

        private async Task LoadOrganisationDetailsAsync(int organisationId)
        {
            try
            {
                var httpClient = _clientFactory.CreateClient();
                string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                var response = await httpClient.GetFromJsonAsync<OrganisationDetail>($"{_usersAPI}Organisations/{organisationId}");

                if (response != null)
                {
                    OrganisationDetails = response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"An error occurred while loading organisation details for {organisationId}");
            }
        }
    }
}
