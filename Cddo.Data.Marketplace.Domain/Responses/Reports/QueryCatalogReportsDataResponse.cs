using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.Reports;

public class QueryCatalogReportsDataResponse
{
    public int TotalNumberOfMatchedRecords { get; set; }
    public List<CatalogAssetField> SelectableFields { get; set; } = new List<CatalogAssetField>() 
    { CatalogAssetField.AccessRights,
      CatalogAssetField.DataAssetStatus,
      CatalogAssetField.DataAssetType,
      CatalogAssetField.Modified,
      CatalogAssetField.Themes,
      CatalogAssetField.Title,
      CatalogAssetField.Publisher,
      CatalogAssetField.Description,
      CatalogAssetField.SecurityClassification

    };

    public List<CatalogAssetField>? SelectedFields { get; set; }

    public List<DataAssetStatus>? SelectedStatuses { get; set; }

    public List<string>? Organisations { get; set; }
    public List<string>? SelectableOrganisations { get; set; }
    public string? ReportName { get; set; }
    public Guid? TemplateId { get; set; }

    public List<CatalogReportsDataItem> CatalogReportsDataItems { get; set; } = [];
}
