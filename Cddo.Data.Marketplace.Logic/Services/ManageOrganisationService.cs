using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cddo.Data.Marketplace.Logic.Services;

public class ManageOrganisationService : IManageOrganisationService
{
    private readonly string _apiUrl;
    private const string BaseRoute = "Organisations/";
    private readonly IAppInsightsLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public readonly IUserRoleService _userRoleService;

    public ManageOrganisationService(IAppInsightsLogger logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IUserRoleService userRoleService)
    {
        _apiUrl = configuration.GetSection("ApiSettings:UsersAPI").Value!;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _userRoleService = userRoleService;
    }

    public async Task<ManageOrganisationsResponse?> GetManageOrganisationsAsync(ManageOrganisationsRequest manageOrganisationRequest, CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
        {
            bool isAGMAdministrator = await _userRoleService.IsUserRoleSystemAdmin();
            if (isAGMAdministrator)
            {
                var token = _httpContextAccessor.HttpContext.Request.Cookies["CO-Datamarketplace"];

                try
                {
                    var response = await _apiUrl
                        .AppendPathSegments(BaseRoute, "organisationsByPage")
                        .WithOAuthBearerToken(token)
                        .SetQueryParams(new
                        {
                            page = manageOrganisationRequest?.PageNumber,
                            pageSize = manageOrganisationRequest?.PageSize,
                            allowListTrue = manageOrganisationRequest?.AllowListTrue,
                            allowListFalse = manageOrganisationRequest?.AllowListFalse,
                            searchTerm = manageOrganisationRequest?.SearchTerm,
                            organisationType = manageOrganisationRequest?.OrganisationType,
                            sortBy = manageOrganisationRequest?.SortBy,
                            sortDirection = manageOrganisationRequest?.SortDirection,
                        })
                        .GetJsonAsync<ManageOrganisationsResponse>(cancellationToken: cancellationToken);
                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Get Organisations Error ", ex);
                    return null;
                }
            }
        }
        return null;
    }
}