namespace Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;

public class CatalogDataResponseBase
{
    public string? Status { get; set; }
    public Guid? Id { get; set; }
    public DataSetObj? DataSet { get; set; }
}

public class DataSetObj
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? EntryType { get; set; }
    public string? Exchange { get; set; }
}