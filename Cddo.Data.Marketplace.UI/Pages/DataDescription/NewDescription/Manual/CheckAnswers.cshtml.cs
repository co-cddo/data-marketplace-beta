using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataDescription.NewDescription.Manual
{
    public class CheckAnswersModel : PageModel
    {
        public required CddoDataAsset? CddoDataAsset { get; init; }

        public required List<DataAssetValidationPropertyResult> PropertyValidationErrors { get; init; }

        public required List<PotentialDuplicateDataAssetInformation> PotentialDuplicatesToDataAsset { get; init; }
    }
}
