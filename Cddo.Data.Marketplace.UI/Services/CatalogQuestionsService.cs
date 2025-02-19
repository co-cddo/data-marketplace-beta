using System.Net;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl;
using Flurl.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData.DcatUk.V3_1;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetConversion;

namespace Cddo.Data.Marketplace.UI.Services;

public class CatalogQuestionsService : ICatalogQuestionsService
{
    private const string BaseRoute = "DataAsset";
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CatalogQuestionsService> _logger;
    private readonly string _profileId = "dcat-ukap-v3.1";
    private readonly string _apiUrl;

    public CatalogQuestionsService(
        ILogger<CatalogQuestionsService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiUrl = _configuration.GetSection("Api:Main").Value ?? throw new ArgumentNullException(nameof(_apiUrl));
    }

    private Task<string?> GetTokenAsync()
    {
        if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var idToken = httpContext?.Request.Cookies["CO-Datamarketplace"];
            return Task.FromResult(idToken);
        }
        return Task.FromResult<string?>(null);
    }

    private async Task<T?> MakeApiPostRequestAsync<T>(AddProfiledDataAssetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {

                request.ActionSource = DataAssetActionSourceEnum.UserInterface;

                var url = _apiUrl.AppendPathSegments(BaseRoute, "add-profiled-data-asset").WithOAuthBearerToken(token);

                var response = await url.PostJsonAsync(request, cancellationToken: cancellationToken);

                return await response.GetJsonAsync<T>();
            }
        }
        catch (FlurlHttpException ex)
        {
            var statusCode = ex.StatusCode;
            var exceptionText = ex.Message;
            var responseText = await ex.GetResponseStringAsync();
            throw new InvalidOperationException($"Flurl Exception thrown performing CKAN Catalog lookup action: Status Code: {statusCode}, Text: {responseText}, Exception: {exceptionText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return default;
    }

    private async Task<T?> MakeApiPatchRequestAsync<T>(PatchProfiledDataAssetRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {

                request.ActionSource = DataAssetActionSourceEnum.UserInterface;

                var url = _apiUrl.AppendPathSegments(BaseRoute, "patch-profiled-data-asset").WithOAuthBearerToken(token);

                var response = await url.PatchJsonAsync(request, cancellationToken: cancellationToken);

                return await response.GetJsonAsync<T>();
            }
        }
        catch (FlurlHttpException ex)
        {
            var statusCode = ex.StatusCode;

            if (statusCode == (int)HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException();
            }

            var exceptionText = ex.Message;
            var responseText = await ex.GetResponseStringAsync();
            throw new InvalidOperationException($"Flurl Exception thrown performing CKAN Catalog lookup action: Status Code: {statusCode}, Text: {responseText}, Exception: {exceptionText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return default;
    }

    public async Task<AddProfiledDataAssetResponse?> CreateProfiledDataAssetTitleAsync(QuestionFirstCreationRequest questionFirstCreationRequest, DataAssetType dataAssetType)
    {
        var request = new AddProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            ManagementMetadata = new ManagementMetadataDcatUkApV3_1
            {
                DcatUK3_1Properties = new DcatUK3_1SpecificProperties
                {
                    AllowDSRRequest = true,
                    RequiresDSR = true
                },
                DataAssetStatus = DataAssetStatus.Draft,
            },
            Payload = JsonSerializer.Serialize(questionFirstCreationRequest, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumWithEnumMemberConverter<Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum>() }
            })
        };
        return await MakeApiPostRequestAsync<AddProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateTitleAsync(QuestionTitleRequest questionTitleRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionTitleRequest)
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateDescriptionAsync(QuestionDescriptionRequest questionDescriptionRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionDescriptionRequest)
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateIdentifierAsync(QuestionSupplierIdentifierRequest questionSupplierIdentifierRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionSupplierIdentifierRequest)
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateThemesAsync(QuestionThemeRequest questionThemeRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionThemeRequest, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumWithEnumMemberConverter<Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.ThemeEnum>() }
            })
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateKeywordsAsync(QuestionKeywordRequest questionKeywordRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionKeywordRequest)
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateContactPointAsync(QuestionContactPointRequest questionContactRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionContactRequest, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumWithEnumMemberConverter<Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.ContactRoleEnum>() }
            })
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateIssuedAsync(QuestionIssuedRequest questionIssuedRequest, DataAssetType dataAssetType)
    {
        var payload = new
        {
            questionIssuedRequest.Identifier,
            Issued = questionIssuedRequest.metadataIssuedDate.Date
        };

        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(payload)
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateSecurityClassificationAsync(QuestionSecurityClassificationRequest questionSecurityClassificationRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionSecurityClassificationRequest, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumWithEnumMemberConverter<Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum>() }
            })
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateDistributionAsync(QuestionDistributionRequest questionDistributionRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionDistributionRequest, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumWithEnumMemberConverter<Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.ServiceTypeEnum>() }
            })
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateUpdateFrequencyAsync(QuestionUpdateFrequencyRequest questionUpdateFrequencyRequest, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(questionUpdateFrequencyRequest)
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateDataShareRequestNotificationsSelectionAsync(
        DataShareRequestNotificationsRequest dataShareRequestNotificationsRequest,
        DataAssetType dataAssetType)
    {
        var selectedDataShareRequestNotificationRecipientType = dataShareRequestNotificationsRequest.SelectedDataShareRequestNotificationRecipientType!.Value;
        var customDsrNotificationAddress = selectedDataShareRequestNotificationRecipientType == DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress
            ? dataShareRequestNotificationsRequest.CustomDsrNotificationAddress
            : null;

        var payload = new
        {
            identifier = dataShareRequestNotificationsRequest.Identifier
        };

        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            Payload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            }),
            ManagementMetadata = new ManagementMetadataDcatUkApV3_1
            {
                DataShareRequestNotificationRecipient = new DataShareRequestNotificationRecipient
                {
                    DataShareRequestNotificationRecipientType = selectedDataShareRequestNotificationRecipientType,
                    CustomDsrNotificationAddress = customDsrNotificationAddress
                }
            }
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }

    public async Task<PatchProfiledDataAssetResponse?> UpdateDataAssetStatusAsync(string identifier, DataAssetStatus dataAssetStatus, DataAssetType dataAssetType)
    {
        var request = new PatchProfiledDataAssetRequest
        {
            ProfileId = _profileId,
            DataAssetType = dataAssetType,
            ManagementMetadata = new ManagementMetadataDcatUkApV3_1
            {
                DataAssetStatus = dataAssetStatus
            },
            Payload = JsonSerializer.Serialize(new { Identifier = identifier })
        };
        return await MakeApiPatchRequestAsync<PatchProfiledDataAssetResponse>(request);
    }
}
