using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Cddo.Data.Marketplace.Logic.Services.Reports.Results;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

internal class ReportsResponseFactory : IReportsResponseFactory
{
    QueryCatalogReportsDataResponse IReportsResponseFactory.CreateGetCatalogReportsDataResponse(
        IGetCatalogReportsDataResult getCatalogReportsDataResult)
    {
        ArgumentNullException.ThrowIfNull(getCatalogReportsDataResult);

        return new QueryCatalogReportsDataResponse
        {
            TotalNumberOfMatchedRecords = getCatalogReportsDataResult.TotalNumberOfMatchedRecords,
            CatalogReportsDataItems = getCatalogReportsDataResult.CatalogReportsDataItems.ToList()
        };
    }
}