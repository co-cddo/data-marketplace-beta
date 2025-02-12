using Agm.Catalog.DotNet.Dto.Responses.DataAssets.Models;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results.SpreadsheetIngestion.ValidatedDataAssetSpreadsheetItems;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataDescription.NewDescription.Upload
{
    public class SpreadsheetDataAssetSummaryModel : PageModel
    {
        public required IValidatedDataAssetSpreadsheetItemSummary ItemSummary { get; init; }

        public required PotentialDuplicatesToSpreadsheetItemInformation PotentialDuplicatesToSpreadsheetItem { get; init; }
    }
}
