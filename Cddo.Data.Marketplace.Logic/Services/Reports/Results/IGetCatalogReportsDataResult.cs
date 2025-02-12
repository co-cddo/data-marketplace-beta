using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;

namespace Cddo.Data.Marketplace.Logic.Services.Reports.Results;

public interface IGetCatalogReportsDataResult
{
    int TotalNumberOfMatchedRecords { get; }

    IEnumerable<CatalogReportsDataItem> CatalogReportsDataItems { get; }
}