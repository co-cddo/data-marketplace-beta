using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class ApprovalListModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public List<UserRoleApprovalDetail> Approvals { get; set; } = new List<UserRoleApprovalDetail>();
        public List<UserRoleApprovalDetail> PendingApprovals { get; set; } = new List<UserRoleApprovalDetail>();
        public Organisations Organisations { get; set; } = new Organisations();
        public List<DomainDetail> Domains { get; set; } = new List<DomainDetail>();

        [BindProperty(SupportsGet = true)]
        public int? SelectedOrganisationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedDomainId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "username";

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalCount { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public IUserRoleService UserRoleService { get; }

        public ApprovalListModel(IHttpClientFactory clientFactory, IConfiguration configuration, IUserRoleService userRoleService)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
            UserRoleService = userRoleService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                bool? isSystemAdmin = await UserRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.HasValue && isSystemAdmin.Value)
                {
                    HttpClient httpClient = _clientFactory.CreateClient();
                    string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                    // Build URL with query parameters
                    var url = $"{_usersAPI}User/GetUserApprovals?" +
                              $"domainId={SelectedDomainId}&organisationId={SelectedOrganisationId}" +
                              $"&searchTerm={SearchTerm}&sortBy={SortBy}&sortOrder={SortOrder}" +
                              $"&pageNumber={CurrentPage}&pageSize={PageSize}" +
                              $"&noPending=true";

                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<PaginatedUserRoleApproval>(json);
                        Approvals = result.Approvals;
                        TotalCount = result.TotalCount;
                    }

                    //Get pending request
                    var pendingurl = $"{_usersAPI}User/GetUserApprovals-pending?";
                    HttpResponseMessage responsepending = await httpClient.GetAsync(pendingurl);
                    if (responsepending.IsSuccessStatusCode)
                    {
                        var json = await responsepending.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<List<UserRoleApprovalDetail>>(json);
                        PendingApprovals = result;
                    }

                    // Get Organisations and Domains
                    var orgResponse = await httpClient.GetAsync($"{_usersAPI}Organisations/organisationsByPage?page=1&pageSize=3000");
                    if (orgResponse.IsSuccessStatusCode)
                    {
                        var orgJson = await orgResponse.Content.ReadAsStringAsync();
                        Organisations = JsonConvert.DeserializeObject<Organisations>(orgJson);

                        if (SelectedOrganisationId.HasValue)
                        {
                            var selectedOrg = Organisations.Orgs.FirstOrDefault(org => org.OrganisationId == SelectedOrganisationId.Value);
                            if (selectedOrg != null)
                            {
                                Domains = selectedOrg.Domains;
                            }
                        }
                    }

                    return Page();
                }
                else
                {
                    var userProfile = await UserRoleService.GetUserProfileAsync();
                    if (userProfile != null)
                    {
                        
                        var isOrgAdmin = await UserRoleService.IsUserRoleAdmin();

                        if(isOrgAdmin)
                        {
                            SelectedDomainId = userProfile.Domain.DomainId;
                            SelectedOrganisationId = userProfile.Organisation.OrganisationId;

                            HttpClient httpClient = _clientFactory.CreateClient();
                            string idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

                            // Build URL with query parameters
                            var url = $"{_usersAPI}User/GetUserApprovals?" +
                                      $"domainId={SelectedDomainId}&organisationId={SelectedOrganisationId}" +
                                      $"&searchTerm={SearchTerm}&sortBy={SortBy}&sortOrder={SortOrder}" +
                                      $"&pageNumber={CurrentPage}&pageSize={PageSize}";

                            HttpResponseMessage response = await httpClient.GetAsync(url);
                            if (response.IsSuccessStatusCode)
                            {
                                var json = await response.Content.ReadAsStringAsync();
                                var result = JsonConvert.DeserializeObject<PaginatedUserRoleApproval>(json);
                                Approvals = result.Approvals;
                                TotalCount = result.TotalCount;
                            }

                            //Get pending request
                            var pendingurl = $"{_usersAPI}User/GetUserApprovals-pending?organisationId={SelectedOrganisationId}";
                            HttpResponseMessage responsepending = await httpClient.GetAsync(pendingurl);
                            if (responsepending.IsSuccessStatusCode)
                            {
                                var json = await responsepending.Content.ReadAsStringAsync();
                                var result = JsonConvert.DeserializeObject<List<UserRoleApprovalDetail>>(json);
                                PendingApprovals = result;
                            }

                            // Get Organisations and Domains
                            var orgResponse = await httpClient.GetAsync($"{_usersAPI}Organisations/organisationsByPage?page=1&pageSize=3000");
                            if (orgResponse.IsSuccessStatusCode)
                            {
                                var orgJson = await orgResponse.Content.ReadAsStringAsync();
                                var orgs = JsonConvert.DeserializeObject<Organisations>(orgJson);

                                if (SelectedOrganisationId.HasValue)
                                {
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

                                    //var selectedOrg = Organisations.Orgs.FirstOrDefault(org => org.OrganisationId == SelectedOrganisationId.Value);
                                    //if (selectedOrg != null)
                                    //{
                                    //    Domains = selectedOrg.Domains;
                                    //}
                                }
                            }
                            return Page();
                        }
                    }
                }
            }
            return RedirectToPage("/Error/403");
        }
    }

    public class PaginatedUserRoleApproval
    {
        public List<UserRoleApprovalDetail> Approvals { get; set; }
        public int TotalCount { get; set; }
    }

    public class Organisations
    {
        public List<OrganisationDetail> Orgs { get; set; }
    }
}
