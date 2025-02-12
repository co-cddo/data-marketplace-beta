namespace Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth
{
    public class CreateClientAuthCredentialsResponse
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scopes { get; set; }
        public string Environment { get; set; }
    }
}
