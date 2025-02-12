using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;

public class DataAssetManagementSettingsRequest : CatalogDataRequestBase
{
    public DataAssetType DataAssetType { get; set; }

    public DataShareRequestNotificationRecipientType? SelectedDataShareRequestNotificationRecipientType { get; set; }

    public string? CustomDsrNotificationAddress { get; set; }

    public string MaintainerEmailAddress { get; set; }

    public string? DomainDsrNotificationMailboxAddress { get; set; }
}