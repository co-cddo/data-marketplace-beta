using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;

public class LogsQueryDataResult
{
    public TelemetryQueryResultsData Results { get; init; }

    public TelemetryQueryResultsQueryStatistics QueryStatistics { get; init; }
}

public class LogsQueryDataRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? TemplateId { get; set; }
    public string? TimeRange { get; set; }
    public string? ReportName { get; set; }
    public string? SearchQuery { get; set; }
    public string? DataAssetAction { get; set; } // TODO Map to Logic DTO
    public DataAssetPropertyValidationErrorType? Propertype { get; set; }
    public CatalogAssetField? DataAssertField { get; set; }
}

public class PaginatedLogsDataResult : LogsQueryDataResult
{
    public PaginatedLogsDataResult(int pageNumber, int pageSize, int pageTotal)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecodsCount = pageTotal;
        TotalPages = (int)Math.Ceiling((double)pageTotal / PageSize);
    }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalRecodsCount { get; set; }
    public int TotalPages { get; set; }
    public Guid? TemplateId { get; set; }
    public string? ReportName { get; set; }
}