using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;

public class DataShareRequestNotificationsRequest : CatalogDataRequestBase
{
    public DataShareRequestNotificationRecipientType? SelectedDataShareRequestNotificationRecipientType { get; set; }

    public string? CustomDsrNotificationAddress { get; set; }

    public string MaintainerEmailAddress { get; set; }

    public string? DomainDsrNotificationMailboxAddress { get; set; }
}