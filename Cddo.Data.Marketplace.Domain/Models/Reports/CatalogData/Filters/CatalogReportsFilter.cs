namespace Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

public class CatalogReportsFilter : ICatalogReportsFilter
{
    public bool FilterByInitiatingUserPermissions { get; set; }

    public List<CatalogReportFieldFilter> FieldFilters { get; set; } = [];
}