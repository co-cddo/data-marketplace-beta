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
        // TODO: IMPLEMENT: Stubbed out the call to the catalog data service
        //    var getCddoDataAssetsResponse = await _catalogDataService.GetDataAssetsAsync(getCddoDataAssetsRequest);

        var myList = new List<CddoDataAsset>
        {
            new CddoDataAsset
             {
                        Id = Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d22"),
                        InternalIdentifier = "acme-123",
                        Modified = DateTime.Parse("2023-12-09T16:09:53+00:00"),
                        DataAssetStatus = DataAssetStatus.Published,
                        Title = "Advance Passenger Information II",
                        Description = "Travel data and personal data given to airlines by passenger. API covers both inbound and outbound air passengers. API includes the passenger’s full name, nationality, date of birth, gender and travel document number, type and country of issue. The data does not include those arriving by sea or rail routes, by private aircraft or via the Common Travel Area (CTA).",
                        DataAssetType = DataAssetType.DataSet,
                        Themes = new List<string>
                {
                    "Transport and infrastructure",
                    "Population and society"
                },
                        Keywords = new List<string>
                {
                    "Air travel",
                    "Passport",
                    "Airports",
                    "leaving UK",
                    "entering UK"
                },
                        DataAssetContacts = new List<CddoDataAssetContact>
                {
                    new CddoDataAssetContact
                    {
                        Name = "Rob Nichols",
                        Email = "robert.nichols@digital.cabinet-office.gov.uk",
                        Role = DataAssetContactRoleType.Contact
                    }
                },
                        License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                        {
                            Title = "Creative Commons",
                            LicenseUrl = "https://creativecommons.org/licenses/by/4.0"
                        },
                        Publisher = "academy-for-social-justice",
                        SecurityClassification = "OFFICIAL",
                        AccessRights = "RESTRICTED",
                        DataAssetDistribution = new List<CddoDataAssetDistribution>
                {
                    new CddoDataAssetDistribution
                    {
                        Title = "CSV download",
                        Description = "Complete dataset provided as a downloadable file",
                        AccessService = "8d085327-21b6-4d8b-9705-88faad231d23",
                        AccessUrl = "http://example.com/path/to/file.csv",
                        Format = "CSV file",
                        MediaType =  "text/csv"
                    },
                    new CddoDataAssetDistribution
                    {
                        Title = "Rest API",
                        AccessUrl = "http://example.com/api/",
                        Description = "A fully queryable REST API with JSON and XML output",
                        Format = "API"
                    },
                    new CddoDataAssetDistribution
                    {
                        Title = "Web Page",
                        Description = "A web page that provides the data, links to downloads, or documentation.",
                        AccessUrl = "http://example.com/path/to/page",
                        Format = "Web page",
                        MediaType = "text/html"
                    }
                }
             },
            new CddoDataAsset
            {
                Id = Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d23"),
                InternalIdentifier = "acme-123",
                Modified = DateTime.Parse("2023-12-09T16:09:53+00:00"),
                DataAssetStatus = DataAssetStatus.Published,
                Title = "Advance Passenger Information",
                Description = "Travel data and personal data given to airlines by passenger. API covers both inbound and outbound air passengers. API includes the passenger’s full name, nationality, date of birth, gender and travel document number, type and country of issue. The data does not include those arriving by sea or rail routes, by private aircraft or via the Common Travel Area (CTA).",
                DataAssetType = DataAssetType.DataService,
                DataAssetServiceType = DataAssetServiceType.Rest,
                Themes = new List<string>
                {
                    "Transport and infrastructure",
                    "Population and society"
                },
                Keywords = new List<string>
                {
                    "Air travel",
                    "Passport",
                    "Airports",
                    "leaving UK",
                    "entering UK"
                },
                DataAssetContacts = new List<CddoDataAssetContact>
                {
                    new CddoDataAssetContact
                    {
                        Name = "Rob Nichols",
                        Email = "robert.nichols@digital.cabinet-office.gov.uk",
                        Role = DataAssetContactRoleType.Contact
                    }
                },
                Publisher = "academy-for-social-justice",
                SecurityClassification = "OFFICIAL",
                AccessRights = "RESTRICTED",
                EndpointDescription = "http://example.com/path/to/swagger",
                EndpointUrl = "http://example.com/api/v1",
                RelatedResources = new List<string>
                {
                    "8d085327-21b6-4d8b-9705-88faad231d22"
                }
            },
            new CddoDataAsset
            {
                Id = Guid.NewGuid(),
                OrganisationId = 1,
                DomainId = 101,
                ProfileId = "Profile-001",
                DataAssetType = DataAssetType.DataSet,
                Title = "Sample Data Asset 1",
                AlternativeTitles = new List<string> { "Alt Title 1", "Alt Title 2" },
                Summary = "This is a summary of the first data asset.",
                Description = "Detailed description of the first data asset.",
                ManifestationTypes = new List<string> { "Type1", "Type2" },
                InternalIdentifier = "INT-001",
                Publisher = "Sample Publisher",
                Author = "Author Name",
                AuthorEmail = "author1@example.com",
                DataAssetContacts = new List<CddoDataAssetContact>
                {
                    new CddoDataAssetContact
                    {
                        Name = "Contact Name 1",
                        Email = "contact1@example.com",
                        Role =  Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums.DataAssetContactRoleType.Contact
                    }
                },
                SecurityClassification = "Public",
                Version = "1.0",
                LicenseId = "LIC-001",
                LicenseTitle = "Open License",
                Issued = DateTime.UtcNow.AddDays(-30),
                Modified = DateTime.UtcNow.AddDays(-10),
                Created = DateTime.UtcNow.AddDays(-60),
                DraftStatus = "Draft",
                PublishStatus = "Published",
                DataAssetStatus = DataAssetStatus.Published,
                UpdateFrequencyString = "Monthly",
                AccessRights = "Open",
                EndpointUrl = "https://example.com/api/dataasset1",
                EntryType = "Dataset",
                Keywords = new List<string> { "Keyword1", "Keyword2" },
                Themes = new List<string> { "Theme1", "Theme2" },
                RelatedResources = new List<string> { "Resource1", "Resource2" },
                RequiresDSR = true,
                AllowDSRRequest = false,
                EndpointDescription = "API endpoint for the first data asset.",
                DataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaContactPointEmailAddress,
                CustomDsrNotificationAddress = "dsr@example.com",
                CatalogResourceCreated = DateTime.UtcNow.AddDays(-90),
                CatalogResourceCreator = "Catalog Creator",
                License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                {
                    LicenseUrl = "LIC-001",
                    Title = "Open License"
                }
            },
            new CddoDataAsset
            {
                Id = Guid.NewGuid(),
                OrganisationId = 2,
                DomainId = 102,
                ProfileId = "Profile-002",
                DataAssetType = DataAssetType.DataSet,
                Title = "Sample Data Asset 2",
                AlternativeTitles = new List<string> { "Alt Title A", "Alt Title B" },
                Summary = "This is a summary of the second data asset.",
                Description = "Detailed description of the second data asset.",
                ManifestationTypes = new List<string> { "TypeA", "TypeB" },
                InternalIdentifier = "INT-002",
                Publisher = "Another Publisher",
                Author = "Another Author",
                AuthorEmail = "author2@example.com",
                DataAssetContacts = new List<CddoDataAssetContact>
                {
                    new CddoDataAssetContact
                    {
                        Name = "Contact Name 2",
                        Email = "contact2@example.com",
                        Role =  DataAssetContactRoleType.Owner
                    }
                },
                SecurityClassification = "Restricted",
                Version = "2.0",
                LicenseId = "LIC-002",
                LicenseTitle = "Restricted License",
                Issued = DateTime.UtcNow.AddDays(-20),
                Modified = DateTime.UtcNow.AddDays(-5),
                Created = DateTime.UtcNow.AddDays(-40),
                DraftStatus = "Final",
                PublishStatus = "Unpublished",
                DataAssetStatus = DataAssetStatus.Published,
                UpdateFrequencyString = "Weekly",
                AccessRights = "Restricted",
                EndpointUrl = "https://example.com/api/dataasset2",
                EntryType = "Service",
                Keywords = new List<string> { "KeywordA", "KeywordB" },
                Themes = new List<string> { "ThemeA", "ThemeB" },
                RelatedResources = new List<string> { "ResourceA", "ResourceB" },
                RequiresDSR = false,
                AllowDSRRequest = true,
                DataAssetServiceType = DataAssetServiceType.Event,
                DataAssetDistribution = new List<CddoDataAssetDistribution>
                {
                    new CddoDataAssetDistribution
                    {
                        Format = "CSV",
                        AccessUrl = "https://example.com/api/dataasset2/distribution"
                    }
                },
                ServiceStatus = ServiceStatusEnum.Beta,
                EndpointDescription = "API endpoint for the second data asset.",
                DataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaContactPointEmailAddress,
                CustomDsrNotificationAddress = null,
                CatalogResourceCreated = DateTime.UtcNow.AddDays(-80),
                CatalogResourceCreator = "Another Catalog Creator",
                License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                {
                    LicenseUrl = "LIC-002",
                    Title = "Open License"
                }
            }
        };

        Agm.Catalog.DotNet.Dto.Responses.DataAssets.GetCddoDataAssetsResponse test = new Agm.Catalog.DotNet.Dto.Responses.DataAssets.GetCddoDataAssetsResponse();
        test.TotalNumberOfMatchingCddoDataAssets = 2;

        test.CddoDataAssets = myList;

        var getCddoDataAssetsResponse = test;

   //     var getCatalogueFilterOptions = await _catalogDataService.GetCatalogueFilterOptionsAsync(getCddoDataAssetsRequest)
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
                Topics = new List<string> {         "Business, economics and finance",
        "Crime and justice",
        "Culture, leisure and sport",
        "Education",
        "Energy",
        "Environment and nature",
        "Geography",
        "Government and public sector",
        "Health and care",
        "Population and society",
        "Transport and infrastructure" },
                DataAssetTypes = new List<string> { "Dataset", "Service", "API" },
                AccessRights = new List<string> { "OPEN", "SOMETHING_ELSE" }
            };
        var getCatalogueFilterOptions = sampleCatalogueFilterOptions;
        //END Stub

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

            var result = new GetCddoDataAssetResponse();
            if (dataAssetId == Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d23") )
            {
                result.CddoDataAsset =
                new CddoDataAsset
                {
                    AllowDSRRequest = true,
                    RequiresDSR = true,
                    Id = Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d23"),
                    InternalIdentifier = "acme-123",
                    Modified = DateTime.Parse("2023-12-09T16:09:53+00:00"),
                    DataAssetStatus = DataAssetStatus.Published,
                    Title = "Advance Passenger Information",
                    Description = "Travel data and personal data given to airlines by passenger. API covers both inbound and outbound air passengers. API includes the passenger’s full name, nationality, date of birth, gender and travel document number, type and country of issue. The data does not include those arriving by sea or rail routes, by private aircraft or via the Common Travel Area (CTA).",
                    DataAssetType = DataAssetType.DataService,
                    DataAssetServiceType = DataAssetServiceType.Rest,
                    Themes = new List<string>
                    {
                        "Transport and infrastructure",
                        "Population and society"
                    },
                    Keywords = new List<string>
                    {
                        "Air travel",
                        "Passport",
                        "Airports",
                        "leaving UK",
                        "entering UK"
                    },
                    DataAssetContacts = new List<CddoDataAssetContact>
                    {
                        new CddoDataAssetContact
                        {
                            Name = "Rob Nichols",
                            Email = "robert.nichols@digital.cabinet-office.gov.uk",
                            Role = DataAssetContactRoleType.Contact
                        }
                    },
                    Publisher = "academy-for-social-justice",
                    SecurityClassification = "OFFICIAL",
                    AccessRights = "RESTRICTED",
                    EndpointDescription = "http://example.com/path/to/swagger",
                    EndpointUrl = "http://example.com/api/v1",
                    RelatedResources = new List<string>
                    {
                        "8d085327-21b6-4d8b-9705-88faad231d23"
                    }
                };
            }
            else
            {
                result.CddoDataAsset =
                new CddoDataAsset
                {
                    AllowDSRRequest = true,
                    RequiresDSR = true,
                    Id = Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d22"),
                    InternalIdentifier = "acme-123",
                    Modified = DateTime.Parse("2023-12-09T16:09:53+00:00"),
                    DataAssetStatus = DataAssetStatus.Published,

                    Title = "Advance Passenger Information II",
                    Description = "Travel data and personal data given to airlines by passenger. API covers both inbound and outbound air passengers. API includes the passenger’s full name, nationality, date of birth, gender and travel document number, type and country of issue. The data does not include those arriving by sea or rail routes, by private aircraft or via the Common Travel Area (CTA).",
                    DataAssetType = DataAssetType.DataSet,
                    Themes = new List<string>
                {
                "Transport and infrastructure",
                "Population and society"
                },
                    Keywords = new List<string>
                {
                "Air travel",
                "Passport",
                "Airports",
                "leaving UK",
                "entering UK"
                },
                    DataAssetContacts = new List<CddoDataAssetContact>
                {
                new CddoDataAssetContact
                {
                    Name = "Rob Nichols",
                    Email = "robert.nichols@digital.cabinet-office.gov.uk",
                    Role = DataAssetContactRoleType.Contact
                }
                },
                    License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                    {
                        Title = "Creative Commons",
                        LicenseUrl = "https://creativecommons.org/licenses/by/4.0"
                    },
                    Publisher = "academy-for-social-justice",
                    SecurityClassification = "OFFICIAL",
                    AccessRights = "RESTRICTED",
                    DataAssetDistribution = new List<CddoDataAssetDistribution>
                {
                new CddoDataAssetDistribution
                {
                    Title = "CSV download",
                    Description = "Complete dataset provided as a downloadable file",
                    AccessService = "8d085327-21b6-4d8b-9705-88faad231d23",
                    AccessUrl = "http://example.com/path/to/file.csv",
                    Format = "CSV file",
                    MediaType =  "text/csv"
                },
                new CddoDataAssetDistribution
                {
                    Title = "Rest API",
                    AccessUrl = "http://example.com/api/",
                    Description = "A fully queryable REST API with JSON and XML output",
                    Format = "API"
                },
                new CddoDataAssetDistribution
                {
                    Title = "Web Page",
                    Description = "A web page that provides the data, links to downloads, or documentation.",
                    AccessUrl = "http://example.com/path/to/page",
                    Format = "Web page",
                    MediaType = "text/html"
                }
                }
                };
            }
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