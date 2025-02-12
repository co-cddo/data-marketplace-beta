using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using CDDO.DataMarketplace.Controllers.External;
using System.Text.Json;

internal static class DataMarketplaceApiControllerHelpers
{

    public static JsonSerializerOptions JsonSerializationOptions => new()
    {
        Converters =
            {
                new JsonStringEnumWithEnumMemberConverter<ResourceEnum>(),
                new JsonStringEnumWithEnumMemberConverter<AccessRightsEnum>(),
                new JsonStringEnumWithEnumMemberConverter<SecurityClassificationEnum>(),
                new JsonStringEnumWithEnumMemberConverter<ResourceStatusEnum>(),
                new JsonStringEnumWithEnumMemberConverter<ThemeEnum>(),
                new JsonStringEnumWithEnumMemberConverter<ContactRoleEnum>(),
                new JsonStringEnumWithEnumMemberConverter<ServiceTypeEnum>()
            },
        PropertyNameCaseInsensitive = true
    };
}