using System.ComponentModel.DataAnnotations;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.ClientAuth;

public class ClientAuthCredentialsRequest
{
    public int UserId { get; set; }
    public int OrganisationID { get; set; }
    [Required(ErrorMessage = "Select one or more scopes")]
    public List<string>? ScopeList { get; set; }
    public string Scope { get; set; } = String.Empty;
    [Required(ErrorMessage = "Select an environment")]
    public string Environment { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    [Required(ErrorMessage = "Enter a name")]
    public string AppName { get; set; } = string.Empty;
    public DateTime? Expiration { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
