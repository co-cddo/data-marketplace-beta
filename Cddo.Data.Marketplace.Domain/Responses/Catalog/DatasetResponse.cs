using System.Text.Json.Serialization;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;

public class DatasetResponse
{
    public string? Id { get; set; }
    public string? DraftStatus { get; set; }
    public string? PublishedStatus { get; set; }
    public string? Title { get; set; }
    public List<string>? AlternativeTitles { get; set; }
    public string? Description { get; set; }
    public string? Summary { get; set; }
    public string? Identifier { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Themes { get; set; }
    public List<string>? WorkflowKeywords { get; set; }
    public string? Visibility { get; set; }
    public string? Creator { get; set; }
    public string EntryType { get; set; }
    public string Exchange { get; set; } = "cddo";
    public string? License { get; set; }
    public string? AccrualPeriodicity { get; set; }
    public long? CreatedAt { get; set; }
    public string? ServiceType { get; set; }
    public long? MetadataModified { get; set; }
    public object? Published { get; set; }
    public string? Publisher { get; set; }
    public List<object>? Concepts { get; set; }
    public GeospatialExtent? GeospatialExtent { get; set; }
    public string? CoordinateReferenceSystemId { get; set; }
    public string? Lineage { get; set; }
    public List<string>? Topics { get; set; }
    public Licence? Licence { get; set; }
    public List<TaxonomyKeyword>? TaxonomyKeywords { get; set; }
    public List<CitationIdentifier>? CitationIdentifiers { get; set; }
    public List<DataFormatModel>? DataFormats { get; set; }
    public string? Language { get; set; }
    public string? CharacterSet { get; set; }
    public string? HierarchyLevel { get; set; }
    public string? MetadataLanguage { get; set; }
    public string? MetadataCharacterSet { get; set; }
    public string? MetadataStandardName { get; set; }
    public string? MetadataStandardVersion { get; set; }
    public string? AccessRights { get; set; }
    public TemporalExtent? TemporalExtent { get; set; }
    public PublicContact? PublicContact { get; set; }
    public List<object>? Distributions { get; set; }
    public List<Resource>? Resources { get; set; }
    public List<object>? Services { get; set; }
    public List<string>? Entitlements { get; set; }
    public List<string>? SecurityClassifications { get; set; }
    public bool? IsOwner { get; set; }
    public List<object>? DerivedFrom { get; set; }
    public List<object>? UsedBy { get; set; }
    public EntitlementsByIdentity? EntitlementsByIdentity { get; set; }
    public long? Modified { get; set; }
    public string? SpatialRepresentationType { get; set; }
    public string? DraftNotes { get; set; }
}

