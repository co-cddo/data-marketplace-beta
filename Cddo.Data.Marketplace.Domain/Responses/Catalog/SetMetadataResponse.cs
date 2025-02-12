namespace Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;
public class SetMetadataResponse
{
    public string? Status { get; set; }
    public Guid? Id { get; set; }
    public DataSet? DataSet { get; set; }
}
public class DataSet
{
    public string? Id { get; set; }
    public string? PublishedStatus { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<object>? Tags { get; set; }
    public string? Visibility { get; set; }
    public string? Creator { get; set; }
    public string? EntryType { get; set; }
    public long? CreatedAt { get; set; }
    public long? Modified { get; set; }
    public string? Publisher { get; set; }
    public List<object>? Concepts { get; set; }
    public string? CoordinateReferenceSystemId { get; set; }
    public string? SpatialRepresentationType { get; set; }
    public List<object>? Topics { get; set; }
    public LicenceObj? Licence { get; set; }
    public List<object>? TaxonomyKeywords { get; set; }
    public List<CitationIdentifier>? CitationIdentifiers { get; set; }
    public List<object>? DataFormats { get; set; }
    public List<object>? ServesData { get; set; }
    public List<object>? SecurityClassifications { get; set; }
    public string? Language { get; set; }
    public string? CharacterSet { get; set; }
    public string? MetadataStandardName { get; set; }
    public string? MetadataStandardVersion { get; set; }
    public string? AccessRights { get; set; }
    public string? EndpointDescription { get; set; }
    public string? SecurityClassification { get; set; }
    public string? ServiceType { get; set; }
    public string? ServiceStatus { get; set; }
    public List<object>? AlternativeTitles { get; set; }
    public List<object>? Contacts { get; set; }
    public MetadataContact? MetadataContact { get; set; }
    public List<object>? WorkflowKeywords { get; set; }
    public List<object>? Distributions { get; set; }
    public List<object>? Resources { get; set; }
    public List<object>? Services { get; set; }
    public List<string>? Entitlements { get; set; }
    public bool IsOwner { get; set; }
    public List<object>? DerivedFrom { get; set; }
    public List<object>? UsedBy { get; set; }
    public EntitlementsByIdentity? EntitlementsByIdentity { get; set; }
    public string? Type { get; set; }
}
public class MetadataContact
{
    public string? OrganisationName { get; set; }
    public string? Role { get; set; }
    public string? IndividualName { get; set; }
    public string? PositionName { get; set; }
    public string? EmailAddress { get; set; }
}
public class LicenceObj
{
    public string? Text { get; set; }
    public string? Url { get; set; }
    public string? UseLimitationStatement { get; set; }
    public string? AttributionStatement { get; set; }
    public string? UseConstraints { get; set; }
}