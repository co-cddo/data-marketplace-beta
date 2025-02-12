using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataDescription.NewDescription.Upload
{
    public class SpreadsheetDataAssetsModel : PageModel
    {
        public required IProfiledDataAssetsSpreadsheetValidationSummary ValidationSummary { get; init; }

        public required List<PotentialDuplicatesToSpreadsheetItemInformation> PotentialDuplicatesToSpreadsheetContentItems { get; init; }
    }
}
