using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Dataset;

public class DatasetResultsModel : PageModel
{
    public required List<CddoDataAsset> DataAssets { get; init; }

    public required int TotalNumberOfResults { get; init; }

    public required List<string> Topics { get; init; }

    public required List<string> Organisations { get; init; }
}