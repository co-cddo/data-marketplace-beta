using System.ComponentModel.DataAnnotations;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Reports;

public class QueryCatalogReportsDataRequestFilter
{
    [Required]
    public List<CatalogAssetField> RequiredFields { get; set; }

    public List<string>? Organisations { get; set; }
    public List<DataAssetStatus>? DataAssetStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SelectableOrganisations { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}