using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Net.Http.Headers;
using static Cddo.Data.Marketplace.Api.Dto.ManageUser.OrganisationDetail;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class AssignDepartmentModel : PageModel
    {
        public List<Department>? Departments { get; set; }

        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public OrganisationDetail? OrganisationDetails { get; set; }
        public IUserRoleService UserRoleService { get; }
        private readonly AppInsightsLogger _logger;
        private readonly string manageOrganisations = "ManageOrganisations";
        private readonly string forbiddenErrorUrl = "/Error/403";
        private readonly string assignDepartment = "AssignDepartment";



        public AssignDepartmentModel(IHttpClientFactory clientFactory, IConfiguration configuration, IUserRoleService userRoleService, AppInsightsLogger logger)
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
                bool isAGMAdministrator = await UserRoleService.IsUserRoleSystemAdmin();
                if (isAGMAdministrator)
                {
                    try
                    {
                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.GetFromJsonAsync<List<Department>>($"{_usersAPI}Department/departments");

                        if (response == null)
                        {
                            _logger.LogWarning($"No departments found.");
                            return NotFound();
                        }

                        Departments = response;

						var secondResponse = await httpClient.GetFromJsonAsync<OrganisationDetail>($"{_usersAPI}Organisations/{id}");

						if (secondResponse == null)
						{
							_logger.LogWarning($"Organisation {id} not found.");
							return NotFound();
						}

                       OrganisationDetails = secondResponse;

						UserProfile usr = await UserRoleService.GetUserProfileAsync();
                        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                        _logger.LogUserEvent(UserEvent.UserPageNavigation, manageOrganisations, "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserAccessAllowed, manageOrganisations, "CDDO", userEventProperties);
                        _logger.LogUserEvent(UserEvent.UserManageOrganisationPageAccess, manageOrganisations, "CDDO", userEventProperties);
                        return Page();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while fetching organisation details for {id}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(forbiddenErrorUrl);
        }

        public async Task<IActionResult> OnPostAssignDepartmentAsync(int organisationId, int selectedDepartmentId, int currentDepartmentId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await UserRoleService.IsUserRoleSystemAdmin();

                if (isAGMAdministrator)
                {
                    try
                    {
                        string endpointPath = $"{_usersAPI}Department";
                        if (currentDepartmentId != 0) endpointPath = $"{endpointPath}/re-assign";
                        else endpointPath = $"{endpointPath}/Assign";

                        endpointPath = $"{endpointPath}/{selectedDepartmentId}/{organisationId}";

                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"]!;
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.PostAsJsonAsync(endpointPath, new StringContent(String.Empty), default);

                        if (response.IsSuccessStatusCode)
                        {

                            UserProfile usr = await UserRoleService.GetUserProfileAsync();
                            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                            _logger.LogUserEvent(UserEvent.UserPageNavigation, assignDepartment, "CDDO", userEventProperties);
                            _logger.LogUserEvent(UserEvent.UserAccessAllowed, assignDepartment, "CDDO", userEventProperties);
                            _logger.LogUserEvent(UserEvent.UserManageOrganisationPageAccess, manageOrganisations, "CDDO", userEventProperties);

                            var departmentResponse = await httpClient.GetFromJsonAsync<List<Department>>($"{_usersAPI}Department/departments");

                            if (departmentResponse == null)
                            {
                                _logger.LogWarning($"No departments found.");
                                return NotFound();
                            }

                            Departments = departmentResponse;

                            var secondResponse = await httpClient.GetFromJsonAsync<OrganisationDetail>($"{_usersAPI}Organisations/{organisationId}");

                            if (secondResponse == null)
                            {
                                _logger.LogWarning($"Organisation {organisationId} not found.");
                                return NotFound();
                            }

                            OrganisationDetails = secondResponse;

                            return Page();
                        }
                        else
                        {
                            return RedirectToPage(forbiddenErrorUrl);
                        }                       

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while assigning department {selectedDepartmentId} to organisation {organisationId}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(forbiddenErrorUrl);

        }
        public async Task<IActionResult> OnPostUnassignDepartmentAsync(int organisationId, int currentDepartmentId)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await UserRoleService.IsUserRoleSystemAdmin();

                if (isAGMAdministrator)
                {
                    try
                    {
                        string endpointPath = $"{_usersAPI}Department";
                        if (currentDepartmentId == 0) return Page();
                        else endpointPath = $"{endpointPath}/un-assign";

                        endpointPath = $"{endpointPath}/{currentDepartmentId}/{organisationId}";

                        var httpClient = _clientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"]!;
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                        var response = await httpClient.PostAsJsonAsync(endpointPath, new StringContent(String.Empty), default);

                        if (response.IsSuccessStatusCode)
                        {

                            UserProfile usr = await UserRoleService.GetUserProfileAsync();
                            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(usr);
                            _logger.LogUserEvent(UserEvent.UserPageNavigation, assignDepartment, "CDDO", userEventProperties);
                            _logger.LogUserEvent(UserEvent.UserAccessAllowed, assignDepartment, "CDDO", userEventProperties);
                            _logger.LogUserEvent(UserEvent.UserManageOrganisationPageAccess, manageOrganisations, "CDDO", userEventProperties);

                            var departmentResponse = await httpClient.GetFromJsonAsync<List<Department>>($"{_usersAPI}Department/departments");

                            if (departmentResponse == null)
                            {
                                _logger.LogWarning($"No departments found.");
                                return NotFound();
                            }

                            Departments = departmentResponse;

                            var secondResponse = await httpClient.GetFromJsonAsync<OrganisationDetail>($"{_usersAPI}Organisations/{organisationId}");

                            if (secondResponse == null)
                            {
                                _logger.LogWarning($"Organisation {organisationId} not found.");
                                return NotFound();
                            }

                            OrganisationDetails = secondResponse;

                            return Page();
                        }
                        else
                        {
                            return RedirectToPage(forbiddenErrorUrl);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while un-assigning department {currentDepartmentId} to organisation {organisationId}");
                        return RedirectToPage("/Error");
                    }
                }
            }
            return RedirectToPage(forbiddenErrorUrl);

            return Page();
        }

    }
}
