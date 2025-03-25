using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.Reports.Results;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

public interface IReportsService
{
    Task<IServiceOperationDataResult<IGetCatalogReportsDataResult>> GetCatalogReportsDataAsync(
        IUserDetails initiatingUserDetails,
        IEnumerable<CatalogAssetField> requiredFields,
        ICatalogReportsFilter? catalogReportsFilter,
        int startRecordIndex,
        int numberOfRecords,
         string? searchText);

    Task<IServiceOperationDataResult<IPerformCatalogReportsQueryResult>> PerformCatalogReportsQueryAsync(
        IUserDetails initiatingUserDetails);
}