using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataDescription
{
    public class EditDataAssetManagementSettingsModel : PageModel
    {
        public required ICddoDataAsset CddoDataAsset { get; init; }

        public required IDomainInformation EsdaDomainInformation { get; init; }
    }
}
