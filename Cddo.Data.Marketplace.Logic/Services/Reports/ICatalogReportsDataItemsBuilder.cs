using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

public interface ICatalogReportsDataItemsBuilder
{
    IEnumerable<CatalogReportsDataItem> BuildCatalogReportsDataItems(
        IEnumerable<CatalogAssetField> requiredFields,
        CkanPackageSearchResponseResultSet resultSet);
}