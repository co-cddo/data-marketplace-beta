using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.DataShareRequests;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.UI.Services;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using ICatalogDataService = Cddo.Data.Marketplace.UI.Services.Interfaces.ICatalogDataService;
using Cddo.Data.Marketplace.UI.Builders;
using Cddo.Data.Marketplace.Audit;
using System.Globalization;

namespace Cddo.Data.Marketplace.UI.Controllers;
[Authorize]
public class ReportsController : Controller
{
    private readonly ILogger<ReportsController> _logger;
    private readonly ICatalogReportsService _catalogReportsService;
    private readonly IUserRoleService _userRoleService;
    private readonly ICatalogDataService _catalogDataService;
    private readonly string dateFormat = "yyyyMMdd_HHmmss";
    private readonly string csvString = "text/csv";
    private static readonly string AccessDeniedPage = "/Error/403";
    public ReportsController(ILogger<ReportsController> logger,
        ICatalogReportsService catalogReportsService, IUserRoleService userRoleService, ICatalogDataService catalogDataService)
    {
        _logger = logger;
        _catalogReportsService = catalogReportsService;
        _userRoleService = userRoleService;
        _catalogDataService = catalogDataService;
    }

    [Route("MetadataReport")]
    public async Task<IActionResult> GotoMetadataReport(Guid? templateId, bool isDownload = false)
    {
        if (templateId.HasValue && templateId.ToString() == "86ef337b-1432-49ae-9d96-adcfa87553c2")
        {
            return RedirectToAction("GetMetadataStatsReport", new { templateId });
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isOrganisationAdmin = await _userRoleService.IsUserRoleAdmin();
            var filterByInitiatingUserPermissions = !isSystemAdmin;

            if (isSystemAdmin || isOrganisationAdmin)
            {
               
                var templateDetails = await ProcessMetadaReportRequest(templateId, filterByInitiatingUserPermissions);

                var result = await _catalogReportsService.GetCatalogReportsDataAsync(templateDetails.Item1);
                var orderedDataItems = new List<CatalogReportsDataItem>();
                foreach (var item in result.CatalogReportsDataItems)
                {
                    var orderedFields = item.CatalogReportsDataItemFields.OrderBy(x => x.Field).ToList();
                    item.CatalogReportsDataItemFields = orderedFields;
                    orderedDataItems.Add(item);
                }
                result.CatalogReportsDataItems = orderedDataItems;
                result.ReportName = templateDetails.Item2.ReportName;
                result.TemplateId = templateDetails.Item2.TemplateId;
                result.SelectedFields = templateDetails.Item1.RequiredFields;


                var allOrganisations = (await _catalogDataService.GetCddoOrganisationsAsync()).OrderBy(x => x).ToList();
                if (templateDetails.Item2.ReportName.Contains("all"))
                {
                    result.SelectableOrganisations = allOrganisations;
                }
                else
                {
                    var selectedOrg = templateDetails.Item1.Filter.FieldFilters.Where(x=>x.Field == CatalogAssetField.Publisher).FirstOrDefault();
                    var orgs = allOrganisations!.Where(x => x == selectedOrg.Values.FirstOrDefault()).ToList();
                    result.SelectableOrganisations = orgs.Any()
                                                     ? new List<string>() { orgs[0].ToString() } : new List<string>() {_catalogReportsService.PrettifyString(selectedOrg.Values[0]) };
                }
                

                ViewBag.PageNumber = 1;
                ViewBag.PageSize = 10;
                ViewBag.TemplateId = result.TemplateId;
                ViewBag.TotalRecords = result.TotalNumberOfMatchedRecords;

                return View("~/Pages/Reports/MetadataReport.cshtml", result);
            }
        }

        return RedirectToPage(AccessDeniedPage);
    }

    [Route("DownloadMetadataReport")]
    public async Task<IActionResult> DownloadMetadataReport(Guid? templateId)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for DownloadMetadataReport.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isOrganisationAdmin = await _userRoleService.IsUserRoleAdmin();
            var filterByInitiatingUserPermissions = !isSystemAdmin;

            if (isSystemAdmin || isOrganisationAdmin)
            {
                var templateDetails = await ProcessMetadaReportRequest(templateId, filterByInitiatingUserPermissions);
                templateDetails.Item1.NumberOfRecords = 1000;
                var result = await _catalogReportsService.DownloadCatalogReportsDataAsync(templateDetails.Item1);

                var csvContent = GenerateMetadataCsvContent(result);
                var bytes = Encoding.UTF8.GetBytes(csvContent);
                var dateTimeNow = DateTime.Now.ToString(dateFormat);
                var fileName = $"MetadataReport_{templateDetails.Item2.ReportName}_{dateTimeNow}.csv";
                return File(bytes, csvString, fileName);
            }
        }

        return RedirectToPage(AccessDeniedPage);
    }

    [HttpPost(Name = "MetadataReports")]
    public async Task<IActionResult> GetCatalogMetadataReportsData(QueryCatalogReportsDataRequestFilter requestFilter, Guid? templateId, bool isDownload = false,  CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for GetCatalogMetadataReportsData.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isOrganisationAdmin = await _userRoleService.IsUserRoleAdmin();
            var filterByInitiatingUserPermissions = !isSystemAdmin;

            if (isSystemAdmin || isOrganisationAdmin)
            {
                var queryCatalogReportsDataRequest = new QueryCatalogReportsDataRequest()
                {
                    StartRecordIndex = !isDownload ? ((requestFilter.PageNumber -1) * requestFilter.PageSize) : 0,
                    NumberOfRecords = !isDownload ? requestFilter.PageSize: 10000,
                    RequiredFields = requestFilter.RequiredFields,
                    SearchText = requestFilter.SearchTerm,
                    Filter = new CatalogReportsFilter() { FilterByInitiatingUserPermissions = filterByInitiatingUserPermissions, FieldFilters = CreateFieldFilter(requestFilter) }
                };

                var selectedOrgs = JsonConvert.DeserializeObject<List<string>>(requestFilter.SelectableOrganisations);
                if(selectedOrgs.Count() == 1)
                {
                    queryCatalogReportsDataRequest.Filter.FieldFilters.Add(new CatalogReportFieldFilter() { Field = CatalogAssetField.Publisher, Values = selectedOrgs });
                }
                
                var result = await _catalogReportsService.GetCatalogReportsDataAsync(queryCatalogReportsDataRequest, cancellationToken);
                result.SelectedFields = requestFilter.RequiredFields;
                result.Organisations = requestFilter.Organisations;
                result.SelectedStatuses = requestFilter.DataAssetStatus;
                result.ReportName = "Custom Report";
                if (isDownload)
                {
                    var csvContent = GenerateMetadataCsvContent(result);
                    var bytes = Encoding.UTF8.GetBytes(csvContent);
                    var dateTimeNow = DateTime.Now.ToString(dateFormat);
                    var fileName = $"MetadataReport_{result.ReportName}_{dateTimeNow}.csv";
                    return File(bytes, csvString, fileName);
                }

                ViewBag.PageNumber = requestFilter.PageNumber;
                ViewBag.PageSize = requestFilter.PageSize;
                ViewBag.TotalRecords = result.TotalNumberOfMatchedRecords;
                ViewBag.TemplateId = templateId;

                result.SelectableOrganisations = JsonConvert.DeserializeObject<List<string>>(requestFilter.SelectableOrganisations);
                return View("~/Pages/Reports/MetadataReport.cshtml", result);
            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [Route("metadataStatsReport")]
    public async Task<IActionResult> GetMetadataStatsReport(Guid templateId)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for GetMetadataStatsReport.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isOrganisationAdmin = await _userRoleService.IsUserRoleAdmin();
            var filterByInitiatingUserPermissions = !isSystemAdmin;

            if (isSystemAdmin || isOrganisationAdmin)
            {
                var templateDetails = await ProcessMetadaReportRequest(templateId, filterByInitiatingUserPermissions);

                templateDetails.Item1.NumberOfRecords = 1;

                //MetadataReportsStats
                var result = new MetadataReportsStats() {
                    TemplateId = templateId
                };
                
                result.TotalDraft = await SetTotalRecords(templateDetails.Item1, "Draft");
                result.TotalPublished = await SetTotalRecords(templateDetails.Item1, "Published");  //.Where(c=>c.CatalogReportsDataItemFields.Where(f=>f.Field == CatalogAssetField.DataAssetStatus && f.Values.FirstOrDefault() == "Draft")).Count();
                result.TotalArchived = await SetTotalRecords(templateDetails.Item1, "Archived"); //.Where(c=>c.CatalogReportsDataItemFields.Where(f=>f.Field == CatalogAssetField.DataAssetStatus && f.Values.FirstOrDefault() == "Draft")).Count();
                result.Totaldeleted = await SetTotalRecords(templateDetails.Item1, "Deleted"); //.Where(c=>c.CatalogReportsDataItemFields.Where(f=>f.Field == CatalogAssetField.DataAssetStatus && f.Values.FirstOrDefault() == "Draft")).Count();
                result.TotalEdited = await SetModifiedTotal(templateDetails.Item1);

                return View("~/Pages/Reports/MetadataReportStats.cshtml", result);
            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [HttpPost("MetadataStats")]
    public async Task<IActionResult> MetadataStatsPost(Guid templateId, string OrganisationName, string DataAssetType, bool downloadCsv)
    {
        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isOrganisationAdmin = await _userRoleService.IsUserRoleAdmin();
            var filterByInitiatingUserPermissions = !isSystemAdmin;

            if (isSystemAdmin || isOrganisationAdmin)
            {
                var templateDetails = await ProcessMetadaStatsReportRequest(templateId, filterByInitiatingUserPermissions);

                templateDetails.Item1.NumberOfRecords = 1;

                //MetadataReportsStats
                var result = new MetadataReportsStats()
                {
                    TemplateId = templateId
                };

                result.TotalDraft = await SetTotalRecords(templateDetails.Item1, "Draft", DataAssetType, OrganisationName);
                result.TotalPublished = await SetTotalRecords(templateDetails.Item1, "Published", DataAssetType, OrganisationName);  //.Where(c=>c.CatalogReportsDataItemFields.Where(f=>f.Field == CatalogAssetField.DataAssetStatus && f.Values.FirstOrDefault() == "Draft")).Count();
                result.TotalArchived = await SetTotalRecords(templateDetails.Item1, "Archived", DataAssetType, OrganisationName); //.Where(c=>c.CatalogReportsDataItemFields.Where(f=>f.Field == CatalogAssetField.DataAssetStatus && f.Values.FirstOrDefault() == "Draft")).Count();
                result.Totaldeleted = await SetTotalRecords(templateDetails.Item1, "Deleted", DataAssetType, OrganisationName); //.Where(c=>c.CatalogReportsDataItemFields.Where(f=>f.Field == CatalogAssetField.DataAssetStatus && f.Values.FirstOrDefault() == "Draft")).Count();

                //Set the Modified count
                result.TotalEdited = await SetModifiedTotal(templateDetails.Item1, DataAssetType, OrganisationName);

                ViewBag.OrganisationName = OrganisationName;
                ViewBag.DataAssetType = DataAssetType;
                if(downloadCsv)
                {
                    var csvContent = GenerateMetadataReportCsvContent(result.TotalDraft, result.TotalPublished, result.TotalEdited, result.TotalArchived, result.Totaldeleted);
                    var bytes = Encoding.UTF8.GetBytes(csvContent);
                    var dateTimeNow = DateTime.Now.ToString(dateFormat);
                    string orgName = ViewBag.OrganisationName;
                    string modifiedOrgName = orgName == null ? "" : orgName.Replace(' ', '_') + "_";
                    string assetType = DataAssetType == null ? "" : DataAssetType + "_";
                    var fileName = $"{modifiedOrgName}{assetType}MetadataStatsReport_{dateTimeNow}.csv";
                    return File(bytes, csvString, fileName);
                }
                return View("~/Pages/Reports/MetadataReportStats.cshtml", result);
            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

  
    [Route("GetTelemetryReport")]
    public async Task<IActionResult> GetTelemetryReportsData(Guid templateId, string timeRange, string searchQuery = "", int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for GetTelemetryReportsData.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isAdmin = await _userRoleService.IsUserRoleAdmin();
            var reportName = "Custom Report";
            var rawQuery = searchQuery;
            if (isSystemAdmin || isAdmin)
            {
                if (string.IsNullOrEmpty(timeRange))
                {
                    timeRange = "1.00:00:00";
                }
                try
                {
                    var request = new LogsQueryDataRequest() 
                    { 
                        TemplateId = templateId,
                        TimeRange = timeRange,
                        SearchQuery = rawQuery,
                        ReportName = reportName,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };
                    var result = await ProcessTelemetryReportRequest(request, cancellationToken: cancellationToken);

                    return View("~/Pages/Reports/TelemetryReport.cshtml", result);
                }
                catch (Exception ex)
                {
                    ViewBag.SearchQuery = rawQuery;
                    ViewBag.TimeRange = timeRange;
                    return View("~/Pages/Reports/TelemetryReport.cshtml", new PaginatedLogsDataResult(pageNumber, pageSize, 0)
                    {
                        ReportName = reportName,
                        TemplateId = templateId,
                    });
                }

            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [HttpGet(Name = "CreateReportDetails")]
    public async Task<IActionResult> CreateReportDetails()
    {
        return View("~/Pages/Reports/CreateReport.cshtml");

    }
    [HttpPost(Name = "CreateReportDetailsTemplate")]
    public async Task<IActionResult> CreateReportDetailsTemplate(ReportTemplate reportDetails, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for CreateReportDetailsTemplate.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            var userProfile = await _userRoleService.GetUserProfileAsync();
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isAdmin = await _userRoleService.IsUserRoleAdmin();
            if (userProfile != null && (isSystemAdmin || isAdmin))
            {
                var reportTemplate = new ReportTemplate()
                {
                    KqlQuery = reportDetails.KqlQuery,
                    ReportName = reportDetails.ReportName,
                    TemplateId = Guid.NewGuid(),
                    CreatedOn = DateTime.Now,
                    Description = reportDetails.Description,
                    ReportType = reportDetails.ReportType,
                    OrganisationId = userProfile.Organisation.OrganisationId,
                    UserId = userProfile.User.UserId
                };

                await CreateReport(reportTemplate);

                switch (reportTemplate.ReportType)
                {
                    case ReportType.Telemetry:
                        return RedirectToPage("/Reports/TelemetryReports");
                    case ReportType.Metadata:
                        return RedirectToPage("/Reports/MetadataReports");
                    case ReportType.Users:
                        return RedirectToPage("/Reports/UsersReports"); ;
                    case ReportType.Datasharerequests:
                        return RedirectToPage("/Reports/DatashareReports");
                    case ReportType.None:
                    default:
                        return RedirectToPage("/Reports/ReportsList"); ;
                }
            }
        }

        return RedirectToPage(AccessDeniedPage);
    }
    [HttpPost(Name = "QueryTelemetryReport")]
    public async Task<IActionResult> QueryTelemetryData(LogsQueryDataRequest request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for QueryTelemetryData.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isAdmin = await _userRoleService.IsUserRoleAdmin();
            if (isSystemAdmin || isAdmin)
            {
                try
                {
                    var result = await ProcessTelemetryReportRequest(request, default);
                    return View("~/Pages/Reports/TelemetryReport.cshtml", result);
                }
                catch (Exception)
                {

                    ViewBag.SearchQuery = request.SearchQuery;
                    ViewBag.TimeRange = request.TimeRange;
                    return View("~/Pages/Reports/TelemetryReport.cshtml", new PaginatedLogsDataResult(request.PageNumber, request.PageSize, 0)
                    {
                        ReportName = request.ReportName,
                        TemplateId = request.TemplateId,
                    });
                }
                
            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [Route("DownloadTelemetryReport")]
    public async Task<IActionResult> DownloadTelemetryData(LogsQueryDataRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for DownloadTelemetryData.");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isAdmin = await _userRoleService.IsUserRoleAdmin();
            if (isSystemAdmin || isAdmin)
            {
                try
                {
                    var result = await ProcessTelemetryReportRequest(request, isDownLoad: true);

                    if (result != null)
                    {
                        var csvContent = GenerateTelemetryCsvContent(result!.Results);
                        var bytes = Encoding.UTF8.GetBytes(csvContent);
                        var dateTimeNow = DateTime.Now.ToString(dateFormat);
                        var fileName = $"TelemtryReport_{result.ReportName}_{dateTimeNow}.csv";
                        return File(bytes, csvString, fileName);
                    }
                }
                catch (Exception ex)
                {

                    _logger.LogError($"Error downloading query report :{ex.Message}");
                }
                
            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

    [Route("GetDSRReport")]
    public async Task<IActionResult> GetDSRReportData(DataShareRequestCountQuery dataShareRequestCountQuery, string? startDate, string? endDate, bool downloadCsv = false, bool isSystemAdministrator = false, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            if (validationErrors.Count() > 0)
            {
                _logger.LogError("GetDSRReportData validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
            }
        }


        if (User.Identity!.IsAuthenticated)
        {
            bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            bool isAdministrator = await _userRoleService.IsUserRoleAdmin();
            bool isSupplier = await _userRoleService.IsUserRoleSupplier();

            if (isSystemAdmin || isAdministrator || isSupplier)
            {
                ParseAndSetDates(dataShareRequestCountQuery, startDate, endDate, CultureInfo.InvariantCulture);

                if (!isSystemAdmin)
                {
                    var userProfile = await _userRoleService.GetUserProfileAsync();
                    dataShareRequestCountQuery.PublisherOrganisationId = userProfile.Organisation?.OrganisationId;
                }

                var commonQueryProps = new DataShareRequestCountQuery
                {
                    From = dataShareRequestCountQuery.From,
                    To = dataShareRequestCountQuery.To,
                    MinimumDuration = dataShareRequestCountQuery.MinimumDuration,
                    MaximumDuration = dataShareRequestCountQuery.MaximumDuration,
                    PublisherOrganisationId = dataShareRequestCountQuery.PublisherOrganisationId,
                    PublisherDomainId = dataShareRequestCountQuery.PublisherDomainId,
                    UseOnlyTheMostRecentPeriodSpentInIntermediateStatuses = dataShareRequestCountQuery.UseOnlyTheMostRecentPeriodSpentInIntermediateStatuses
                };

                var queries = CreateQueries(commonQueryProps);

                var queryDataShareRequestsCountsRequest = new QueryDataShareRequestsCountsRequest
                {
                    DataShareRequestCountQueries = queries
                };

                var result = await _catalogReportsService.GetDSRReportDataAsync(queryDataShareRequestsCountsRequest, cancellationToken);

                var counts = ExtractCounts(result?.DataShareRequestCounts);

                var selectedOption = result?.DataShareRequestCounts.FirstOrDefault()?.DataShareRequestCountQuery;

                if (downloadCsv)
                {
                    var csvContent = GenerateCsvContent(counts, isSystemAdministrator);
                    var bytes = Encoding.UTF8.GetBytes(csvContent);
                    var dateTimeNow = DateTime.Now.ToString(dateFormat);
                    var fileName = $"DSRReport_{dateTimeNow}.csv";
                    return File(bytes, csvString, fileName);
                }
                else
                {
                    var viewModel = new DataShareRequestCountsViewModel
                    {
                        DraftCurrentCount = counts.DraftCurrentCount,
                        SubmittedCurrentCount = counts.SubmittedCurrentCount,
                        AcceptedCurrentCount = counts.AcceptedCurrentCount,
                        RejectedCurrentCount = counts.RejectedCurrentCount,
                        CancelledCurrentCount = counts.CancelledCurrentCount,
                        ReturnedCurrentCount = counts.ReturnedCurrentCount,
                        DraftIntermediateCount = counts.DraftIntermediateCount,
                        SubmittedIntermediateCount = counts.SubmittedIntermediateCount,
                        AcceptedIntermediateCount = counts.AcceptedIntermediateCount,
                        RejectedIntermediateCount = counts.RejectedIntermediateCount,
                        CancelledIntermediateCount = counts.CancelledIntermediateCount,
                        ReturnedIntermediateCount = counts.ReturnedIntermediateCount,
                        DataShareRequestCountQuery = selectedOption
                    };
                    return View("~/Pages/Reports/DataShareReport.cshtml", viewModel);
                }
            }
        }
        return RedirectToPage(AccessDeniedPage);
    }

    public RedirectToActionResult ClearViewBagAndRefresh()
    {
        ViewData.Clear(); // Clear the ViewBag
        return RedirectToAction(nameof(GetTelemetryReportsData), new { templateId = new Guid("6806d74c-a510-4add-b8ce-ee808dd6efc7") }); // Refresh the page
    }

    #region Privates 
    private async Task<int> SetModifiedTotal(QueryCatalogReportsDataRequest item1, string assetType = "", string publisher = "")
    {

        item1.RequiredFields = new List<CatalogAssetField>() { CatalogAssetField.Modified };

        item1.Filter = new CatalogReportsFilter()
        {
            FieldFilters = new List<CatalogReportFieldFilter>()
            {
            }
        };

        if (!string.IsNullOrEmpty(assetType))
        {
            item1.Filter.FieldFilters.Add(new CatalogReportFieldFilter()
            {
                Field = CatalogAssetField.DataAssetType,
                Values = new List<string>() { assetType }
            });
        }


        if (!string.IsNullOrEmpty(publisher))
        {
            item1.Filter.FieldFilters.Add(new CatalogReportFieldFilter()
            {
                Field = CatalogAssetField.Publisher,
                Values = new List<string>() { publisher }
            });
        }
        item1.NumberOfRecords = 10000; //TODO change hardcoded value by fixing 0 check in the API        

        var response = await _catalogReportsService.DownloadCatalogReportsDataAsync(item1);

        if (response == null) return 0;

        return response.CatalogReportsDataItems.Count();

    }

    private async Task<int> SetTotalRecords(QueryCatalogReportsDataRequest item1, string status, string assetType = "", string publisher = "")
    {
        item1.Filter = new CatalogReportsFilter()
        {
            FieldFilters = new List<CatalogReportFieldFilter>()
            {
                new CatalogReportFieldFilter()
                {
                    Field = CatalogAssetField.DataAssetStatus, Values = new List<string>() { status}
                }
            }
        };

        if (!string.IsNullOrEmpty(publisher))
        {
            item1.Filter.FieldFilters.Add(new CatalogReportFieldFilter()
            {
                Field = CatalogAssetField.Publisher,
                Values = new List<string>() { publisher }
            });
        }

        if (!string.IsNullOrEmpty(assetType))
        {
            item1.Filter.FieldFilters.Add(new CatalogReportFieldFilter()
            {
                Field = CatalogAssetField.DataAssetType,
                Values = new List<string>() { assetType }
            });
        }

        item1.NumberOfRecords = 10000; //TODO change hardcoded value by fixing 0 check in the API        

        var response = await _catalogReportsService.DownloadCatalogReportsDataAsync(item1);

        if (response == null) return 0;

        return response.CatalogReportsDataItems.Count();
    }
    private List<CatalogReportFieldFilter> CreateFieldFilter(QueryCatalogReportsDataRequestFilter requestFilter)
    {
        var response = new List<CatalogReportFieldFilter>();

        if (requestFilter.Organisations != null && requestFilter.Organisations.Any())
        {
            response.Add(new CatalogReportFieldFilter() { Field = CatalogAssetField.Publisher, Values = requestFilter.Organisations });
        }
        if (requestFilter.DataAssetStatus != null && requestFilter.DataAssetStatus.Any())
        {
            response.Add(new CatalogReportFieldFilter() { Field = CatalogAssetField.DataAssetStatus, Values = JoinSelectedEnumValues(requestFilter.DataAssetStatus) });
        }
        /// TODO: Ask Adam about the date range filter
        /// 
        return response;
    }

    public static List<string> JoinSelectedEnumValues(List<DataAssetStatus> selectedEnums)
    {
        return selectedEnums.Select(v=>v.ToString()).ToList();
    }
    private static void ParseAndSetDates(DataShareRequestCountQuery query, string? startDate, string? endDate, IFormatProvider formatProvider)
    {
        string[] dateFormats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy" };

        if (DateTime.TryParseExact(startDate, dateFormats, formatProvider, DateTimeStyles.None, out DateTime tempStartDate))
        {
            query.From = tempStartDate;
        }
        if (DateTime.TryParseExact(endDate, dateFormats, formatProvider, DateTimeStyles.None, out DateTime tempEndDate))
        {
            query.To = tempEndDate;
        }
    }

    private List<DataShareRequestCountQuery> CreateQueries(DataShareRequestCountQuery commonQueryProps)
    {
        return
    [
        CreateQuery(1, [DataShareRequestStatus.Draft], [], commonQueryProps),
        CreateQuery(2, [DataShareRequestStatus.Submitted, DataShareRequestStatus.InReview], [], commonQueryProps),
        CreateQuery(3, [DataShareRequestStatus.Accepted], [], commonQueryProps),
        CreateQuery(4, [DataShareRequestStatus.Rejected], [], commonQueryProps),
        CreateQuery(5, [DataShareRequestStatus.Cancelled], [], commonQueryProps),
        CreateQuery(6, [DataShareRequestStatus.Returned], [], commonQueryProps),
        CreateQuery(7, [], [DataShareRequestStatus.Draft], commonQueryProps),
        CreateQuery(8, [], [DataShareRequestStatus.Submitted, DataShareRequestStatus.InReview], commonQueryProps),
        CreateQuery(9, [], [DataShareRequestStatus.Accepted], commonQueryProps),
        CreateQuery(10, [], [DataShareRequestStatus.Rejected], commonQueryProps),
        CreateQuery(11, [], [DataShareRequestStatus.Cancelled], commonQueryProps),
        CreateQuery(12, [], [DataShareRequestStatus.Returned], commonQueryProps)
    ];
    }

    private static (int DraftCurrentCount, int SubmittedCurrentCount, int AcceptedCurrentCount, int RejectedCurrentCount, int CancelledCurrentCount, int ReturnedCurrentCount,
            int DraftIntermediateCount, int SubmittedIntermediateCount, int AcceptedIntermediateCount, int RejectedIntermediateCount, int CancelledIntermediateCount, int ReturnedIntermediateCount)
        ExtractCounts(IEnumerable<DataShareRequestCount> dataShareRequestCounts)
    {
        return (
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Draft, true),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Submitted, true),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Accepted, true),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Rejected, true),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Cancelled, true),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Returned, true),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Draft, false),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Submitted, false),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Accepted, false),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Rejected, false),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Cancelled, false),
            GetCountForStatus(dataShareRequestCounts, DataShareRequestStatus.Returned, false)
        );
    }

    private static int GetCountForStatus(IEnumerable<DataShareRequestCount> dataShareRequestCounts, DataShareRequestStatus status, bool isCurrent)
    {
        return dataShareRequestCounts
            .FirstOrDefault(r => (isCurrent ? r.DataShareRequestCountQuery.CurrentStatuses : r.DataShareRequestCountQuery.IntermediateStatuses).Contains(status))
            ?.NumberOfDataShareRequests ?? 0;
    }

    private DataShareRequestCountQuery CreateQuery(int id, DataShareRequestStatus[] currentStatuses, DataShareRequestStatus[] intermediateStatuses, DataShareRequestCountQuery baseQuery)
    {
        return new DataShareRequestCountQuery
        {
            Id = id,
            CurrentStatuses = currentStatuses.ToList(),
            IntermediateStatuses = intermediateStatuses.ToList(),
            From = baseQuery.From,
            To = baseQuery.To,
            MinimumDuration = baseQuery.MinimumDuration,
            MaximumDuration = baseQuery.MaximumDuration,
            PublisherOrganisationId = baseQuery.PublisherOrganisationId,
            PublisherDomainId = baseQuery.PublisherDomainId,
            UseOnlyTheMostRecentPeriodSpentInIntermediateStatuses = baseQuery.UseOnlyTheMostRecentPeriodSpentInIntermediateStatuses
        };
    }

    private static string GenerateCsvContent(
    (int DraftCurrentCount, int SubmittedCurrentCount, int AcceptedCurrentCount, int RejectedCurrentCount, int CancelledCurrentCount, int ReturnedCurrentCount,
    int DraftIntermediateCount, int SubmittedIntermediateCount, int AcceptedIntermediateCount, int RejectedIntermediateCount, int CancelledIntermediateCount, int ReturnedIntermediateCount) counts,
    bool isSystemAdministrator)
    {
        var sb = new StringBuilder();

        var statuses = new Dictionary<string, (int Current, int Intermediate)>
    {
        {"Submitted", (counts.SubmittedCurrentCount, counts.SubmittedIntermediateCount)},
        {"Accepted", (counts.AcceptedCurrentCount, counts.AcceptedIntermediateCount)},
        {"Rejected", (counts.RejectedCurrentCount, counts.RejectedIntermediateCount)},
        {"Cancelled", (counts.CancelledCurrentCount, counts.CancelledIntermediateCount)},
        {"Returned", (counts.ReturnedCurrentCount, counts.ReturnedIntermediateCount)}
    };

        if (isSystemAdministrator)
        {
            statuses.Add("Draft", (counts.DraftCurrentCount, counts.DraftIntermediateCount));

            sb.AppendLine("Status,Current Count,Intermediate Count");

            foreach (var status in statuses)
            {
                sb.AppendLine($"{status.Key},{status.Value.Current},{status.Value.Intermediate}");
            }
        }
        else
        {
            sb.AppendLine("Status,Current Count");

            foreach (var status in statuses)
            {
                sb.AppendLine($"{status.Key},{status.Value.Current}");
            }
        }

        return sb.ToString();
    }

    private static string GenerateMetadataReportCsvContent(
    int DraftCurrentCount, int PublishedCurrentCount, int EditedCurrentCount, int ArchivedCurrentCount, int DeletedCurrentCount)
    {
        var sb = new StringBuilder();

        var statuses = new Dictionary<string, int>
        {
            {"Draft", DraftCurrentCount},
            {"Published", PublishedCurrentCount},
            {"Edited", EditedCurrentCount},
            {"Archived", ArchivedCurrentCount},
            {"Deleted", DeletedCurrentCount}
        };
       
        sb.AppendLine("Status,Current Count");

        foreach (var status in statuses)
        {
            sb.AppendLine($"{status.Key},{status.Value}");
        }
        

        return sb.ToString();
    }

    private static string GenerateTelemetryCsvContent(TelemetryQueryResultsData results)
    {
        var sb = new StringBuilder();
        foreach (var column in results.ColumnData.Columns)
        {
            sb.Append(column.Name);
            if (column != results.ColumnData.Columns.Last())
            {
                sb.Append(", ");
            }

        }
        sb.Append("\n");
        foreach (var result in results.RowData.Rows)
        {
            sb.AppendLine(string.Join(", ", result.RowValues.Select(field => field.Value)));
        }

        return sb.ToString();
    }    
    private static string GenerateMetadataCsvContent(QueryCatalogReportsDataResponse results)
    {
        var sb = new StringBuilder();
        if (results.CatalogReportsDataItems.Count() == 0) return "";
        foreach (var column in results.CatalogReportsDataItems.First().CatalogReportsDataItemFields)
        {
            sb.Append(column.Field.ToString());
            if (column != results.CatalogReportsDataItems.First().CatalogReportsDataItemFields.Last())
            {
                sb.Append(", ");
            }

        }
        sb.Append("\n");
        foreach (var result in results.CatalogReportsDataItems)
        {
            foreach(var field in result.CatalogReportsDataItemFields)
            {
                sb.Append(EscapeCsvField(field.Values));
                if (field != result.CatalogReportsDataItemFields.Last())
                {
                    sb.Append(", ");
                }
            }          

            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string EscapeCsvField(List<string>? values)
    {
        if (values == null || values.Count == 0)
        {
            return string.Empty;
        }

        // Escape each value and join them with commas
        var escapedValues = values.Select(value => ConvertToSingleLine(value)).ToList();
        return string.Join(",", escapedValues);
    }

    private static string ConvertToSingleLine(string input)
    {
        // Split the input string by whitespace characters (spaces, newlines, tabs, etc.)
        string[] words = input.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Join the words back together with a single space
        var result = string.Join(" ", words);
        if (result.Contains(","))
        {
            if (result.Contains("\""))
            {
                result = result.Replace("\"", "\"\"");
            }
            result = $"\"{result}\"";
        }
        return result.Replace("\\", " ");
    }

    private async Task<(QueryCatalogReportsDataRequest?, ReportTemplate?)> ProcessMetadaReportRequest(Guid? templateId, bool filterByInitiatingUserPermissions)
    {
        var response = new QueryCatalogReportsDataRequest();
        ReportTemplate? template = null;
        if (templateId != Guid.Empty && templateId != null)
        {
            var jsonHandler = new ReadWriteJson<ReportTemplate>($"{templateId}.json");
            string _directoryPath = "Pages/Reports/ReportTemplates/Metadata/";
            template = await jsonHandler.ReadJsonAsync($"{_directoryPath}{templateId}.json");

            response = JsonConvert.DeserializeObject<QueryCatalogReportsDataRequest>(template.KqlQuery);
        }

        response.Filter.FilterByInitiatingUserPermissions = filterByInitiatingUserPermissions;
        return (response, template);
    }
    private async Task<(QueryCatalogReportsDataRequest?, ReportTemplate?)> ProcessMetadaStatsReportRequest(Guid? templateId, bool filterByInitiatingUserPermissions)
    {
        var response = new QueryCatalogReportsDataRequest();
        ReportTemplate? template = null;
        if (templateId != Guid.Empty && templateId != null)
        {
            var jsonHandler = new ReadWriteJson<ReportTemplate>($"{templateId}.json");
            string _directoryPath = "Pages/Reports/ReportTemplates/Metadata/";
            template = await jsonHandler.ReadJsonAsync($"{_directoryPath}{templateId}.json");

            response = JsonConvert.DeserializeObject<QueryCatalogReportsDataRequest>(template.KqlQuery);
        }

        response.Filter.FilterByInitiatingUserPermissions = filterByInitiatingUserPermissions;
        return (response, template);
    }
    private async Task<PaginatedLogsDataResult> ProcessTelemetryReportRequest(LogsQueryDataRequest request, bool isDownLoad = false, CancellationToken cancellationToken = default)
    {
        var rawQuery = request.SearchQuery;
        if (request.TemplateId != Guid.Empty && request.TemplateId != null)
        {
            var templateStore = new ReportTemplates();

            var jsonHandler = new ReadWriteJson<ReportTemplate>($"{request.TemplateId}.json");
            string _directoryPath = "Pages/Reports/ReportTemplates/Telemetry/";
            var template = await jsonHandler.ReadJsonAsync($"{_directoryPath}{request.TemplateId}.json");

            if (template != null && string.IsNullOrEmpty(template.KqlQuery))
            {
                template.KqlQuery = "AppEvents | where Name == 'UserLogin'";
            }
            rawQuery = template?.KqlQuery;

            if(request.TemplateId.ToString() == "6806d74c-a510-4add-b8ce-ee808dd6efc7")
            {
                var splitQuery = KqlQuerySplitter.SplitKqlQuery(rawQuery);
                if (!string.IsNullOrEmpty(request.DataAssetAction))
                {
                    var whereClauses = splitQuery["WhereCondition"] as List<string>;
                    whereClauses.Add($" ActionSource == '{request.DataAssetAction}'");

                    splitQuery["WhereCondition"] = whereClauses;
                }

                if (request.Propertype != null)
                {
                    var whereClauses = splitQuery["WhereCondition"] as List<string>;
                    whereClauses.Add($" ErrorType == '{request.Propertype.ToString()}'");

                    splitQuery["WhereCondition"] = whereClauses;
                }
                if (request.DataAssertField != null)
                {
                    var whereClauses = splitQuery["WhereCondition"] as List<string>;
                    whereClauses.Add($" FieldName == '{request.DataAssertField.ToString()}'");

                    splitQuery["WhereCondition"] = whereClauses;
                }

                rawQuery = KqlQuerySplitter.RebuildKqlQuery(splitQuery);
            }
            request.ReportName = template?.ReportName;
        }
        if (!isDownLoad)
        {
            var selectedColumnNames = KqlQueryBuilderHelper.ExtractExtendValues(rawQuery);

            request.SearchQuery = ReportsTelemetryPagination.GetPaginationTemplate(rawQuery, request.PageNumber, request.PageSize, selectedColumnNames);
        }
        else
        {
            request.SearchQuery = rawQuery;
        }
        if(request.ReportName.IsNullOrEmpty() || request.ReportName == "")
        {
            request.ReportName = "Custom Report";
        }

        var result = await _catalogReportsService.GetTelemetryReportsDataAsync(request.SearchQuery, request.TimeRange, cancellationToken);
        var pageTotal = 0;
        if (result != null)
        {
            var paginationData = result.Results.RowData.GetRowWhere(row => row.RowValues.Any(rv => rv.Value.ToString() == "Total"));
            if (paginationData != null)
            {
                var matchingRow = paginationData.RowValues.FirstOrDefault(x => x.ValueName == "Count")?.Value.ToString();
                if (int.TryParse(matchingRow, out int number))
                {
                    pageTotal = number;
                }

                var removeRow = result.Results.RowData.Rows.FirstOrDefault(row => row.RowValues.Any(rv => rv.Value.ToString() == "Total"));
                result.Results.RowData.Rows.Remove(paginationData);
            }
        }

        var response = new PaginatedLogsDataResult(request.PageNumber, request.PageSize, pageTotal)
        {
            Results = await CreateNameColumn(result!.Results),
            TemplateId = request.TemplateId,
            PageSize = request.PageSize,
            PageNumber = request.PageNumber,
            ReportName = request.ReportName,
        };

        ViewBag.DataAssertField = request.DataAssertField;
        ViewBag.DataAssetAction = request.DataAssetAction;
        ViewBag.Propertype = request.Propertype;

        ViewBag.SearchQuery = rawQuery;
        ViewBag.TimeRange = request.TimeRange;
        ViewBag.ReportName = request.ReportName;
        return response;
    }

    private async Task<TelemetryQueryResultsData> CreateNameColumn(TelemetryQueryResultsData dataResult)
    {
        int ageColumnIndex = dataResult.ColumnData.Columns.FindIndex(column => column.Name == "UserId");

        if(ageColumnIndex == -1)
        {
            return dataResult;
        }

        List<string> userIds = new List<string>();
        foreach (var row in dataResult.RowData.Rows)
        {
            if (ageColumnIndex >= 0 && ageColumnIndex < row.RowValues.Count)
            {
                if (row.RowValues[ageColumnIndex].Value.ToString() is string ageValue)
                {
                    if (!userIds.Contains(ageValue))
                    {
                        userIds.Add(ageValue);
                    }
                }
            }
        }


        
        var userIdsWithName = new List<UserInfo>();
        foreach (var item in userIds)
        {
            if(!string.IsNullOrEmpty(item) && item != "-1")
            {
                var userProfile = await _userRoleService.GetUserByIdAsync(item);
                userIdsWithName.Add(new UserInfo() { UserId = userProfile.User.UserId, UserEmail = userProfile.User.UserEmail, UserName = userProfile.User.UserName});
            }
            else
            {
                userIdsWithName.Add(new UserInfo() { UserId = -1, UserName = "No user", UserEmail = "No email"});
            }
            
        }


        string newColumnName = "UserName";
        dataResult.ColumnData.Columns.Add(new TelemetryQueryResultsTableColumn() { Name = "UserName", Type = TelemetryQueryResultsTableValueType.String });
        dataResult.ColumnData.Columns.Add(new TelemetryQueryResultsTableColumn() { Name = "Email", Type = TelemetryQueryResultsTableValueType.String });

        foreach (var row in dataResult.RowData.Rows)
        {
            //if (row[""])
            var currentRows = row.RowValues.FirstOrDefault(x => x.ValueName == "UserId");

            if(currentRows != null)
            {
                foreach (var userVal in userIdsWithName)
                {
                    if (userVal.UserId.ToString() == currentRows.Value.ToString())
                    {
                        row.RowValues.Add(new TelemetryQueryResultsTableRowValue() { ValueType = TelemetryQueryResultsTableValueType.String, Value = userVal.UserName, ValueName = "UserName" });
                        row.RowValues.Add(new TelemetryQueryResultsTableRowValue() { ValueType = TelemetryQueryResultsTableValueType.String, Value = userVal.UserEmail, ValueName = "UserName" });
                        break;
                    }
                }
            }
        }
        return dataResult;
    }

    private async Task CreateReport(ReportTemplate template)
    {
        var filePath = $"{template.ReportType}/{template.TemplateId}.json";
        var jsonHandler = new ReadWriteJson<ReportTemplate>(filePath);

        // Write the user object to a JSON file
        await jsonHandler.WriteJsonAsync(template);

    }
    #endregion
}
