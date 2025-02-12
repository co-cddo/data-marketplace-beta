using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataDescription.NewDescription.Upload
{
    public class DataShareRequestNotificationsSelectionModel : PageModel
    {
        public required DataShareRequestNotificationRecipientType? SelectedRecipientType { get; init; }

        public required string? EnteredCustomAddress { get; init; }

        public required IDomainInformation UserDomainInformation { get; init; }
    }
}
