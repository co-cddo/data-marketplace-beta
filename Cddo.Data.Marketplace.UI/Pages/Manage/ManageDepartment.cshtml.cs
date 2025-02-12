using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Cddo.Data.Marketplace.Audit.EventTypes;
using System.Net.Http.Headers;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Api.Dto.Requests;
using System.Text.Json;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class ManageDepartmentModel : PageModel
    {
        public Department? DepartmentDetails { get; set; }
        public List<OrganisationDetail>? Organisations { get; set; }
        public List<OrganisationDetail>? AllOrganisations { get; set; }



        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public IUserRoleService UserRoleService { get; }
        private const string ManageOrganisationsEvent = "ManageOrganisations";
        private const string ForbiddenErrorPage = "/Error/403";

        private readonly AppInsightsLogger _logger;

        public ManageDepartmentModel(IHttpClientFactory clientFactory, IConfiguration configuration, IUserRoleService userRoleService, AppInsightsLogger logger)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"]!;
            UserRoleService = userRoleService;
            _logger = logger;
        }
        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (User.Identity!.IsAuthenticated)
            {
                var roles = new List<string> { "System Administrator" };
                bool isAGMAdministrator = await UserRoleService.IsUserInRoleAsync(roles);
                if (isAGMAdministrator)
                {
                    try
                    {
                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"]!;
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.GetFromJsonAsync<Department>($"{_usersAPI}Department/department/{id}");

                        if (response == null)
                        {
                            _logger.LogWarning($"Department {id} not found.");
                            return NotFound();
                        }
                        DepartmentDetails = response;

                        var organisationResponse = await httpClient.GetFromJsonAsync<List<OrganisationDetail>>($"{_usersAPI}Department/assigned-organisations/{id}");

                        if (organisationResponse == null)
                        {
                            _logger.LogWarning($"Department {id} not found.");
                            return NotFound();
                        }

                        Organisations = organisationResponse;

                        var unassignedOrganisationsResponse = await httpClient.GetFromJsonAsync<List<OrganisationDetail>>($"{_usersAPI}Department/un-assigned-organisations");

                        if (unassignedOrganisationsResponse == null)
                        {
                            _logger.LogWarning($"Department {id} not found.");
                            return NotFound();
                        }

                        AllOrganisations = unassignedOrganisationsResponse;

                        var assignedOrganisationsResponse = await httpClient.GetFromJsonAsync<List<OrganisationDetail>>($"{_usersAPI}Department/assigned-organisations");

                        if (assignedOrganisationsResponse == null)
                        {
                            _logger.LogWarning($"Department {id} not found.");
                            return NotFound();
                        }

                        HashSet<int> assignedOrgIds = new HashSet<int>();

                        foreach (var org in assignedOrganisationsResponse)
                        {
                            assignedOrgIds.Add((int)org.OrganisationId!);
                        }
                        HttpContext.Session.SetString("assignedIds", JsonSerializer.Serialize(assignedOrgIds));

                        AllOrganisations.AddRange(assignedOrganisationsResponse);

                        HashSet<int> idsToRemove = new HashSet<int>();
                        foreach (var org in organisationResponse)
                        {
                            idsToRemove.Add((int)org.OrganisationId!);
                        }
                        AllOrganisations.RemoveAll(x => idsToRemove.Contains((int)x.OrganisationId!));
                        AllOrganisations = AllOrganisations.OrderBy(x => x.OrganisationName).ToList();




                        UserProfile usr = await UserRoleService.GetUserProfileAsync();
                        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                        _logger.LogUserEvent(UserEvent.UserPageNavigation, ManageOrganisationsEvent, "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserAccessAllowed, ManageOrganisationsEvent, "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserManageOrganisationPageAccess, ManageOrganisationsEvent, "CDDO", userEventProperties);
                        return Page();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while fetching organisation details for {id}: {ex}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(ForbiddenErrorPage);
        }
        public async Task<IActionResult> OnPostAssignOrganisationAsync(int selectedOrganisationId, int departmentId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await UserRoleService.IsUserRoleSystemAdmin();

                if (isAGMAdministrator)
                {
                    try
                    {
                        string endpointPath = $"{_usersAPI}Department";

                        var assignedOrgIds = HttpContext.Session.GetString("assignedIds");
                        bool currentlyAssigned = false;

                        if (assignedOrgIds != null)
                        {
                            var myList = JsonSerializer.Deserialize<HashSet<int>>(assignedOrgIds);
                            if (myList!.Contains(selectedOrganisationId))
                            {
                                currentlyAssigned = true;
                            }
                        }

                        if (currentlyAssigned) endpointPath = $"{endpointPath}/re-assign";
                        else endpointPath = $"{endpointPath}/Assign";

                        endpointPath = $"{endpointPath}/{departmentId}/{selectedOrganisationId}";

                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"]!;
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.PostAsJsonAsync(endpointPath, new StringContent(String.Empty), default);

                        if (response.IsSuccessStatusCode)
                        {

                            return RedirectToPage("/Manage/ManageDepartment", new { id = departmentId });
                        }
                        else
                        {
                            return RedirectToPage(ForbiddenErrorPage);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while assigning organisation {selectedOrganisationId} to department {departmentId} error: {ex}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(ForbiddenErrorPage);
        }
        public async Task<IActionResult> OnPostUnassignOrganisationAsync(int organisationId, int departmentId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await UserRoleService.IsUserRoleSystemAdmin();

                if (isAGMAdministrator)
                {
                    try
                    {
                        string endpointPath = $"{_usersAPI}Department";
                        if (departmentId == 0) return Page();
                        else endpointPath = $"{endpointPath}/un-assign";

                        endpointPath = $"{endpointPath}/{departmentId}/{organisationId}";

                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"]!;
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.PostAsJsonAsync(endpointPath, new StringContent(String.Empty), default);

                        if (response.IsSuccessStatusCode)
                        {

                            UserProfile usr = await UserRoleService.GetUserProfileAsync();
                            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                            _logger.LogUserEvent(UserEvent.UserPageNavigation, "AssignDepartment", "CDDO", userEventProperties);
                            _logger.LogUserEvent(UserEvent.UserAccessAllowed, "AssignDepartment", "CDDO", userEventProperties);
                            _logger.LogUserEvent(UserEvent.UserManageOrganisationPageAccess, ManageOrganisationsEvent, "CDDO", userEventProperties);


                            return RedirectToPage("/Manage/ManageDepartment", new { id = departmentId });



                        }
                        else
                        {
                            return RedirectToPage(ForbiddenErrorPage);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while un-assigning organisation {organisationId} from department {departmentId} error: {ex}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(ForbiddenErrorPage);
        }
    }
}
