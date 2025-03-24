using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Logic.Services.Ckan;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.Reports.Results;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.Extensions.Logging;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

public class ReportsService(
    ILogger<ReportsService> logger,
    ICkanConnection ckanConnection,
    ICatalogReportsDataItemsBuilder catalogReportsDataItemsBuilder,
    IReportFieldFilterConverter reportFieldFilterConverter,
    IServiceOperationResultFactory serviceOperationResultFactory) : IReportsService
{
    async Task<IServiceOperationDataResult<IGetCatalogReportsDataResult>> IReportsService.GetCatalogReportsDataAsync(
        IUserDetails initiatingUserDetails,
        IEnumerable<CatalogAssetField> requiredFields,
        ICatalogReportsFilter? catalogReportsFilter,
        int startRecordIndex,
        int numberOfRecords,
         string? searchText)
    {
        ArgumentNullException.ThrowIfNull(requiredFields);
        ArgumentOutOfRangeException.ThrowIfNegative(startRecordIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(numberOfRecords);

        try
        {
            var resultPagination = new CatalogEntriesResultPagination
            {
                StartIndex = startRecordIndex,
                NumberOfAssets = numberOfRecords,
                SortField = Agm.Catalog.DotNet.Dto.Models.DataAssets.DataAssetsSortField.Title,
                SortDirection = Agm.Catalog.DotNet.Dto.Models.DataAssets.DataAssetsSortDirection.Ascending,
            };

            var catalogAssetFieldFilters = catalogReportsFilter?.FieldFilters
                .Select(reportFieldFilterConverter.ConvertReportFieldFilter).ToList() ?? [];

            var catalogEntriesOrganisationFilter = await BuildCatalogEntriesOrganisationFilterAsync();

            var filteredCatalogEntriesResultSet = await ckanConnection.GetFilteredCatalogEntriesAsync(
                catalogEntriesOrganisationFilter,
                catalogAssetFieldFilters,
                resultPagination,
                searchText);

            var catalogReportsDataItems = catalogReportsDataItemsBuilder.BuildCatalogReportsDataItems(
                requiredFields,
                filteredCatalogEntriesResultSet);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetCatalogReportsDataResult
            {
                TotalNumberOfMatchedRecords = filteredCatalogEntriesResultSet.Count,
                CatalogReportsDataItems = catalogReportsDataItems
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Catalog Reports Data");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetCatalogReportsDataResult>(ex.Message);

            return await Task.FromResult(response);
        }

        async Task<ICatalogEntriesOrganisationFilter> BuildCatalogEntriesOrganisationFilterAsync()
        {
            if (catalogReportsFilter?.FilterByInitiatingUserPermissions != true)
            {
                return await Task.FromResult(new CatalogEntriesOrganisationFilter
                {
                    FilterByOrganisationDiscoverability = false,
                    FilterByOrganisationOwnership = false
                });
            }

            return await Task.FromResult(new CatalogEntriesOrganisationFilter
            {
                OrganisationId = initiatingUserDetails.UserIdSet.OrganisationId.ToString(),
                FilterByOrganisationDiscoverability = true,
                FilterByOrganisationOwnership = false
            });
        }
    }

    async Task<IServiceOperationDataResult<IPerformCatalogReportsQueryResult>> IReportsService.PerformCatalogReportsQueryAsync(
        IUserDetails initiatingUserDetails)
    {
        try
        {
            return serviceOperationResultFactory.CreateSuccessfulDataResult(new PerformCatalogReportsQueryResult());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Perform Catalog Reports Query");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IPerformCatalogReportsQueryResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }
}
