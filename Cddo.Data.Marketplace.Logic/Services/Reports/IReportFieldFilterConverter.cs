using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

public interface IReportFieldFilterConverter
{
    ICatalogAssetFieldFilter ConvertReportFieldFilter(
        CatalogReportFieldFilter catalogReportFieldFilter);
}