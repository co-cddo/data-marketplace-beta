using Cddo.Data.Marketplace.Api.Dto.Requests.ClientAuth;
using Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces;

public interface IDeveloperService
{
    Task<ClientAuthCredentialsResponse?> CreateClientAuthCredentialAsync(ClientAuthCredentialsRequest request, CancellationToken cancellationToken = default);
    Task<List<ClientAuthCredentialsResponse>?> GetClientAuthCredentialsAsync(CancellationToken cancellationToken = default);
    Task<ClientAuthCredentialsResponse?> GetClientAuthCredentialByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> DeleteClientAuthCredentialByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ClientAuthCredentialsResponse?> UpdateClientAuthCredentialByIdAsync(string id, ClientAuthCredentialsRequest updateRequest, CancellationToken cancellationToken = default);
}
