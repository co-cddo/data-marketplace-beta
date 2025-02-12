using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using System.Text.Json.Serialization;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;

public class ManageOrganisationsRequest
{
    [JsonPropertyName("searchTerm")]
    public string? SearchTerm { get; set; }
    [JsonPropertyName("page")]
    public int PageNumber { get; set; } = 1;
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;
    [JsonPropertyName("allowListTrue")]
    public bool AllowListTrue { get; set; }
    [JsonPropertyName("allowListFalse")]
    public bool AllowListFalse { get; set; }
    [JsonPropertyName("organisationType")]
    public List<OrganisationType>? OrganisationType { get; set; }
    [JsonPropertyName("sortBy")]
    public SortBy SortBy { get; set; }
    [JsonPropertyName("sortDirection")]
    public SortDirection SortDirection { get; set; } 
    public Organisations? Organisations { get; private set; }
   
}

public enum SortDirection
{
    Ascending,
    Descending
}

public enum SortBy
{
    Modified,
    OrganisationName
}