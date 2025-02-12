using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class UsersModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public List<UserAdminDto> Users { get; private set; } = new List<UserAdminDto>();

        public Organisations Organisations { get; set; } = new Organisations();

        [BindProperty(SupportsGet = true)]
        public int? SelectedOrganisationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedDomainId { get; set; }

        public List<DomainDetail> Domains { get; private set; } = new List<DomainDetail>();
        private readonly string _usersAPI;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? OrganisationID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DomainID { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Visible { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "email";

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "ASC";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalCount { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages
        {
            get { return (int)Math.Ceiling((double)TotalCount / PageSize); }
        }

        public IUserRoleService UserRoleService { get; }
        public UsersModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IUserRoleService userRoleService)
        {
            _httpClientFactory = httpClientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
            UserRoleService = userRoleService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool? isSystemAdmin = await UserRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.HasValue && isSystemAdmin.Value)
                {
                    ViewData["IsOrgAdmin"] = false;
                    HttpClient httpClient = _httpClientFactory.CreateClient();
                    string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                    string url = $"{_usersAPI}user?page={CurrentPage}&pageSize={PageSize}&searchTerm={SearchTerm}&sortBy={SortBy}&sortOrder={SortOrder}&organisationID={OrganisationID}&domainID={DomainID}&visible={Visible}";
                    HttpResponseMessage response = await httpClient.GetAsync(url);

                    string orglist = $"{_usersAPI}Organisations/organisationsByPage?page=1&pageSize=3000";
                    HttpResponseMessage organisations = await httpClient.GetAsync(orglist);
                    var orgjson = await organisations.Content.ReadAsStringAsync();
                    Organisations = JsonConvert.DeserializeObject<Organisations>(orgjson);

                    if (SelectedOrganisationId.HasValue)
                    {
                        TempData["SelectedOrgId"] = SelectedOrganisationId.Value;
                        OrganisationDetail selectedOrg = Organisations.Orgs
                        .FirstOrDefault(org => org.OrganisationId == SelectedOrganisationId.Value);

                        if (selectedOrg != null)
                        {
                            Domains = selectedOrg.Domains;
                        }
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var userResponse = JsonConvert.DeserializeObject<UserResponseDto>(json);

                        Users = userResponse.Users;
                        TotalCount = userResponse.TotalCount;
                    }

                    return Page();
                }
                else
                {
                    var isOrgAdmin = await UserRoleService.IsUserRoleAdmin();
                    if (!isOrgAdmin)
                    {
                        return RedirectToPage("/Error/403");
                    }
                    ViewData["IsOrgAdmin"] = true;
                    //Check if user is an org admin of any orgs on the list
                    var userProfile = await UserRoleService.GetUserProfileAsync();
                    if (userProfile != null)
                    {
                        HttpClient httpClient = _httpClientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                        string url = $"{_usersAPI}user?page={CurrentPage}&pageSize={PageSize}&searchTerm={SearchTerm}&sortBy={SortBy}&sortOrder={SortOrder}&organisationID={userProfile.Organisation.OrganisationId}";
                        HttpResponseMessage response = await httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var userResponse = JsonConvert.DeserializeObject<UserResponseDto>(json);

                            Users = userResponse.Users;
                            TotalCount = userResponse.TotalCount;
                        }
                        SelectedOrganisationId = userProfile.Organisation.OrganisationId;

                        Organisations = new Organisations()
                        {
                            Orgs = new List<OrganisationDetail> {
                                new OrganisationDetail()
                                {
                                    OrganisationId = userProfile.Organisation.OrganisationId,
                                    OrganisationName = userProfile.Organisation.OrganisationName,
                                }
                            }
                        };
                    }

                    return Page();
                }
            }

            // If not authenticated or not the required role, redirect to an unauthorized page
            return RedirectToPage("/Error/403");
        }


        public async Task<IActionResult> OnPostAsync(ManageUsersRequest manageUsersRequest)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool? isSystemAdmin = await UserRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.HasValue && isSystemAdmin.Value)
                {
                    HttpClient httpClient = _httpClientFactory.CreateClient();
                    string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                    // Build the URL with query parameters
                    var queryParams = new Dictionary<string, string>
            {
                { "pageNumber", manageUsersRequest.CurrentPage.ToString() },
                { "pageSize", manageUsersRequest.PageSize.ToString() },
                { "sortBy", manageUsersRequest.SortBy },
                { "sortOrder", manageUsersRequest.SortOrder },
                { "organisationID", manageUsersRequest.SelectedOrganisationId?.ToString() },
                { "visible", manageUsersRequest.Visible?.ToString() },
                { "domainId", manageUsersRequest.SelectedDomainId?.ToString() },
                { "searchTerm", manageUsersRequest.SearchTerm }
            };

                    var queryString = string.Join("&", queryParams.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    string url = $"{_usersAPI}user?{queryString}";

                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var userResponse = JsonConvert.DeserializeObject<UserResponseDto>(json);

                        Users = userResponse.Users;
                        TotalCount = userResponse.TotalCount;
                    }

                    string orglist = $"{_usersAPI}Organisations/organisationsByPage?page=1&pageSize=3000";
                    HttpResponseMessage organisations = await httpClient.GetAsync(orglist);
                    var orgjson = await organisations.Content.ReadAsStringAsync();
                    Organisations = JsonConvert.DeserializeObject<Organisations>(orgjson);

                    if (manageUsersRequest.SelectedOrganisationId.HasValue)
                    {
                        TempData["SelectedOrgId"] = manageUsersRequest.SelectedOrganisationId.Value;
                        OrganisationDetail selectedOrg = Organisations.Orgs
                            .FirstOrDefault(org => org.OrganisationId == manageUsersRequest.SelectedOrganisationId.Value);

                        if (selectedOrg != null)
                        {
                            Domains = selectedOrg.Domains;
                        }
                    }
                    return Page();
                }
                else
                {
                    var isOrgAdmin = await UserRoleService.IsUserRoleAdmin();
                    if (!isOrgAdmin)
                    {
                        return RedirectToPage("/Error/403");
                    }
                    ViewData["IsOrgAdmin"] = true;
                    //Check if user is an org admin of any orgs on the list
                    var userProfile = await UserRoleService.GetUserProfileAsync();
                    if (userProfile != null)
                    {
                        HttpClient httpClient = _httpClientFactory.CreateClient();
                        string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                        // Build the URL with query parameters
                        var queryParams = new Dictionary<string, string>
                            {
                                { "pageNumber", manageUsersRequest.CurrentPage.ToString() },
                                { "pageSize", manageUsersRequest.PageSize.ToString() },
                                { "sortBy", manageUsersRequest.SortBy },
                                { "sortOrder", manageUsersRequest.SortOrder },
                                { "organisationID", manageUsersRequest.SelectedOrganisationId?.ToString() },
                                { "visible", manageUsersRequest.Visible?.ToString() },
                                { "domainId", manageUsersRequest.SelectedDomainId?.ToString() },
                                { "searchTerm", manageUsersRequest.SearchTerm }
                            };

                        var queryString = string.Join("&", queryParams.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                        string url = $"{_usersAPI}user?{queryString}";

                        HttpResponseMessage response = await httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var userResponse = JsonConvert.DeserializeObject<UserResponseDto>(json);

                            Users = userResponse.Users;
                            TotalCount = userResponse.TotalCount;
                        }
                        TempData["SelectedOrgId"] = manageUsersRequest.SelectedOrganisationId.Value;
                        SelectedOrganisationId = userProfile.Organisation.OrganisationId;

                        Organisations = new Organisations()
                        {
                            Orgs = new List<OrganisationDetail> {
                                new OrganisationDetail()
                                {
                                    OrganisationId = userProfile.Organisation.OrganisationId,
                                    OrganisationName = userProfile.Organisation.OrganisationName,
                                }
                            }
                        };
                        return Page();
                    }
                }
            }

            // If not authenticated or not the required role, redirect to an unauthorized page
            return RedirectToPage("/Error/403");
        }

    }


    public class UserResponseDto
    {
        public List<UserAdminDto> Users { get; set; }
        public int TotalCount { get; set; }
    }



    public class UserAdminDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool? EmailNotification { get; set; }
        public bool? WelcomeNotification { get; set; }
        public int? OrganisationID { get; set; }
        public string OrganisationName { get; set; } // New attribute for the organisation's name
        public int? DomainID { get; set; }
        public string UserName { get; set; }
        public bool? Visible { get; set; }
        public List<Role> Roles { get; set; }
    }
}
