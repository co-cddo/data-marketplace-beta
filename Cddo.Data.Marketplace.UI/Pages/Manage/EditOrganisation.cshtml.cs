using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class EditOrganisationModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        private readonly IManageDepartmentsService _manageDepartmentService;
        private readonly IManageOrganisationsService _manageOrganisationService;

        [BindProperty]
        public OrganisationDetail? Organisation { get; set; }

        // Assuming these are the domain details entered in the form
        [BindProperty]
        public string? OrganisationType { get; set; }
        [BindProperty]
        public int? DepartmentId { get; set; }
        [BindProperty]
        public string? OrganisationFormat { get; set; }
        public List<Department>? AllDepartments { get; set; }
        [BindProperty]
        public bool AllowList { get; set; }

        public EditOrganisationModel(IHttpClientFactory clientFactory, IConfiguration configuration, IManageDepartmentsService manageDepartmentService, IManageOrganisationsService manageOrganisationService)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
            _manageDepartmentService = manageDepartmentService;
            _manageOrganisationService = manageOrganisationService;
        }

        public async Task<IActionResult> OnGet(int organisationId)
        {
            ViewData["OrganisationId"] = organisationId;

            var allDepartments = await _manageDepartmentService.GetManageDepartmentsAsync(new Api.Dto.Requests.ManageDepartmentRequest() { PageSize = 1000 });

            if (allDepartments != null)
            {
                AllDepartments = allDepartments.Departments;
            }

            Organisation = await _manageOrganisationService.GetOrganisationAsync(organisationId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(OrganisationDetail? updateOrganisation, int? selectedDepartmentId)
        {
            if (string.IsNullOrWhiteSpace(OrganisationType))
            {
                OrganisationType = "";
            }
            if (string.IsNullOrWhiteSpace(OrganisationFormat))
            {
                OrganisationFormat = "";
            }

            updateOrganisation.Domains = new List<DomainDetail>();

            var httpClient = _clientFactory.CreateClient();
            string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var response = await httpClient.PatchAsJsonAsync($"{_usersAPI}Organisations", updateOrganisation, options);

            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
               
                if (updateOrganisation.OrganisationId != 0 && selectedDepartmentId != null)
                {
                    string endpointPath = $"{_usersAPI}Department/Assign";

                    endpointPath = $"{endpointPath}/{selectedDepartmentId}/{updateOrganisation.OrganisationId}";

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
                    var departmentResponse = await httpClient.PostAsJsonAsync(endpointPath, new StringContent(String.Empty), default);

                    if (response.IsSuccessStatusCode)
                    {
                        if(updateOrganisation.Allowed == true)
                        {
                            return RedirectToPage("/manage/manageorganisation", new { id = updateOrganisation.OrganisationId, savedYes = true });
                        }
                        return RedirectToPage("/manage/manageorganisation", new { id = updateOrganisation.OrganisationId });
                    }
                }

                return Redirect("/manageorganisation");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the organisation.");

                var allDepartments = await _manageDepartmentService.GetManageDepartmentsAsync(new Api.Dto.Requests.ManageDepartmentRequest() { PageSize = 1000 });

                if (allDepartments != null)
                {
                    AllDepartments = allDepartments.Departments;
                }
                return Page();

            }
        }
    }
}
