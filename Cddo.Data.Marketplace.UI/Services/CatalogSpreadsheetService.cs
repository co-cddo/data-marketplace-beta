using System.Text.Json;
using System.Text.Json.Serialization;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl.Http;
using Flurl;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results.SpreadsheetIngestion.ValidatedDataAssetSpreadsheetItems;

namespace Cddo.Data.Marketplace.UI.Services
{
    public class CatalogSpreadsheetService : ICatalogSpreadsheetService
    {
        private const string BaseRoute = "DataAsset";
        private const string dataAssetProfileId = "dcat-ukap-v3.1";

        private readonly string _apiUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CatalogSpreadsheetService> _logger;
        private readonly ICddoFlurlExceptionBuilder _cddoFlurlExceptionBuilder;
        private readonly IValidatedDataAssetSpreadsheetItemSummaryBuilder _validatedDataAssetSpreadsheetItemSummaryBuilder;
        private readonly IDataShareRequestMailboxAddressValidation _dataShareRequestMailboxAddressValidation;

        public CatalogSpreadsheetService(
            ILogger<CatalogSpreadsheetService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ICddoFlurlExceptionBuilder cddoFlurlExceptionBuilder,
            IValidatedDataAssetSpreadsheetItemSummaryBuilder validatedDataAssetSpreadsheetItemSummaryBuilder,
            IDataShareRequestMailboxAddressValidation dataShareRequestMailboxAddressValidation)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiUrl = configuration.GetSection("Api:Main").Value ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _cddoFlurlExceptionBuilder = cddoFlurlExceptionBuilder;
            _validatedDataAssetSpreadsheetItemSummaryBuilder = validatedDataAssetSpreadsheetItemSummaryBuilder;
            _dataShareRequestMailboxAddressValidation = dataShareRequestMailboxAddressValidation ?? throw new ArgumentNullException(nameof(dataShareRequestMailboxAddressValidation));
        }

        private async Task<string?> GetTokenAsync()
        {
            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                string idToken = httpContext.Request.Cookies["CO-Datamarketplace"];
                return idToken;
            }
            return null;
        }

        public async Task<byte[]> DownloadSpreadsheetTemplateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await GetTokenAsync();

                var data = await _apiUrl
                    .AppendPathSegments(BaseRoute, "get-data-asset-template-spreadsheet")
                    .SetQueryParam("ProfileId", dataAssetProfileId)
                    .WithOAuthBearerToken(token)
                    .GetBytesAsync(cancellationToken: cancellationToken);

                return data;
            }
            catch (FlurlHttpException ex)
            {
                var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
                _logger.LogError(ex, "Failed to Download Spreadsheet Template: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while downloading the Spreadsheet Template");
                return [];
            }
        }

        public async Task<GetValidatedProfiledDataAssetsSpreadsheetContentResponse?> UploadSpreadsheetAsync(IFormFile uploadFile, CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        await uploadFile.CopyToAsync(stream, cancellationToken);
                        stream.Position = 0;

                        using (var formData = new MultipartFormDataContent())
                        {
                            var fileContent = new StreamContent(stream);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(uploadFile.ContentType);
                            formData.Add(fileContent, "file", uploadFile.FileName);

                            var response = await _apiUrl
                                .AppendPathSegments(BaseRoute, "validate-profiled-data-assets-spreadsheet-content")
                                .SetQueryParam("dataAssetProfileId", dataAssetProfileId)
                                .WithOAuthBearerToken(token)
                                .PostAsync(content: formData, cancellationToken: cancellationToken);

                            var foo = response.GetStringAsync().Result;
                            var responseString = JsonConvert.DeserializeObject<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>(foo);

                            return responseString;
                        }
                    }
                }
                catch (FlurlHttpException ex)
                {
                    var cddoFlurlException = await _cddoFlurlExceptionBuilder.BuildAsync(ex);
                    _logger.LogError(ex, "Failed to Upload Data Descriptions from file. Flurl Response: {FlurlResponseText}", cddoFlurlException.FlurlResponseText);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while uploading the data description. Error: {ErrorMessage}", ex.Message);
                }
            }
            return null;
        }

        public async Task<GetValidatedProfiledDataAssetsSpreadsheetContentResponse?> GetValidatedDataAssetsSpreadsheetAsync(CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var url = _apiUrl
                        .AppendPathSegments(BaseRoute, "get-validated-profiled-data-assets-spreadsheet-content");

                    var responseObject = await url
                        .WithOAuthBearerToken(token)
                        .WithSettings(x =>
                            x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                            {
                                Converters = { new JsonStringEnumConverter() },
                                PropertyNameCaseInsensitive = true
                            }))
                        .GetJsonAsync<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>(cancellationToken: cancellationToken);

                    return responseObject;
                }
                catch (JsonSerializationException jex)
                {
                    _logger.LogError(jex, "JSON Serialization Exception: {Message}", jex.Message);
                }
                catch (FlurlHttpException ex)
                {
                    var responseString = await ex.GetResponseStringAsync();
                    _logger.LogError(ex, "Flurl HTTP Exception: {ResponseString}", responseString);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return null;
            }
            return null;
        }

        public async Task<IValidatedDataAssetSpreadsheetItemSummary> GetValidatedDataAssetSpreadsheetItemAsync(string recordId, CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var url = _apiUrl
                        .AppendPathSegments(BaseRoute, "get-validated-profiled-data-assets-spreadsheet-item-content");

                    var responseObject = await url
                        .WithOAuthBearerToken(token)
                        .SetQueryParam("RecordId", recordId)
                        .WithSettings(x =>
                            x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                            {
                                Converters = { new JsonStringEnumConverter() },
                                PropertyNameCaseInsensitive = true
                            }))
                        .GetJsonAsync<GetValidatedProfiledDataAssetsSpreadsheetItemContentResponse>(cancellationToken: cancellationToken);

                    return _validatedDataAssetSpreadsheetItemSummaryBuilder.BuildFromResponse(responseObject);
                }
                catch (JsonSerializationException jex)
                {
                    _logger.LogError(jex, "JSON Serialization Exception: {Message}", jex.Message);
                }
                catch (FlurlHttpException ex)
                {
                    var responseString = await ex.GetResponseStringAsync();
                    _logger.LogError(ex, "Flurl HTTP Exception: {ResponseString}", responseString);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
            return null;
        }

        public async Task<IPublishSpreadsheetDataAssetsResult> PublishSpreadsheetDataAssetsAsync(
    IFormCollection formData,
    CancellationToken cancellationToken = default)
        {
            var validationResult = ValidateDataShareRequestNotificationAddress(formData);
            if (!validationResult.RequestWasValid)
            {
                return new PublishSpreadsheetDataAssetsResult
                {
                    DataShareRequestNotificationAddressValidationResult = validationResult,
                    Response = null
                };
            }

            var publishResponse = await PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(
                validationResult.SelectedRecipientType!.Value,
                validationResult.EnteredCustomAddress,
                cancellationToken);

            return new PublishSpreadsheetDataAssetsResult
            {
                DataShareRequestNotificationAddressValidationResult = validationResult,
                Response = publishResponse
            };
        }

        private IDataShareRequestNotificationAddressValidationResult ValidateDataShareRequestNotificationAddress(IFormCollection formData)
        {
            if (!formData.TryGetValue("dsr-notification-option", out var selectedOption))
            {
                return BuildValidationResult(false, "dsr-notification-options", "Select where data share request notifications should be sent to");
            }

            var recipientType = Enum.Parse<DataShareRequestNotificationRecipientType>(selectedOption.ToString());
            if (recipientType == DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress)
            {
                return ValidateCustomAddress(formData, recipientType);
            }

            return BuildValidationResult(true, selectedRecipientType: recipientType);
        }

        private IDataShareRequestNotificationAddressValidationResult ValidateCustomAddress(IFormCollection formData, DataShareRequestNotificationRecipientType recipientType)
        {
            const string customAddressInputName = "custom-address";
            var customAddress = formData[customAddressInputName].ToString();

            if (customAddress.Length > 255)
            {
                return BuildValidationResult(false, customAddressInputName, "Data share request mailbox address is too long.", recipientType, customAddress);
            }

            if (!_dataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(customAddress, out var validationError))
            {
                return BuildValidationResult(false, customAddressInputName, validationError!, recipientType, customAddress);
            }

            return BuildValidationResult(true, selectedRecipientType: recipientType, enteredCustomAddress: customAddress);
        }

        private IDataShareRequestNotificationAddressValidationResult BuildValidationResult(
            bool requestWasValid,
            string? errorKey = null,
            string? errorMessage = null,
            DataShareRequestNotificationRecipientType? selectedRecipientType = null,
            string? enteredCustomAddress = null)
        {
            var validationErrors = new Dictionary<string, string>();
            if (errorKey != null && errorMessage != null)
            {
                validationErrors[errorKey] = errorMessage;
            }

            return new DataShareRequestNotificationAddressValidationResult
            {
                RequestWasValid = requestWasValid,
                SelectedRecipientType = selectedRecipientType,
                EnteredCustomAddress = enteredCustomAddress,
                ValidationErrors = validationErrors
            };
        }

        private async Task<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse?> PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(
            DataShareRequestNotificationRecipientType recipientType,
            string? customAddress,
            CancellationToken cancellationToken)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var request = new PublishValidatedProfiledDataAssetsSpreadsheetContentRequest
                {
                    DataShareRequestNotificationRecipient = new DataShareRequestNotificationRecipient
                    {
                        DataShareRequestNotificationRecipientType = recipientType,
                        CustomDsrNotificationAddress = customAddress
                    }
                };

                var url = _apiUrl.AppendPathSegments(BaseRoute, "publish-validated-profiled-data-assets-spreadsheet-content");

                return await url
                    .WithSettings(x => x.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() },
                        PropertyNameCaseInsensitive = true
                    }))
                    .WithOAuthBearerToken(token)
                    .PostJsonAsync(request, cancellationToken: cancellationToken)
                    .ReceiveJson<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            }
            catch (Exception ex)
            {
                LogException(ex);
                return null;
            }
        }

        private void LogException(Exception ex)
        {
            switch (ex)
            {
                case FlurlHttpException httpEx:
                    _logger.LogError(httpEx, "Flurl HTTP Exception occurred. Response: {ResponseString}", httpEx.GetResponseStringAsync().Result);
                    break;
                case JsonSerializationException jsonEx:
                    _logger.LogError(jsonEx, "JSON Serialization Exception occurred. Message: {Message}", jsonEx.Message);
                    break;
                default:
                    _logger.LogError(ex, "An unexpected error occurred. Error: {ErrorMessage}", ex.Message);
                    break;
            }
        }

        public async Task<string?> ClearSpreadsheetDataAssets(CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var url = _apiUrl
                        .AppendPathSegments(BaseRoute, "clear-validated-profiled-data-assets-spreadsheet-content");

                    var flurlResponse = await url
                        .WithOAuthBearerToken(token)
                        .PostAsync(null, cancellationToken: cancellationToken);

                    var response = await flurlResponse.GetStringAsync();

                    return response;
                }
                catch (FlurlHttpException ex)
                {
                    var responseString = await ex.GetResponseStringAsync();
                    _logger.LogError(ex, "Flurl HTTP Exception occurred. Response: {ResponseString}", responseString);
                }
                catch (JsonSerializationException jex)
                {
                    _logger.LogError(jex, "JSON Serialization Exception occurred. Message: {Message}", jex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unexpected exception occurred. Error: {ErrorMessage}", ex.Message);
                }
                return null;
            }
            return null;
        }

        public async Task<CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse?> CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync(
            CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var url = _apiUrl
                    .AppendPathSegments(BaseRoute, "check-for-potential-duplicates-in-validated-spreadsheet-content");

                var input = new CheckForPotentialDuplicatesInValidatedSpreadsheetContentRequest();

                var response = await url
                    .AppendQueryParam(input)
                    .WithOAuthBearerToken(token)
                    .GetJsonAsync<CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse>(cancellationToken: cancellationToken);

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex,
                    "Flurl HTTP Exception occurred while checking for potential duplicates in validated spreadsheet content. Response: {ResponseString}",
                    responseString);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An exception occurred while checking for potential duplicates in validated spreadsheet content. Error: {ErrorMessage}",
                    ex.Message);

                return null;
            }
        }

        public async Task<CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse?> CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(
            string recordId,
            CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var url = _apiUrl
                    .AppendPathSegments(BaseRoute, "check-for-potential-duplicates-in-validated-spreadsheet-item");

                var input = new CheckForPotentialDuplicatesInValidatedSpreadsheetItemRequest
                {
                    RecordId = recordId
                };

                var response = await url
                    .AppendQueryParam(input)
                    .WithOAuthBearerToken(token)
                    .GetJsonAsync<CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse>(cancellationToken: cancellationToken);

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex,
                    "Flurl HTTP Exception checking for potential duplicates in validated spreadsheet item. Response: {ResponseString}",
                    responseString);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception checking for potential duplicates in validated spreadsheet item. Error: {ErrorMessage}",
                    ex.Message);

                return null;
            }
        }
    }
}
