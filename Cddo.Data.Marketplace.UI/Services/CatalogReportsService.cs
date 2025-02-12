using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl.Http;
using Flurl;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Flurl.Http.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;

namespace Cddo.Data.Marketplace.UI.Services;

public class CatalogReportsService : ICatalogReportsService
{
    private const string dataAssetProfileId = "dcat-ukap-v1.0";

    private readonly string _apiUrl;
    private readonly string _usersUrl;
    private readonly string _dsrApiUrl;
    private readonly ILogger<CatalogReportsService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRoleService _userRoleService;

    public CatalogReportsService(
        ILogger<CatalogReportsService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IUserRoleService userRoleService)
    {
        _apiUrl = configuration.GetSection("Api:Main").Value!;
        _usersUrl = configuration.GetSection("ApiSettings:UsersAPI").Value!;
        _dsrApiUrl = configuration.GetSection("Api:DataShare").Value!;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _userRoleService = userRoleService;
    }

    private async Task<string?> GetTokenAsync()
    {
        if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string idToken = httpContext.Request.Cookies["CO-Datamarketplace"];
            return idToken;
        }
        return null;
    }
    private const string FlurlHttpExceptionMessage = "Flurl HTTP Exception: {ResponseString}";
    //DownloadCatalogReportsDataAsync
    public async Task<QueryCatalogReportsDataResponse?> DownloadCatalogReportsDataAsync(QueryCatalogReportsDataRequest queryCatalogReportsDataRequest, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var url = _apiUrl.AppendPathSegment("download-catalog-reports-data");
                var mappedFilter = AddOrganisationMapping(queryCatalogReportsDataRequest);
                var response = await url
                    .WithOAuthBearerToken(token)
                    .WithSettings(x =>
                            x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                            {
                                Converters = { new JsonStringEnumConverter() },
                                PropertyNameCaseInsensitive = true
                            }))
                    .PostJsonAsync(mappedFilter, cancellationToken: cancellationToken)
                    .ReceiveJson<QueryCatalogReportsDataResponse>();
                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
        return null;
    }

    public async Task<QueryCatalogReportsDataResponse?> GetCatalogReportsDataAsync(QueryCatalogReportsDataRequest queryCatalogReportsDataRequest, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var url = _apiUrl.AppendPathSegment("query-catalog-reports-data");
                var mappedFilter = AddOrganisationMapping(queryCatalogReportsDataRequest);
                var response = await url
                    .WithOAuthBearerToken(token)
                    .WithSettings(x =>
                            x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                            {
                                Converters = { new JsonStringEnumConverter() },
                                PropertyNameCaseInsensitive = true
                            }))
                    .PostJsonAsync(mappedFilter, cancellationToken: cancellationToken)
                    .ReceiveJson<QueryCatalogReportsDataResponse>();
                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
        return null;
    }

    public async Task<LogsQueryDataResult> GetTelemetryReportsDataAsync(string searchQuery, string timeRange, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var url = _usersUrl.AppendPathSegments("User", "GetEventLogs");

                var cleanQuery = searchQuery.Replace("\r", "").Replace("\n", "").Replace("\\", "");

                var response = await url
                .WithOAuthBearerToken(token)
                .WithSettings(x =>
                            x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                            {
                                Converters = { new JsonStringEnumConverter() },
                                PropertyNameCaseInsensitive = true
                            }))
                .SetQueryParam("searchQuery", cleanQuery)
                .SetQueryParam("timeRange", timeRange)
                .GetJsonAsync<LogsQueryDataResult>(cancellationToken: cancellationToken);

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
        return null;
    }

    public async Task<QueryDataShareRequestsCountsResponse> GetDSRReportDataAsync(QueryDataShareRequestsCountsRequest queryDataShareRequestsCountsRequest, CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
        {
            var roles = new List<string> { "System Administrator", "Metadata Publisher", "Data Request Approver", "Organisation Administrator" };
            bool isAGMAdministrator = await _userRoleService.IsUserInRoleAsync(roles);
            if (isAGMAdministrator)
            {
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var response = await _dsrApiUrl
                            .AppendPathSegments("Reporting", "QueryDataShareRequestCounts")
                            .WithOAuthBearerToken(token)
                            .PostJsonAsync(queryDataShareRequestsCountsRequest, cancellationToken: cancellationToken)
                            .ReceiveJson<QueryDataShareRequestsCountsResponse>();

                        return response;
                    }
                    catch (FlurlHttpException ex)
                    {
                        var responseString = await ex.GetResponseStringAsync();
                        _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                }
            }
        }
        return null;
    }
    public string PrettifyString(string? input)
    {
        if (input == null)
        {
            return "";
        }

        return string.Join(" ", input.Split('-').Select(w => char.ToUpper(w[0]) + w.Substring(1)));
    }
    private static QueryCatalogReportsDataRequest AddOrganisationMapping(QueryCatalogReportsDataRequest? dataCatalogueRequest)
    {
        if (dataCatalogueRequest != null && dataCatalogueRequest.Filter!.FieldFilters.Any(f => f.Field == CatalogAssetField.Publisher))
        {
            var selectedOrgFilter = dataCatalogueRequest.Filter.FieldFilters.Where(f => f.Field == CatalogAssetField.Publisher).FirstOrDefault();
            if (selectedOrgFilter != null)
            {
                var selectedOrgFiltersValues = new List<string>();

                foreach (var currentVal in selectedOrgFilter.Values)
                {
                    if (!currentVal.Contains("-"))
                    {
                        selectedOrgFiltersValues.Add(currentVal.ToLower().Replace(" ", "-"));
                    }
                    else
                    {
                        selectedOrgFiltersValues.Add(string.Join(" ", currentVal.Split('-').Select(w => char.ToUpper(w[0]) + w.Substring(1))));

                    }
                }

                dataCatalogueRequest.Filter.FieldFilters.Where(f => f.Field == CatalogAssetField.Publisher).FirstOrDefault()?.Values.AddRange(selectedOrgFiltersValues);
            }

        }
        
        return dataCatalogueRequest;
    }
}
