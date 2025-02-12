using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results.SpreadsheetIngestion.ValidatedDataAssetSpreadsheetItems;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces;

public interface ICatalogSpreadsheetService
{
    Task<byte[]> DownloadSpreadsheetTemplateAsync(
        CancellationToken cancellationToken = default);

    Task<GetValidatedProfiledDataAssetsSpreadsheetContentResponse?> UploadSpreadsheetAsync(
        IFormFile uploadFile,
        CancellationToken cancellationToken = default);

    Task<GetValidatedProfiledDataAssetsSpreadsheetContentResponse?> GetValidatedDataAssetsSpreadsheetAsync(
        CancellationToken cancellationToken = default);

    Task<IValidatedDataAssetSpreadsheetItemSummary> GetValidatedDataAssetSpreadsheetItemAsync(
        string recordId,
        CancellationToken cancellationToken = default);

    Task<IPublishSpreadsheetDataAssetsResult> PublishSpreadsheetDataAssetsAsync(
        IFormCollection formData,
        CancellationToken cancellationToken = default);

    Task<string?> ClearSpreadsheetDataAssets(CancellationToken cancellationToken = default);

    Task<CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse?> CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync(
        CancellationToken cancellationToken = default);

    Task<CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse?> CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(
        string recordId,
        CancellationToken cancellationToken = default);
}
