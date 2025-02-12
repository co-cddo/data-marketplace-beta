using System.ComponentModel.DataAnnotations;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Reports;

public class QueryCatalogReportsDataRequest
{
    [Required]
    public List<CatalogAssetField> RequiredFields { get; set; }

    public CatalogReportsFilter? Filter { get; set; }
    [Required]
    public int StartRecordIndex { get; set; } = 0;

    [Required]
    public int NumberOfRecords { get; set; } = 10;
    public string? SearchText { get; set; }
}