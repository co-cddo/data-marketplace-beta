namespace Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth
{
    public class ClientAuthCredentialsResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OrganisationId { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Scopes { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public DateTime? Expiration { get; set; }
        public string? Status { get; set; }
    }
}
