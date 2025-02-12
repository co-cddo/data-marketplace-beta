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

        public UserRoleClaimService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserRoleClaimService> logger,
            IConfiguration configuration)
        {
            _userApiUrl = configuration.GetSection("ApiSettings:UsersAPI").Value ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
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
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
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

        public async Task<IActionResult?> SetUserEmailNotificationAsync(bool notificationDecision, int userId, CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                var request = new
                {
                    id = userId,
                    set = notificationDecision
                };

                var response = await _userApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments("User", "notifications")
                    .PostJsonAsync(request, cancellationToken: cancellationToken)
                    .ReceiveJson<IActionResult>();

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

        public async Task<List<UserRoleApprovalDetailResponse>?> GetUserRoleApprovalListAsync(int userId, CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
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

        public async Task<IActionResult?> SetUserRoleApprovalAsync(List<SetUserApprovalRequest> setUserApprovalRequest, CancellationToken cancellationToken = default)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;
            try
            {                    
                var response = await _userApiUrl
                    .WithOAuthBearerToken(token)
                    .AppendPathSegments("User", "ApprovalRequest-multiple")
                    .PostJsonAsync(setUserApprovalRequest, cancellationToken: cancellationToken)
                    .ReceiveJson<IActionResult>();
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
