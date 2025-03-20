using Cddo.Data.Marketplace.Api.Dto.Requests.RequestAccess;
using Cddo.Data.Marketplace.Api.Dto.Responses.RequestAccess;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl.Http;

namespace Cddo.Data.Marketplace.UI.Services
{
    public class RequestAccessService: IRequestAccessService
    {
        private readonly string _usersApiUrl;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RequestAccessService> _logger;

      
        private const string OrganisationsPath = "Organisations";
        private const string RequestPath = "Request";
        private const string FlurlHttpExceptionMessage = "Flurl HTTP Exception: {ResponseString}";

        public RequestAccessService(
        ILogger<RequestAccessService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _usersApiUrl = _configuration.GetSection("ApiSettings:UsersAPI").Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        private Task<string?> GetTokenAsync()
        {
            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true)
                return Task.FromResult<string?>(null);
            var httpContext = _httpContextAccessor.HttpContext;
            string? idToken = httpContext.Request.Cookies["CO-Datamarketplace"];
            return Task.FromResult(idToken);
        }


        public async Task<int?> SubmitOrganisationRequestAsync(CreateOrganisationRequest organisationAccessRequest, CancellationToken cancellationToken = default)
        {
            
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;

                var response = await _usersApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments(OrganisationsPath, RequestPath)
                    .PostJsonAsync(organisationAccessRequest, cancellationToken: cancellationToken)
                    .ReceiveJson<int>();

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }

        private async Task<int?> CreateOrganisationAsync(OrganisationAccessResponse organisationAccessResponse, string token, CancellationToken cancellationToken = default)
        {

            var request = new
            {
                organisationAccessResponse.OrganisationName,
                organisationAccessResponse.OrganisationType,
                Domains = new[]
        {
                new
                {
                    organisationAccessResponse.DomainName,
                    organisationAccessResponse.OrganisationType,
                    organisationAccessResponse.OrganisationFormat,
                    AllowList = true,
                }
            }
            };

            var response = await _usersApiUrl
                .WithOAuthBearerToken(token)
                .AppendPathSegments(OrganisationsPath)
                .PostJsonAsync(request, cancellationToken: cancellationToken)
                .ReceiveJson<int?>();

            return response;

        }

        public async Task<int?> UpdateOrganisationRequestAsync(OrganisationAccessResponse organisationAccessRequest, CancellationToken cancellationToken = default)
        {
            
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;

                int? organisationId = null;
                if (organisationAccessRequest.Status == "Approved")
                {
                    organisationId = await CreateOrganisationAsync(organisationAccessRequest, token);
                    if (!organisationId.HasValue) 
                        return null;
                }

                organisationId = await _usersApiUrl
                   .WithOAuthBearerToken(token)
                   .AppendPathSegments(OrganisationsPath, RequestPath, organisationAccessRequest.OrganisationRequestID)
                   .PatchJsonAsync(organisationAccessRequest, cancellationToken: cancellationToken)
                   .ReceiveJson<int>();

                return organisationId;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return null;
        }

        public async Task<List<OrganisationAccessResponse>?> GetOrganisationAllRequestAsync(CancellationToken cancellationToken = default)
        {
           
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;

                var response = await _usersApiUrl
                    .WithOAuthBearerToken(token)
                .AppendPathSegments(OrganisationsPath, RequestPath, "All")
                    .GetJsonAsync<List<OrganisationAccessResponse>>(cancellationToken: cancellationToken);

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }

        public async Task<OrganisationAccessResponse?> GetOrganisationRequestByIdAsync(int? organisationRequestID, CancellationToken cancellationToken = default)
        {
            
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;

                var response = await _usersApiUrl
                    .WithOAuthBearerToken(token)
                .AppendPathSegments(OrganisationsPath, RequestPath, organisationRequestID)
                    .GetJsonAsync<OrganisationAccessResponse>(cancellationToken: cancellationToken);

                return response;
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.GetResponseStringAsync();
                _logger.LogError(ex, FlurlHttpExceptionMessage, responseString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }
    }
}
