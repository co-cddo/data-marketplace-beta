using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces;

public interface ICatalogQuestionsService
{
    Task<AddProfiledDataAssetResponse?> CreateProfiledDataAssetTitleAsync(QuestionFirstCreationRequest questionFirstCreationRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateTitleAsync(QuestionTitleRequest questionTitleRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateDescriptionAsync(QuestionDescriptionRequest questionDescriptionRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateIdentifierAsync(QuestionSupplierIdentifierRequest questionSupplierIdentifierRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateThemesAsync(QuestionThemeRequest questionThemeRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateKeywordsAsync(QuestionKeywordRequest questionKeywordRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateContactPointAsync(QuestionContactPointRequest questionContactRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateSecurityClassificationAsync(QuestionSecurityClassificationRequest questionSecurityClassificationRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateIssuedAsync(QuestionIssuedRequest questionIssuedRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateDistributionAsync(QuestionDistributionRequest questionDistributionRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateUpdateFrequencyAsync(QuestionUpdateFrequencyRequest questionUpdateFrequencyRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateDataShareRequestNotificationsSelectionAsync(DataShareRequestNotificationsRequest dataShareRequestNotificationsRequest, DataAssetType dataAssetType);
    Task<PatchProfiledDataAssetResponse?> UpdateDataAssetStatusAsync(string identifier, DataAssetStatus dataAssetStatus, DataAssetType dataAssetType);
}
