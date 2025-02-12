namespace Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;

public class CatalogDataResponse
{
    public List<DatasetResponse>? DataSets { get; set; }
    public int Count { get; set; }
}
public class CitationIdentifier
{
    public string? Id { get; set; }
}

public class DataFormatModel
{
    public string? DataFormat { get; set; }
    public string? Version { get; set; }
}

public class DataSet2
{
    public string? Id { get; set; }
}

public class EntitlementsByIdentity
{
    public List<string>? PUBLIC { get; set; }
}

public class GeospatialExtent
{
    public double NorthBoundLatitude { get; set; }
    public double EastBoundLongitude { get; set; }
    public double SouthBoundLatitude { get; set; }
    public double WestBoundLongitude { get; set; }
}

public class Licence
{
    public string? Text { get; set; }
    public string? AttributionStatement { get; set; }
    public string? UseLimitationStatement { get; set; }
    public string? UseConstraints { get; set; }
    public string? Url { get; set; }
}

public class PublicContact
{
    public string? OrganisationName { get; set; }
    public string? Role { get; set; }
    public string? EmailAddress { get; set; }
    public string? Url { get; set; }
    public string? UrlLabel { get; set; }
}

public class Resource
{
    public string? Url { get; set; }
    public string? Name { get; set; }
    public string? Id { get; set; }
    public string? Description { get; set; }
}



public class TaxonomyKeyword
{
    public string? SourceUri { get; set; }
    public string? SourceLabel { get; set; }
    public string? ValueUri { get; set; }
    public string? ValueLabel { get; set; }
}

public class TemporalExtent
{
    public string? Begin { get; set; }
    public string? End { get; set; }
}

