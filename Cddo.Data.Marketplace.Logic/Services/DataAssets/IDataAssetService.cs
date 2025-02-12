using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;
using Agm.Catalog.DotNet.Logic.Services.EmbeddedResourceProvision;
using Agm.Catalog.DotNet.Logic.Services.Lookup.Results;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Logic.Services.DataAssets
{
    public interface IDataAssetService
    {
        Task<IServiceOperationDataResult<IAddDataAssetResult>> AddProfiledDataAssetAsync(
            IUserDetails initiatingUserDetails,
            string profileId,
            DataAssetType dataAssetType,
            string payload,
            ManagementMetadataBase managementMetadata,
            DataAssetActionSourceEnum actionSource);

        Task<IServiceOperationDataResult<IPatchDataAssetResult>> PatchProfiledDataAssetAsync(
            IUserDetails initiatingUserDetails,
            string profileId,
            DataAssetType dataAssetType,
            string? payload,
            ManagementMetadataBase? managementMetadata,
            DataAssetActionSourceEnum actionSource);

        Task<IServiceOperationDataResult<IGetProfiledDataAssetsResult>> GetProfiledDataAssetsAsync(
            IUserDetails initiatingUserDetails,
            string profileId,
            IEnumerable<DataAssetType> dataAssetTypes,
            bool? onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
            bool? onlyIncludeRecordsOwnedByOrganisationOfCallingUser,
            string? searchText);

        Task<IServiceOperationDataResult<IDeleteDataAssetResult>> DeleteProfiledDataAssetAsync(
            IUserDetails initiatingUserDetails,
            string profileId,
            Guid dataAssetId);

        Task<IServiceOperationDataResult<IGetProfiledDataAssetIdsResult>> GetProfiledDataAssetIdsAsync(
            IUserDetails initiatingUserDetails,
            string profileId);

        Task<IServiceOperationDataResult<IGetProfiledDataAssetResult>> GetProfiledDataAssetAsync(
            IUserDetails initiatingUserDetails,
            string profileId,
            Guid dataAssetId);

        Task<IServiceOperationDataResult<IGetCddoDataAssetsResult>> GetCddoDataAssetsAsync(
            IUserDetails initiatingUserDetails,
            bool? onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
            bool? onlyIncludeRecordsOwnedByOrganisationOfCallingUser,
            IEnumerable<DataAssetType>? dataAssetTypes,
            IEnumerable<DataAssetStatus>? dataAssetStatuses,
            int startIndex,
            int numberOfAssets,
            DataAssetsSortField sortField,
            DataAssetsSortDirection sortDirection,
            string? searchText,
            IEnumerable<string>? publishers,
            IEnumerable<string>? themes,
            IEnumerable<string>? entryTypes);

        Task<IServiceOperationDataResult<IGetCddoDataAssetResult>> GetCddoDataAssetAsync(
            IUserDetails initiatingUserDetails,
            Guid dataAssetId);

        Task<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>> GetCddoDataAssetValidationPropertyErrorsAsync(
            IUserDetails initiatingUserDetails,
            Guid dataAssetId);

        Task<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetResult>> CheckForPotentialDuplicatesToDataAssetAsync(
            IUserDetails initiatingUserDetails,
            Guid dataAssetId);

        Task<IServiceOperationDataResult<IGetCddoOrganisationsResult>> GetCddoOrganisationsAsync(
            IUserDetails initiatingUserDetails,
            IEnumerable<DataAssetStatus>? dataAssetStatuses);

        Task<IServiceOperationDataResult<IGetCddoTopicsResult>> GetCddoTopicsAsync(
            IUserDetails initiatingUserDetails,
            IEnumerable<DataAssetStatus>? dataAssetStatuses);

        Task<IServiceOperationDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>> GetSearchSuggestionsForPublishedDataAssetsAsync(
            IUserDetails initiatingUserDetails,
            string searchText);

        Task<IServiceOperationDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>> GetSearchSuggestionsForOrganisationDataAssetsAsync(
            IUserDetails initiatingUserDetails,
            string searchText);

        Task<IServiceOperationDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>> ValidateProfiledDataAssetsSpreadsheetContentAsync(
            IUserDetails initiatingUserDetails,
            IFormFile dataAssetSpreadsheet,
            string dataAssetProfileId);

        Task<IServiceOperationDataResult<ValidatedProfiledDataAssetSet?>> GetValidatedProfiledDataAssetsSpreadsheetContentAsync(
            IUserDetails initiatingUserDetails);

        Task<IServiceOperationDataResult<IValidatedProfiledDataAsset>> GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(
            IUserDetails initiatingUserDetails,
            string recordId);

        Task<IServiceOperationDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>> PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(
            IUserDetails initiatingUserDetails, 
            PublishValidatedProfiledDataAssetsSpreadsheetContentRequest profiledDataAssetsSpreadsheetContentRequest);

        Task<IServiceOperationResult> ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(
            IUserDetails initiatingUserDetails);

        Task<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>> CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(
            IUserDetails initiatingUserDetails);

        Task<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>> CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(
            IUserDetails initiatingUserDetails,
            string recordId);

        Task<IServiceOperationDataResult<IEmbeddedResourceData>> GetDataAssetTemplateSpreadsheetAsync(
            IUserDetails initiatingUserDetails,
            string profileId);

        Task<IServiceOperationDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>> MigrateProfiledDataAssetsFrom1p0To3p1Async(
            IUserDetails initiatingUserDetails,
            IEnumerable<Guid> dataAssetIds);

        Task<IServiceOperationDataResult<IGetEsdaOwnershipDetailsResult>> GetEsdaOwnershipDetailsAsync(
            IUserDetails initiatingUserDetails,
            Guid dataAssetId);

        Task<IServiceOperationDataResult<IValidateCataloguedResourceResult>> ValidateCataloguedResourceAsync(
            string profileId,
            CataloguedResource cataloguedResource,
            DataAssetType dataAssetType,
            bool includeRequiredPropertiesInValidation);

        Task<ManagementMetadataDcatUkApV3_1> SetMetadataManagement(DataSet dataset, IUserDetails initiatingUserDetails, string profileId);
    }
}
