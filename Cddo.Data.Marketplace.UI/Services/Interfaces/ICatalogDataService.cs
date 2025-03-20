using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.UI.Model;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces;

public interface ICatalogDataService
{
    Task<IEnumerable<string>> GetCddoTopicsAsync(
        IEnumerable<DataAssetStatus>? dataAssetStatuses = null,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<string>> GetCddoOrganisationsAsync(
        IEnumerable<DataAssetStatus>? dataAssetStatuses = null,
        CancellationToken cancellationToken = default);
    
    Task<GetCddoDataAssetsResponse?> GetDataAssetsAsync(
        GetCddoDataAssetsRequest getCddoDataAssetsRequest,
        CancellationToken cancellationToken = default);

    Task<CatalogueFilterOptions?> GetCatalogueFilterOptionsAsync(
        GetCddoDataAssetsRequest getCddoDataAssetsRequest,
        CancellationToken cancellationToken = default);


    Task<GetCddoDataAssetsResponse?> GetDataAssetsByUserAsync(
        GetCddoDataAssetsRequest getCddoDataAssetsRequest,
        CancellationToken cancellationToken = default);
    
    Task<GetCddoDataAssetResponse?> GetDataAssetAsync(
        Guid dataAssetId,
        CancellationToken cancellationToken = default);

    Task<GetCddoDataAssetValidationErrorsResponse?> GetDataAssetValidationErrorsAsync(
        Guid dataAssetId,
        CancellationToken cancellationToken = default);
    
    Task<DeleteProfiledDataAssetResponse?> DeleteDataAssetAsync(
        DeleteProfiledDataAssetRequest deleteProfiledDataAssetRequest,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<string>> GetSearchSuggestionsForPublishedDataAssetsAsync(
        string searchText, 
        CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetSearchSuggestionsForOrganisationDataAssetsAsync(
        string searchText,
        CancellationToken cancellationToken = default);

    Task<CheckForPotentialDuplicatesToDataAssetResponse?> CheckForPotentialDuplicatesToDataAssetAsync(
        Guid dataAssetId,
        CancellationToken cancellationToken = default);
}