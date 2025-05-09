using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V1_0;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.Api.Dto.Requests.DataShareRequests;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.UI.Pages.Dataset;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("[controller]")]
[Authorize]
public class CatalogDataController : Controller
{
    private readonly ILogger<CatalogDataController> _logger;
    private readonly ICatalogDataService _catalogDataService;
    private readonly IAppInsightsLogger _appInsightlogger;
    private readonly IUserRoleService _userRoleService;


    // Lazy-loaded static property for the API base URL
    // CatalogueDataService structure can get the base URL from the appsettings!
    // You can use that pattern!
    private static readonly Lazy<string> _apiBaseUrl = new(() =>
        Environment.GetEnvironmentVariable("DM_CATALOGUE_BASE_URL")
        ?? "https://dm-server-prototype-89cdd9b9c4f8.herokuapp.com/api/v1" // Default value
    );
    // Getter for the API base URL
    private static string ApiBaseUrl => _apiBaseUrl.Value;

    private sealed class SortOptions
    {
        public required DataAssetsSortField SortField { get; init; }
        public required DataAssetsSortDirection SortDirection { get; init; }
    }
    private static readonly string AccessDeniedPage = "/Error/403";

    public CatalogDataController(ILogger<CatalogDataController> logger,
        ICatalogDataService catalogDataService, IAppInsightsLogger appInsightlogger, IUserRoleService userRoleService)
    {
        ArgumentNullException.ThrowIfNull(appInsightlogger, nameof(appInsightlogger));

        _logger = logger;
        _catalogDataService = catalogDataService;
        _appInsightlogger = appInsightlogger;
        _userRoleService = userRoleService;
    }


    [Route("GetSearchSuggestionsForPublishedDataAssets")]
    public async Task<IActionResult> GetSearchSuggestionsForPublishedDataAssets(string searchText)
    {
        var suggestions = await _catalogDataService.GetSearchSuggestionsForPublishedDataAssetsAsync(searchText);

        return Ok(suggestions.ToList());
    }

    [Route("GetSearchSuggestionsForOrganisationDataAssets")]
    public async Task<IActionResult> GetSearchSuggestionsForOrganisationDataAssets(string searchText)
    {
        var suggestions = await _catalogDataService.GetSearchSuggestionsForOrganisationDataAssetsAsync(searchText);

        return Ok(suggestions.ToList());
    }

    public static DataAssetType ConvertToDataAssetType(string dataAssetTypeString)
    {
        if (Enum.TryParse<DataAssetType>(dataAssetTypeString, ignoreCase: true, out var dataAssetType))
        {
            return dataAssetType;
        }

        throw new ArgumentException($"Invalid DataAssetType value: {dataAssetTypeString}");
    }


    [Route("GetCddoDataAssets")]
    public async Task<IActionResult> GetCddoDataAssets(
        string? searchText,
        List<string> selectedThemes,
        List<string> selectedOrganisations,
        List<string>? selectedAccessRights,
        List<DataAssetType> selectedDataAssetTypes,
        int? selectedNumberOfRecordsToShow,
        int? selectedPageNumber,
        string? sortOption)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for GetCddoDataAssets.");
        }

        if (!User.Identity!.IsAuthenticated) return RedirectToPage(AccessDeniedPage);

        var sortOptions = DetermineSortOptions();

        var organisations = await _catalogDataService.GetCddoOrganisationsAsync(
            dataAssetStatuses: [DataAssetStatus.Published]);

        var prettifiedInputOrgs = GroupOrganisationsByPrettifiedName(organisations.ToList());
        var selectedOrganisationValues = GetSelectedOrganisationValues().ToList();


        var getCddoDataAssetsRequest = new GetCddoDataAssetsRequest
        {
            SearchText = searchText,
            Themes = selectedThemes,
            Creator = selectedOrganisationValues,
            DataAssetTypes = selectedDataAssetTypes,
            AccessRights = selectedAccessRights,
            DataAssetStatuses = [DataAssetStatus.Published],
            SortField = sortOptions.SortField,
            SortDirection = sortOptions.SortDirection,
            StartRecordIndex = 0,
            NumberOfRecords = 20,
            PageNumber = selectedPageNumber ?? 1
        };

        LogSearch(getCddoDataAssetsRequest);

        var getCddoDataAssetsResponse = await _catalogDataService.GetDataAssetsAsync(getCddoDataAssetsRequest);

        var getCatalogueFilterOptions = await _catalogDataService.GetCatalogueFilterOptionsAsync(getCddoDataAssetsRequest);

        var organisationsGroupedByPrettifiedName = GroupOrganisationsByPrettifiedName(getCatalogueFilterOptions?.Organisations);

        var organisationNames = organisationsGroupedByPrettifiedName.Select(x => x.Key).ToList();



        SetViewBagPropertiesForCddoDataAssetViewing(
            getCddoDataAssetsRequest.SearchText,
            selectedThemes,
            selectedOrganisations.Count <= 0 ? [] : organisationNames,
            getCddoDataAssetsRequest.DataAssetTypes,
            getCddoDataAssetsRequest.DataAssetStatuses,
            getCddoDataAssetsRequest.NumberOfRecords,
            getCddoDataAssetsRequest.PageNumber,
            getCddoDataAssetsRequest.SortField,
            getCddoDataAssetsRequest.SortDirection,
            getCddoDataAssetsRequest.AccessRights);

        //Remove the None of the above filter option
        if (getCatalogueFilterOptions?.Topics != null)
        {
            getCatalogueFilterOptions?.Topics.Remove("None of the above");
        }

        var datasetResultsModel = new DatasetResultsModel
        {
            DataAssets = getCddoDataAssetsResponse!.CddoDataAssets,
            TotalNumberOfResults = getCddoDataAssetsResponse.TotalNumberOfMatchingCddoDataAssets,
            Topics = getCatalogueFilterOptions?.Topics,
            Organisations = organisationNames,
            DataAssetTypes = getCatalogueFilterOptions?.DataAssetTypes,
            AccessRights = getCatalogueFilterOptions?.AccessRights

        };

        return View("~/Pages/Dataset/DatasetResults.cshtml", datasetResultsModel);

        IDictionary<string, List<string>> GroupOrganisationsByPrettifiedName(List<string>? organisations)
        {
            var organisationsGroupedByName = organisations?.GroupBy(CreateOrganisationNameView);
            if (organisationsGroupedByName == null) return new Dictionary<string, List<string>>();
            return organisationsGroupedByName.ToDictionary(
                x => x.Key,
                x => x.ToList());

            string CreateOrganisationNameView(
                string organisationNameValue)
            {
                if (string.IsNullOrWhiteSpace(organisationNameValue)) return string.Empty;

                char[] delimiters = ['-', ' '];

                var inputTokens = organisationNameValue.Split(separator: delimiters,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var viewTokens = inputTokens.Select(token =>
                {
                    List<string> tokensToMakeEntirelyUpperCase = ["HM"];

                    if (tokensToMakeEntirelyUpperCase.Any(x => x.Equals(token, StringComparison.OrdinalIgnoreCase)))
                    {
                        return token.ToUpper();
                    }

                    var firstCharInUpperCase = char.ToUpper(token.First());

                    var restOfToken = token.Length > 1 ? token[1..] : string.Empty;

                    return $"{firstCharInUpperCase}{restOfToken}";
                });

                return string.Join(" ", viewTokens);
            }
        }

        IEnumerable<string> GetSelectedOrganisationValues()
        {
            foreach (var selectedOrganisation in selectedOrganisations)
            {
                if (!prettifiedInputOrgs.TryGetValue(selectedOrganisation, out var organisationGroupValues))
                    continue;

                foreach (var organisationValue in organisationGroupValues)
                {
                    yield return organisationValue;
                }
            }
        }

        SortOptions DetermineSortOptions()
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (string.IsNullOrWhiteSpace(sortOption))
                {
                    return new SortOptions { SortField = DataAssetsSortField.Relevance, SortDirection = DataAssetsSortDirection.Descending };
                }

                return ExtractSortOptions(sortOption);
            }

            if (!string.IsNullOrWhiteSpace(sortOption))
            {
                return ExtractSortOptions(sortOption);
            }

            return new SortOptions { SortField = DataAssetsSortField.UpdatedOn, SortDirection = DataAssetsSortDirection.Descending };
        }
    }

    private void LogSearch(GetCddoDataAssetsRequest getCddoDataAssetsRequest)
    {
        //Log the get event
        try
        {
            Dictionary<string, string> additionalProperties = new Dictionary<string, string>
            {
                { "SearchText", getCddoDataAssetsRequest.SearchText ?? "" }
            };
            if (getCddoDataAssetsRequest.Creator != null && getCddoDataAssetsRequest.Creator.Any())
            {
                foreach (var item in getCddoDataAssetsRequest.Creator)
                {
                    additionalProperties.Add($"Creator - {item}", item);
                }
            }

            _appInsightlogger.LogEvent(MetadataEvent.MetadataSearchPerformed, additionalProperties);
        }
        catch (Exception ex)
        {


        }

    }

    [Route("GetCddoDataAssetsByUser")]
    public async Task<IActionResult> GetCddoDataAssetsByUser(GetCddoDataAssetsRequest getCddoDataAssetsRequest, string? sortOption)
    {
        if (!User.Identity!.IsAuthenticated)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for GetCddoDataAssetsRequest: {Errors}",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

            return BadRequest(ModelState);
        }

        if (!string.IsNullOrEmpty(sortOption))
        {
            var sortOptions = ExtractSortOptions(sortOption);
            getCddoDataAssetsRequest.SortField = sortOptions.SortField;
            getCddoDataAssetsRequest.SortDirection = sortOptions.SortDirection;
        }

        LogSearch(getCddoDataAssetsRequest);

        var result = await _catalogDataService.GetDataAssetsByUserAsync(getCddoDataAssetsRequest);

        SetViewBagPropertiesForCddoDataAssetViewing(
            getCddoDataAssetsRequest.SearchText,
            getCddoDataAssetsRequest.Themes,
            getCddoDataAssetsRequest.Creator,
            getCddoDataAssetsRequest.DataAssetTypes,
            getCddoDataAssetsRequest.DataAssetStatuses,
            getCddoDataAssetsRequest.NumberOfRecords,
            getCddoDataAssetsRequest.PageNumber,
            getCddoDataAssetsRequest.SortField,
            getCddoDataAssetsRequest.SortDirection,
            getCddoDataAssetsRequest.AccessRights);

        return View("~/Pages/DataDescription/ViewDatasetDescriptions.cshtml", result);
    }

    [Route("GetCddoDataAsset")]
    public async Task<IActionResult> GetCddoDataAsset(Guid dataAssetId)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            // TODO: IMPLEMENT: Stubbed out the call to the catalog data service
            // var result = await _catalogDataService.GetDataAssetAsync(dataAssetId);

            using var httpClient = new HttpClient();
            var apiUrl = $"{ApiBaseUrl}/DataAsset/get-cddo-data-asset?DataAssetId={dataAssetId}";
            GetCddoDataAssetResponse? dataAssetResponse = null;
            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                var dataAssets = new List<CddoDataAsset>();

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    dataAssetResponse = JsonSerializer.Deserialize<GetCddoDataAssetResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (dataAssetResponse?.CddoDataAsset != null)
                    {
                        _logger.LogInformation("Successfully deserialized data asset  from API.");
                    }
                    else
                    {
                        _logger.LogWarning("No data asset  found in the API response.");
                    }

                  
                    _logger.LogInformation("Successfully fetched data asset from external API.");
                }
                else
                {
                    _logger.LogError("Failed to fetch data assets. Status Code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling the external API.");
            }

            var result = new GetCddoDataAssetResponse();
            result = dataAssetResponse;

            //END Stub

            if (result != null)
            {
                var additionalProperties = new Dictionary<string, string>
                {
                    { "dataAssetId", dataAssetId.ToString() },
                    { "title", result.CddoDataAsset.Title },
                    { "publisher", result.CddoDataAsset.PublisherPrettified },
                };
                _appInsightlogger.LogEvent(MetadataEvent.MetadataViewed, additionalProperties);

                return View("~/Pages/Dataset/DatasetSummary.cshtml", result);
            }

            return RedirectToPage("/Error/404");
        }

        return RedirectToPage(AccessDeniedPage);
    }

    [HttpGet("StartDataShareRequestPrompt/{dataAssetId}")]
    public async Task<IActionResult> StartDataShareRequestPrompt(Guid dataAssetId)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for StartDataShareRequestPrompt.");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(this.StartDataShareRequest), new { dataAssetId });
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [HttpGet("StartDataShareRequest/{dataAssetId}")]
    public async Task<IActionResult> StartDataShareRequest(Guid dataAssetId)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for StartDataShareRequest.");
        }

        if (dataAssetId == Guid.Empty) return View("~/Pages/Error/400.cshtml");

        var getDataAssetResult = await _catalogDataService.GetDataAssetAsync(dataAssetId);

        var title = getDataAssetResult!.CddoDataAsset.Title;

        var dataShareRequest = new DataShareRequest
        {
            Title = title,
            DatasetId = dataAssetId
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userResponse = await _userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

            userEventProperties.Add("datasetId", dataAssetId.ToString());
            userEventProperties.Add("title", title ?? "");

            _appInsightlogger.LogEventMainBase(UserEvent.UserPageNavigation, "DataShareRequest", "CDDO", "", "", "", userEventProperties);
        }

        return View("~/Pages/DataShare/DataShareRequest.cshtml", dataShareRequest);
    }

    [Route("GetCddoDataAssetByUser")]
    public async Task<IActionResult> GetCddoDataAssetByUser(string identifier)
    {
        if (User.Identity!.IsAuthenticated)
        {
            var result = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            var additionalProperties = new Dictionary<string, string>
            {
                { "dataAssetId", identifier }
            };
            _appInsightlogger.LogEvent(MetadataEvent.MetadataViewed, additionalProperties);
            return View("~/Pages/DataDescription/ViewDataAssetSummary.cshtml", result!.CddoDataAsset);
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [Route("DeleteCddoDataAssetConfirmation")]
    public async Task<IActionResult> DeleteCddoDataAssetConfirmation(DeleteDataAssetModel deleteDataAssetModel)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for DeleteCddoDataAssetConfirmation.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            return View("~/Pages/DataDescription/DeleteDataAssetConfirmation.cshtml", deleteDataAssetModel);
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [Route("DeleteCddoDataAsset")]
    public async Task<IActionResult> DeleteCddoDataAssetSubmit(DeleteDataAssetModel deleteDataAssetModel)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            if (validationErrors.Count() > 0)
            {
                _appInsightlogger.LogEvent(MetadataEvent.MetadataDeleted, new Dictionary<string, string>
            {
                { "validationErrors", string.Join(", ", validationErrors) },
                { "identifier", deleteDataAssetModel.Identifier },
                { "title", deleteDataAssetModel.Title }
            });
            }
        }

        if (User.Identity!.IsAuthenticated)
        {
            var request = new DeleteProfiledDataAssetRequest
            {
                DataAssetId = new Guid(deleteDataAssetModel.Identifier),
            };

            try
            {
                await _catalogDataService.DeleteDataAssetAsync(request);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage(AccessDeniedPage);
            }

            var userresponse = await _userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userresponse);

            userEventProperties.Add("esdaId", deleteDataAssetModel.Identifier);
            userEventProperties.Add("esdaName", deleteDataAssetModel.Title);

            _appInsightlogger.LogEvent(MetadataEvent.MetadataDeleted, userEventProperties);
            return RedirectToAction(nameof(GetCddoDataAssetsByUser), new GetCddoDataAssetsRequest());
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [Route("ArchiveCddoDataAssetConfirmation")]
    public IActionResult ArchiveCddoDataAssetConfirmation(ArchiveDataAssetModel archiveDataAssetModel)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }

        if (User.Identity!.IsAuthenticated)
        {
            return View("~/Pages/DataDescription/ArchiveDataAssetConfirmation.cshtml", archiveDataAssetModel);
        }
        return RedirectToPage(AccessDeniedPage);
    }

    private static SortOptions ExtractSortOptions(string sortOptionInput)
    {
        var options = sortOptionInput.Split('|');

        var sortField = DataAssetsSortField.UpdatedOn;
        var sortDirection = DataAssetsSortDirection.Descending;

        if (options.Length == 2)
        {
            if (Enum.TryParse(options[0], out DataAssetsSortField fieldValue))
            {
                sortField = fieldValue;
            }
            if (Enum.TryParse(options[1], out DataAssetsSortDirection directionValue))
            {
                sortDirection = directionValue;
            }
        }

        return new SortOptions
        {
            SortField = sortField,
            SortDirection = sortDirection
        };
    }
    private void SetViewBagPropertiesForCddoDataAssetViewing(
        string? searchText,
        List<string>? selectedTopics,
        List<string>? selectedOrganisations,
        List<DataAssetType>? selectedDataAssetTypes,
        List<DataAssetStatus>? selectedDataAssetStatuses,
        int selectedNumberOfRecords,
        int selectedPageNumber,
        DataAssetsSortField selectedSortField,
        DataAssetsSortDirection selectedSortDirection,
        List<string>? accessRights)
    {
        ViewBag.SearchText = searchText ?? string.Empty;
        ViewBag.Themes = selectedTopics ?? [];
        ViewBag.Creator = selectedOrganisations ?? [];
        ViewBag.DataAssetTypes = selectedDataAssetTypes ?? [];
        ViewBag.DataAssetStatuses = selectedDataAssetStatuses ?? [];
        ViewBag.NumberOfRecords = selectedNumberOfRecords;
        ViewBag.PageNumber = selectedPageNumber;
        ViewBag.SortField = selectedSortField;
        ViewBag.SortDirection = selectedSortDirection;
        ViewBag.AccessRights = accessRights;
    }
}