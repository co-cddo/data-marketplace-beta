using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class AddOrganisationModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        private readonly IManageDepartmentsService _manageDepartmentService;

        [BindProperty]
        public string OrganisationName { get; set; }

        // Assuming these are the domain details entered in the form
        [BindProperty]
        public string DomainName { get; set; }
        [BindProperty]
        public string? OrganisationType { get; set; }
        [BindProperty]
        public int? DepartmentId { get; set; }
        [BindProperty]
        public string? OrganisationFormat { get; set; }
        public List<Department>? AllDepartments { get; set; }
        [BindProperty]
        public bool AllowList { get; set; }

        public AddOrganisationModel(IHttpClientFactory clientFactory, IConfiguration configuration, IManageDepartmentsService manageDepartmentService)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
            _manageDepartmentService = manageDepartmentService;
        }

        public async Task<IActionResult> OnGet(int? departmentId)
        {
            ViewData["DepartmentId"] = departmentId;

            var allDepartments = await _manageDepartmentService.GetManageDepartmentsAsync(new Api.Dto.Requests.ManageDepartmentRequest() { PageSize = 1000 });

            if (allDepartments != null)
            {
                AllDepartments = allDepartments.Departments;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? selectedDepartmentId)
        {
            SetDefaultValuesForOrganisationTypeAndFormat();

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync(selectedDepartmentId);
                return Page();
            }   

            var idToken = GetIdTokenFromCookies();
            var httpClient = _clientFactory.CreateClient();
            SetAuthorizationHeader(httpClient, idToken);

            var newOrganisation = CreateOrganisation();

            var response = await CreateOrganisationAsync(httpClient, newOrganisation);

            if (response.IsSuccessStatusCode)
            {
                return await HandleSuccessResponseAsync(response, selectedDepartmentId, idToken);
            }

            return await HandleErrorResponseAsync(response, selectedDepartmentId);
        }

        private void SetDefaultValuesForOrganisationTypeAndFormat()
        {
            OrganisationType = string.IsNullOrWhiteSpace(OrganisationType) ? "" : OrganisationType;
            OrganisationFormat = string.IsNullOrWhiteSpace(OrganisationFormat) ? "" : OrganisationFormat;
        }

        private async Task LoadDepartmentsAsync(int? selectedDepartmentId)
        {
            DepartmentId = selectedDepartmentId ?? 0;

            var allDepartments = await _manageDepartmentService.GetManageDepartmentsAsync(new Api.Dto.Requests.ManageDepartmentRequest() { PageSize = 1000 });

            if (allDepartments != null)
            {
                AllDepartments = allDepartments.Departments;
            }
        }

        private string GetIdTokenFromCookies()
        {
            return HttpContext.Request.Cookies["CO-Datamarketplace"];
        }

        private static void SetAuthorizationHeader(HttpClient httpClient, string idToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
        }

        private OrganisationDetail CreateOrganisation()
        {
            Enum.TryParse(typeof(OrganisationType), OrganisationType, true, out var parsedValue);

            return new OrganisationDetail
            {
                OrganisationName = this.OrganisationName,
                OrganisationType = (OrganisationType)parsedValue,
                Domains = new List<DomainDetail>
                {
                    new DomainDetail
                    {
                        DomainName = this.DomainName,
                        OrganisationType = (OrganisationType)parsedValue,
                        OrganisationFormat = this.OrganisationFormat,
                        AllowList = this.AllowList
                    }
                }
            };
        }

        private async Task<HttpResponseMessage> CreateOrganisationAsync(HttpClient httpClient, OrganisationDetail newOrganisation)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return await httpClient.PostAsJsonAsync($"{_usersAPI}Organisations", newOrganisation, options);
        }

        private async Task<IActionResult> HandleSuccessResponseAsync(HttpResponseMessage response, int? selectedDepartmentId, string idToken)
        {
            int? orgId = await GetOrganisationIdFromResponseAsync(response);

            if (orgId != 0 && selectedDepartmentId != null && selectedDepartmentId != 0)
            {
                var departmentResponse = await AssignDepartmentToOrganisationAsync(idToken, selectedDepartmentId.Value, orgId.Value);

                if (departmentResponse.IsSuccessStatusCode)
                {
                    return RedirectToPage("/Manage/ManageDepartment", new { id = selectedDepartmentId });
                }
            }

            return Redirect("/manageorganisation");
        }

        private static async Task<int?> GetOrganisationIdFromResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (int.TryParse(content, out int result))
            {
                return result;
            }

            return 0;
        }

        private async Task<HttpResponseMessage> AssignDepartmentToOrganisationAsync(string idToken, int selectedDepartmentId, int orgId)
        {
            var httpClient = _clientFactory.CreateClient();
            SetAuthorizationHeader(httpClient, idToken);

            string endpointPath = $"{_usersAPI}Department/Assign/{selectedDepartmentId}/{orgId}";

            return await httpClient.PostAsJsonAsync(endpointPath, new StringContent(String.Empty), default);
        }

        private async Task<IActionResult> HandleErrorResponseAsync(HttpResponseMessage response, int? selectedDepartmentId)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(string.Empty, "Submission failed: An organisation or domain with the same name already exists");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "An error occurred while creating the organisation");
            }

            await LoadDepartmentsAsync(selectedDepartmentId);
            return Page();
        }

    }
}
