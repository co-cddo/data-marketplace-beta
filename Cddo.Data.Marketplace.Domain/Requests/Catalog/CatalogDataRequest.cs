using System.Text.Json.Serialization;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog;

public class CatalogDataRequest
{
    [JsonPropertyName("extendedText")]
    public string? ExtendedText { get; set; }
    [JsonPropertyName("pageNum")]
    public int PageNum { get; set; } = 1;
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;
    [JsonPropertyName("theme")]
    public List<string>? Theme { get; set; }
    [JsonPropertyName("creator")]
    public List<string>? Creator { get; set; }
    [JsonPropertyName("entrytype")]
    public List<string>? EntryType { get; set; }
    [JsonPropertyName("sort")]
    public string? Sort { get; set; }
    [JsonPropertyName("agmToken")] 
    public string? AgmToken { get; set; }
}
