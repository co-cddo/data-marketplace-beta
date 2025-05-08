using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Requests.Lookup;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl.Http.Configuration;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Agm.Catalog.DotNet.Dto.Responses.Lookup;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.UI.Model;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using System.Web;

namespace Cddo.Data.Marketplace.UI.Services;

public class CatalogDataService : ICatalogDataService
{
    private readonly string _apiUrl;
    private readonly ILogger<CatalogDataService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICddoFlurlExceptionBuilder _cddoFlurlExceptionBuilder;

    private readonly string _profileId = "dcat-ukap-v3.1";

    public CatalogDataService(
        ILogger<CatalogDataService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ICddoFlurlExceptionBuilder cddoFlurlExceptionBuilder)
    {
        _apiUrl = configuration.GetSection("Api:Main").Value!;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _cddoFlurlExceptionBuilder = cddoFlurlExceptionBuilder;
    }
    // in this class, there is a better pattern for getting the base url (from appsettings)
    private static readonly Lazy<string> _apiBaseUrl = new(() =>
    Environment.GetEnvironmentVariable("DM_CATALOGUE_BASE_URL")
    ?? "https://dm-server-prototype-89cdd9b9c4f8.herokuapp.com/api/v1" // Default value
);
    // Getter for the API base URL
    private static string ApiBaseUrl => _apiBaseUrl.Value;

    private string? GetToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.User.Identity.IsAuthenticated)
        {
            // Try to get the Bearer token from the Authorization header
            var authorizationHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                return authorizationHeader.Substring("Bearer ".Length).Trim(); // Extract token
            }
        }
        return null;
    }

    public async Task<IEnumerable<string>> GetCddoTopicsAsync(
        IEnumerable<DataAssetStatus>? dataAssetStatuses = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();
            if (token == null)
            {
                return Enumerable.Empty<string>();
            }

            var getCddoTopicsRequest = new GetCddoTopicsRequest
            {
                DataAssetStatuses = dataAssetStatuses?.ToList()
            };

            var response = await _apiUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegments("Topics")
                .WithOAuthBearerToken(token)
                .SetQueryParams(getCddoTopicsRequest)
                .GetJsonAsync<GetCddoTopicsResponse>(cancellationToken: cancellationToken);

            return response.Topics;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get CDDO Topics. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetCddoOrganisationsAsync(
        IEnumerable<DataAssetStatus>? dataAssetStatuses = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();
            if (token == null)
            {
                return Enumerable.Empty<string>();
            }

            var getCddoOrganisationsRequest = new GetCddoOrganisationsRequest
            {
                DataAssetStatuses = dataAssetStatuses?.ToList()
            };

            var response = await _apiUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegments("Organisations")
                .WithOAuthBearerToken(token)
                .SetQueryParams(getCddoOrganisationsRequest)
                .GetJsonAsync<GetCddoOrganisationsResponse>(cancellationToken: cancellationToken);

            return response.Organisations;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get CDDO Organisations. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsForPublishedDataAssetsAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();
            if (token == null)
            {
                return Enumerable.Empty<string>();
            }
            var getSearchSuggestionsForPublishedDataAssetsRequest = new GetSearchSuggestionsForPublishedDataAssetsRequest
            {
                SearchText = searchText
            };

            var response = await _apiUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegment("DataAsset/get-search-suggestions-for-published-data-assets")
                .WithOAuthBearerToken(token)
                .SetQueryParams(getSearchSuggestionsForPublishedDataAssetsRequest)
                .GetJsonAsync<GetSearchSuggestionsForPublishedDataAssetsResponse>(cancellationToken: cancellationToken);

            return response.SearchSuggestionsForPublishedDataAssets;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get Search Suggestions For Published Data Assets. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsForOrganisationDataAssetsAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();
            if (token == null)
            {
                return Enumerable.Empty<string>();
            }
            var getSearchSuggestionsForOrganisationDataAssetsRequest = new GetSearchSuggestionsForOrganisationDataAssetsRequest
            {
                SearchText = searchText
            };

            var response = await _apiUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegment("DataAsset/get-search-suggestions-for-organisation-data-assets")
                .WithOAuthBearerToken(token)
                .SetQueryParams(getSearchSuggestionsForOrganisationDataAssetsRequest)
                .GetJsonAsync<GetSearchSuggestionsForOrganisationDataAssetsResponse>(cancellationToken: cancellationToken);

            return response.SearchSuggestionsForOrganisationDataAssets;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get Search Suggestions For Organisation Data Assets. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<CheckForPotentialDuplicatesToDataAssetResponse?> CheckForPotentialDuplicatesToDataAssetAsync(
        Guid dataAssetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();

            if (string.IsNullOrWhiteSpace(token)) return null;

            // TODO: IMPLEMENT: Stubbed out the call CheckForPotentialDuplicatesToDataAssetRequest
            //var url = _apiUrl
            //    .AppendPathSegments("DataAsset/check-for-potential-duplicates-to-data-asset");
            var url = $"{ApiBaseUrl}/DataAsset/check-for-potential-duplicates-to-data-asset";
            // END STUB

            var input = new CheckForPotentialDuplicatesToDataAssetRequest
            {
                DataAssetId = dataAssetId
            };

            var response = await url
                .AppendQueryParam(input)
                .WithOAuthBearerToken(token)
                .GetJsonAsync<CheckForPotentialDuplicatesToDataAssetResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            var responseString = await ex.GetResponseStringAsync();
            _logger.LogError(ex, "Flurl HTTP Exception checking for potential duplicates of data asset. Flurl Response: {responseString}", responseString);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception checking for potential duplicates of data asset: {Message}", ex.Message);

            return null;
        }
    }

    public async Task<GetCddoDataAssetsResponse?> GetDataAssetsAsync
        (GetCddoDataAssetsRequest getCddoDataAssetsRequest, CancellationToken cancellationToken = default)
    {
        getCddoDataAssetsRequest.OnlyIncludeRecordsDiscoverableByOrganisationOfCallingUser = false;
        getCddoDataAssetsRequest.OnlyIncludeRecordsOwnedByOrganisationOfCallingUser = false;

        getCddoDataAssetsRequest.StartRecordIndex = (getCddoDataAssetsRequest.PageNumber - 1) * getCddoDataAssetsRequest.NumberOfRecords;

        try
        {
            var token = GetToken();
            // All responses from the Marketplace Api are serialized so that enums are returned as strings rather than
            // their integer value, so we have to read them back with the same conversion.
            //var response = await _apiUrl
            //    .WithSettings(x =>
            //        x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
            //        {
            //            Converters = { new JsonStringEnumConverter() },
            //            PropertyNameCaseInsensitive = true
            //        }))
            //    .AppendPathSegment("DataAsset/get-cddo-data-assets")
            //    .WithOAuthBearerToken(token)
            //    .SetQueryParams(getCddoDataAssetsRequest)
            //    .GetJsonAsync<GetCddoDataAssetsResponse>(cancellationToken: cancellationToken);

            // TODO: IMPLEMENT: Stubbed out the call to the catalog data service
            var baseUrl = $"{ApiBaseUrl}/";
            var response = await baseUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegment("DataAsset/get-cddo-data-assets")
                .WithOAuthBearerToken(token)
                .SetQueryParams(getCddoDataAssetsRequest)
                .GetJsonAsync<GetCddoDataAssetsResponse>(cancellationToken: cancellationToken);

        
            //END Stub


            return response;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get Data Descriptions. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting data descriptions results.");
            return null;
        }
    }
    public async Task<CatalogueFilterOptions?> GetCatalogueFilterOptionsAsync
        (GetCddoDataAssetsRequest getCddoDataAssetsRequest, CancellationToken cancellationToken = default)
    {
        getCddoDataAssetsRequest.OnlyIncludeRecordsDiscoverableByOrganisationOfCallingUser = false;
        getCddoDataAssetsRequest.OnlyIncludeRecordsOwnedByOrganisationOfCallingUser = false;

        getCddoDataAssetsRequest.StartRecordIndex = 0;

        try
        {
            var token = GetToken();
            // All responses from the Marketplace Api are serialized so that enums are returned as strings rather than
            // their integer value, so we have to read them back with the same conversion.

            //TODO: IMPELEMENT: Stubbed out the call GetCatalogueFilterOptionsRequest
            //var response = await _apiUrl
            //    .WithSettings(x =>
            //        x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
            //        {
            //            Converters = { new JsonStringEnumConverter() },
            //            PropertyNameCaseInsensitive = true
            //        }))
            //    .AppendPathSegment("FilteredMenuOptions")
            //    .WithOAuthBearerToken(token)
            //    .SetQueryParams(getCddoDataAssetsRequest)
            //    .GetJsonAsync<CatalogueFilterOptions>(cancellationToken: cancellationToken);

            // TODO: CALL ROB's API to RETRIEVE THESE VALUES !!
            var sampleCatalogueFilterOptions = new CatalogueFilterOptions
            {
                Organisations = new List<string> {
                    "Department For Business And Trade",
                    "Department For Education",
                    "Department For Energy Security And Net Zero",
                    "Department For Environment Food & Rural Affairs",
                    "Driver And Vehicle Standards Agency",
                    "Government Property Agency",
                    "HM Revenue & Customs",
                    "HM Treasury",
                    "Home Office",
                    "Ministry Of Housing Communities Local Government",
                    "Ministry Of Justice",
                    "Office For National Statistics",
                    "Ordnance Survey"
                },
                Topics = new List<string> {
                    "Business, economics and finance",
                    "Crime and justice",
                    "Culture, leisure and sport",
                    "Education",
                    "Energy",
                    "Environment and nature",
                    "Geography",
                    "Government and public sector",
                    "Health and care",
                    "Population and society",
                    "Transport and infrastructure"
                    },
                DataAssetTypes = new List<string> { "Dataset", "Service", "API" },
                AccessRights = new List<string> { "OPEN", "SOMETHING_ELSE" }
            };
 
            var response = sampleCatalogueFilterOptions;
            //END Stub



            return response;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get Data Descriptions. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting data descriptions results.");
            return null;
        }
    }

    public async Task<GetCddoDataAssetsResponse?> GetDataAssetsByUserAsync
        (GetCddoDataAssetsRequest getCddoDataAssetsRequest, CancellationToken cancellationToken = default)
    {
        getCddoDataAssetsRequest.OnlyIncludeRecordsDiscoverableByOrganisationOfCallingUser = true;
        getCddoDataAssetsRequest.OnlyIncludeRecordsOwnedByOrganisationOfCallingUser = true;

        getCddoDataAssetsRequest.StartRecordIndex = (getCddoDataAssetsRequest.PageNumber - 1) * getCddoDataAssetsRequest.NumberOfRecords;

        try
        {
            var token = GetToken();
            if (!string.IsNullOrEmpty(token))
            {

                // All responses from the Marketplace Api are serialized so that enums are returned as strings rather than
                // their integer value, so we have to read them back with the same conversion.
                //var response = await _apiUrl
                //    .WithSettings(x =>
                //        x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                //        {
                //            Converters = { new JsonStringEnumConverter() },
                //            PropertyNameCaseInsensitive = true
                //        }))
                //    .AppendPathSegment("DataAsset/get-cddo-data-assets")
                //    .WithOAuthBearerToken(token)
                //    .SetQueryParams(getCddoDataAssetsRequest)
                //    .GetJsonAsync<GetCddoDataAssetsResponse?>(cancellationToken: cancellationToken);

                var baseUrl = $"{ApiBaseUrl}/";

                var response = await baseUrl
                    .WithSettings(x =>
                        x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                        {
                            Converters = { new JsonStringEnumConverter() },
                            PropertyNameCaseInsensitive = true
                        }))
                    .AppendPathSegment("DataAsset/get-cddo-data-assets")
                    .WithOAuthBearerToken(token)
                    .SetQueryParams(getCddoDataAssetsRequest)
                    .GetJsonAsync<GetCddoDataAssetsResponse?>(cancellationToken: cancellationToken);

                return response;
            }
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get Data Descriptions By User. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting data descriptions results.");
            return null;
        }
        return null;
    }

    public async Task<GetCddoDataAssetResponse?> GetDataAssetAsync
        (Guid dataAssetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();
            // All responses from the Marketplace Api are serialized so that enums are returned as strings rather than
            // their integer value, so we have to read them back with the same conversion.
            var response = await _apiUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegments("DataAsset/get-cddo-data-asset")
                .WithOAuthBearerToken(token)
                .SetQueryParam("dataAssetId", dataAssetId)
                .GetJsonAsync<GetCddoDataAssetResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "Failed to Get Data Description. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting the data set: {DataAssetId}", dataAssetId);
            return null;
        }
    }

    public async Task<GetCddoDataAssetValidationErrorsResponse?> GetDataAssetValidationErrorsAsync(
        Guid dataAssetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = GetToken();

            var response = await _apiUrl
                .WithSettings(x =>
                    x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                .AppendPathSegments("DataAsset/validate-cddo-data-asset")
                .WithOAuthBearerToken(token)
                .AppendQueryParam("dataAssetId", dataAssetId)
                .GetJsonAsync<GetCddoDataAssetValidationErrorsResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
            _logger.LogError(ex, "CDDO Data Description Validation Failed. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while validating the CDDO data description: {DatasetId}", dataAssetId);
            return null;
        }
    }

    public async Task<DeleteProfiledDataAssetResponse?> DeleteDataAssetAsync(
        DeleteProfiledDataAssetRequest deleteProfiledDataAssetRequest, CancellationToken cancellationToken = default)
    {

        try
        {
            var token = GetToken();

            if (!string.IsNullOrEmpty(token))
            {
                deleteProfiledDataAssetRequest.ProfileId = _profileId;

                var response = await _apiUrl
                    .AppendPathSegments("DataAsset/delete-profiled-data-asset")
                    .WithOAuthBearerToken(token)
                    .SetQueryParams(deleteProfiledDataAssetRequest)
                    .DeleteAsync(cancellationToken: cancellationToken)
                    .ReceiveJson<DeleteProfiledDataAssetResponse>();
                return response;
            }
            }
            catch (FlurlHttpException ex)
            {
                var statusCode = ex.StatusCode;
                if (statusCode == (int)HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException();
                }

                var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
                _logger.LogError(ex, "Failed to Delete Data Description. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the data set. Error: {ErrorMessage}", ex.Message);
                return null;
            }
        return null;
    }
}
