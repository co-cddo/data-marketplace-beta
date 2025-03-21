using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Cddo.Data.Marketplace.UI.Services
{
    public class UserRoleClaimService : IUserRoleClaimService
    {
        private readonly string _userApiUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserRoleClaimService> _logger;
        private readonly IConfiguration _configuration;

        public UserRoleClaimService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserRoleClaimService> logger,
            IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userApiUrl = _configuration.GetSection("ApiSettings:UsersAPI").Value ?? throw new ArgumentNullException(nameof(_userApiUrl));
        }

        private Task<string?> GetTokenAsync()
        {
            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated != true) return Task.FromResult<string?>(null);
            var httpContext = _httpContextAccessor.HttpContext;
            var token = httpContext.Request.Cookies["CO-Datamarketplace"];
            return Task.FromResult(token);
        }

        private const string FlurlHttpExceptionMessage = "Flurl HTTP Exception: {ResponseString}";

        public async Task<UserProfileResponse?> GetUserRoleDetailsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                var email = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value;
                var username = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "name")?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
                {
                    _logger.LogError("Email or Username is missing from the token.");
                    return null;
                }

                var request = new
                {
                    Email = email,
                    UserName = username
                };

                var userProfile = await _userApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments("User", "userinfo")
                    .PostJsonAsync(request, cancellationToken: cancellationToken)
                    .ReceiveJson<UserProfileResponse>();
                userProfile.Token = token;
                return userProfile;
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

        public async Task SetUserEmailNotificationAsync(bool notificationDecision, int userId, CancellationToken cancellationToken = default)
        {
           
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return; 

                var request = new
                {
                    id = userId,
                    set = notificationDecision
                };

                var response = await _userApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments("User", "notifications")
                    .PostJsonAsync(request, cancellationToken: cancellationToken);
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
        }

        public async Task<List<UserRoleApprovalDetailResponse>?> GetUserRoleApprovalListAsync(int userId, CancellationToken cancellationToken = default)
        {
           
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return null;

                var response = await _userApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments("User", "myapprovals", userId)
                    .GetJsonAsync<List<UserRoleApprovalDetailResponse>>(cancellationToken: cancellationToken);

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

        public async Task SetUserRoleApprovalAsync(List<SetUserApprovalRequest> setUserApprovalRequest, CancellationToken cancellationToken = default)
        {
            
            try
            {
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token)) return;

                var response = await _userApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments("User", "ApprovalRequest-multiple")
                    .PostJsonAsync(setUserApprovalRequest, cancellationToken: cancellationToken);
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
        }
    }
}
