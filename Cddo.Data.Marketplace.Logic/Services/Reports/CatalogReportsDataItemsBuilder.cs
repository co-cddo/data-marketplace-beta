using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetConversion;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

internal class CatalogReportsDataItemsBuilder(
    IProfiledDataAssetConverterPresenter profiledDataAssetConverterPresenter) : ICatalogReportsDataItemsBuilder
{
    IEnumerable<CatalogReportsDataItem> ICatalogReportsDataItemsBuilder.BuildCatalogReportsDataItems(
        IEnumerable<CatalogAssetField> requiredFields,
        CkanPackageSearchResponseResultSet resultSet)
    {
        ArgumentNullException.ThrowIfNull(resultSet);

        var requiredFieldsList = requiredFields.ToList();

        foreach (var ckanCatalogEntryRead in resultSet.Results)
        {
            var profileIdExtra = ckanCatalogEntryRead.Extras?.FirstOrDefault(x => x.Key.Equals("profileId", StringComparison.InvariantCultureIgnoreCase));
            if (profileIdExtra == null) continue;

            var profileId = profileIdExtra.Value.ToString();
            if (string.IsNullOrWhiteSpace(profileId)) continue;

            if (!profiledDataAssetConverterPresenter.TryGetProfiledDataAssetConverterForProfileId(profileId, out var profiledDataAssetConverter)) continue;

            yield return profiledDataAssetConverter!.ConvertCkanCatalogEntryReadToCatalogReportsDataItem(
                ckanCatalogEntryRead,
                requiredFieldsList);
        }
    }
}