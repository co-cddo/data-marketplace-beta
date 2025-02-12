using System.ComponentModel.DataAnnotations;

namespace Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

public class CatalogReportFieldFilterCondition
{
    [Required]
    public string Value { get; set; }

    [Required]
    public bool CompareWholeValue { get; set; }
}