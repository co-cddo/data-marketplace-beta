namespace Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

public interface ICatalogReportsFilter
{
    bool FilterByInitiatingUserPermissions { get; }

    List<CatalogReportFieldFilter> FieldFilters { get; }
}