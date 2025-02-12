using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;

public class DataShareRequestNotificationsSelectionRequest : CatalogDataRequestBase
{
    public required DataShareRequestNotificationRecipientType? SelectedRecipientType { get; init; }

    public required string? EnteredCustomAddress { get; init; }

    public required string? MaintainerEmailAddress { get; init; }

    public required string? DomainDsrNotificationMailboxAddress { get; init; }
}