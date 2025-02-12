using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cddo.Data.Marketplace.Logic.Services;

public class ManageOrganisationsService : IManageOrganisationsService
{
    private readonly IAppInsightsLogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _apiUrl;
    private const string BaseRoute = "Organisations";

    public ManageOrganisationsService(
        IAppInsightsLogger logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(configuration);
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        _apiUrl = configuration.GetSection("ApiSettings:UsersAPI").Value!;
    }

    public async Task<OrganisationDetail?> GetOrganisationAsync(int organisationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies["CO-Datamarketplace"];

            var response = await _apiUrl
                .AppendPathSegments(BaseRoute, organisationId)
                .WithOAuthBearerToken(token)
                .GetJsonAsync<OrganisationDetail>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            var error = ex.GetResponseStringAsync();
            _logger.LogError($"Get Organisation Error. OrganisationId: {organisationId}. {error}", ex);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get Organisation Error. OrganisationId: {organisationId}", ex);
            return null;
        }
    }

    public async Task UpdateDataShareRequestMailboxAddress(
        int domainId,
        string? dataShareRequestMailboxAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies["CO-Datamarketplace"];

            await _apiUrl
                .AppendPathSegments(BaseRoute, "domains", domainId, "dataShareRequestMailboxAddress")
                .WithOAuthBearerToken(token)
                .PatchJsonAsync(dataShareRequestMailboxAddress, cancellationToken: cancellationToken);
        }
        catch (FlurlHttpException ex)
        {
            var error = ex.GetResponseStringAsync();
            _logger.LogError($"Update Domain Details Error. DomainId: {domainId}. {error}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update Domain Details Error. DomainId: {domainId}", ex);
        }
    }
}