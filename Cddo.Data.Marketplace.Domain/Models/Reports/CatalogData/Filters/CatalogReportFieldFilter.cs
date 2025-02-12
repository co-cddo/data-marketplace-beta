using Agm.Catalog.DotNet.Dto.Models.CatalogData;

namespace Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

public class CatalogReportFieldFilter
{
    public CatalogAssetField Field { get; set; }

    public List<string> Values { get; set; } = [];
}