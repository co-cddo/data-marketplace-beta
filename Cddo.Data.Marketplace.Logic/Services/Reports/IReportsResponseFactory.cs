using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Cddo.Data.Marketplace.Logic.Services.Reports.Results;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

public interface IReportsResponseFactory
{
    QueryCatalogReportsDataResponse CreateGetCatalogReportsDataResponse(
        IGetCatalogReportsDataResult getCatalogReportsDataResult);
}