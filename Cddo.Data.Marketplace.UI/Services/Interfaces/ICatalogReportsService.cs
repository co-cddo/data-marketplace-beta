using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces
{
    public interface ICatalogReportsService
    {
        Task<QueryCatalogReportsDataResponse?> GetCatalogReportsDataAsync(QueryCatalogReportsDataRequest queryCatalogReportsDataRequest, CancellationToken cancellationToken = default);
        Task<QueryCatalogReportsDataResponse?> DownloadCatalogReportsDataAsync(QueryCatalogReportsDataRequest queryCatalogReportsDataRequest, CancellationToken cancellationToken = default);
        Task<LogsQueryDataResult> GetTelemetryReportsDataAsync(string searchQuery, string timeRange, CancellationToken cancellationToken = default);
        Task<QueryDataShareRequestsCountsResponse> GetDSRReportDataAsync(QueryDataShareRequestsCountsRequest queryDataShareRequestsCountsRequest, CancellationToken cancellationToken = default);
        string PrettifyString(string? input);
    }
}
