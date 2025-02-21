using Cddo.Data.Marketplace.Api.Dto.Requests.ClientAuth;
using Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl.Http;
using System.IdentityModel.Tokens.Jwt;

namespace Cddo.Data.Marketplace.UI.Services
{
    public class DeveloperService : IDeveloperService
    {
        private readonly string _userApiUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRoleClaimService _userRoleClaimService;
        private readonly ILogger<DeveloperService> _logger;
        private readonly IConfiguration _configuration;

        public DeveloperService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeveloperService> logger,
            IUserRoleClaimService userRoleClaimService,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userRoleClaimService = userRoleClaimService ?? throw new ArgumentNullException(nameof(userRoleClaimService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _userApiUrl = _configuration.GetSection("ApiSettings:UsersAPI").Value
                          ?? throw new ArgumentException("API URL not found in configuration.");
        }

        private string? GetToken()
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies["CO-Datamarketplace"];
        }

        private const string ClientAuthPath = "ClientAuth";

        private bool ValidateToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token is missing or user is not authenticated.");
                return false;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                handler.ReadToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Invalid JWT token.");
                return false;
            }

            return true;
        }

        private async Task<T?> HandleFlurlRequestAsync<T>(Func<Task<T>> requestFunc)
        {
            try
            {
                return await requestFunc();
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, "Flurl HTTP Exception occurred while handling the API request. Response: {ResponseString}", responseString);

                throw new InvalidOperationException($"Error during API request. Response: {responseString}", ex);
            }
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "An unexpected error occurred during the API request.");

            //    throw new InvalidOperationException("An unexpected error occurred during the API request.", ex);
            //}
        }

        public async Task<ClientAuthCredentialsResponse?> CreateClientAuthCredentialAsync(ClientAuthCredentialsRequest request, CancellationToken cancellationToken = default)
        {
            var token = GetToken();
            if (!ValidateToken(token)) return null;

            var userProfile = await _userRoleClaimService.GetUserRoleDetailsAsync(cancellationToken);
            if (userProfile == null)
            {
                _logger.LogWarning("Unable to retrieve user profile details.");
                return null;
            }

            request.UserId = userProfile.User.UserId;
            request.Domain = userProfile.Domain.DomainName;
            request.OrganisationID = userProfile.Organisation.OrganisationId;
            request.Expiration = DateTime.UtcNow.AddMonths(6);

            var response = await HandleFlurlRequestAsync(async () =>
            {
                return await _userApiUrl
                    .WithOAuthBearerToken(token!)
                    .AppendPathSegments(ClientAuthPath, "generate-client")
                    .PostJsonAsync(request, cancellationToken: cancellationToken)
                    .ReceiveJson<ClientAuthCredentialsResponse>();
            });

            return response;
        }

        public async Task<List<ClientAuthCredentialsResponse>?> GetClientAuthCredentialsAsync(CancellationToken cancellationToken = default)
        {
            var token = GetToken();
            if (!ValidateToken(token)) return null;

            var credentials = await HandleFlurlRequestAsync(async () =>
                await _userApiUrl
                    .WithOAuthBearerToken(token!)
                    .AppendPathSegments(ClientAuthPath, "credentials")
                    .GetJsonAsync<List<ClientAuthCredentialsResponse>>(cancellationToken: cancellationToken));

            return credentials;
        }

        public async Task<ClientAuthCredentialsResponse?> GetClientAuthCredentialByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var token = GetToken();
            if (!ValidateToken(token)) return null;

            return await HandleFlurlRequestAsync(async () =>
                await _userApiUrl
                    .WithOAuthBearerToken(token!)
                    .AppendPathSegments(ClientAuthPath, "credential", id)
                    .GetJsonAsync<ClientAuthCredentialsResponse>(cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteClientAuthCredentialByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var token = GetToken();
            if (!ValidateToken(token)) return false;

            var result = await HandleFlurlRequestAsync(async () =>
            {
                await _userApiUrl
                   .WithOAuthBearerToken(token!)
                   .AppendPathSegments(ClientAuthPath, "credential", id)
                   .DeleteAsync(cancellationToken: cancellationToken);

                return true;
            });

            return result;
        }

        public async Task<ClientAuthCredentialsResponse?> UpdateClientAuthCredentialByIdAsync(string id, ClientAuthCredentialsRequest updateRequest, CancellationToken cancellationToken = default)
        {
            var token = GetToken();
            if (!ValidateToken(token)) return null;

            var result = await HandleFlurlRequestAsync(async () =>
            {
                var response = await _userApiUrl
                    .WithOAuthBearerToken(token!)
                    .AppendPathSegments(ClientAuthPath, "credential", id)
                    .PutJsonAsync(updateRequest, cancellationToken: cancellationToken)
                    .ReceiveJson<ClientAuthCredentialsResponse>();

                return response;
            });
            return result;
        }
    }
}
