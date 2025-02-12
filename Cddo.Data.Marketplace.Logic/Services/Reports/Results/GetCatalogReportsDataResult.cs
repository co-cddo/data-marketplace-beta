using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;

namespace Cddo.Data.Marketplace.Logic.Services.Reports.Results;

internal class GetCatalogReportsDataResult : IGetCatalogReportsDataResult
{
    public required int TotalNumberOfMatchedRecords { get; init; }

    public required IEnumerable<CatalogReportsDataItem> CatalogReportsDataItems { get; init; }
}