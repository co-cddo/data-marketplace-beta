using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Cddo.Data.Marketplace.Audit;
using static Cddo.Data.Marketplace.Audit.EventTypes;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using System.Configuration;

namespace Cddo.Data.Marketplace.Logic.Services;

public class UserRoleService : IUserRoleService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _usersApi;
    private readonly IAppInsightsLogger _logger; // Ensure this is the correct type

    public UserRoleService(IHttpClientFactory clientFactory,
                           IConfiguration configuration,
                           IAppInsightsLogger logger,
                           IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor, nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        ArgumentNullException.ThrowIfNull(clientFactory, nameof(clientFactory));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _clientFactory = clientFactory;
        _logger = logger; // Make sure logger is of type AppInsightsLogger
        _httpContextAccessor = httpContextAccessor;
        _usersApi = configuration.GetSection("ApiSettings:UsersAPI").Value
                ?? throw new ConfigurationErrorsException("Users API address not present in configuration");
    }

    private const string IdTokenErrorMessage = "GetUserProfileAsync: ID token is not available.";
    private const string RemoveUserErrorMessage = "RemoveUserFromRole: ID token is not available.";
    public async Task<UserProfile> GetUserProfileAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Extract the custom token (JWT) from the cookie (CO-Datamarketplace)
            var jwtToken = httpContext.Request.Cookies["CO-Datamarketplace"];

            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                throw new Exception("GetUserProfileAsync: Custom JWT token is not available in the cookie.");
            }

            // Decode the JWT to extract claims (e.g., email, name)
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = tokenHandler.ReadJwtToken(jwtToken);

            // Extract the email claim from the JWT token
            var emailClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            var nameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;

            if (string.IsNullOrWhiteSpace(emailClaim))
            {
                throw new InvalidOperationException("LoginAsync: Email claim is not available.");
            }

            if (string.IsNullOrWhiteSpace(nameClaim))
            {
                nameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            }

            if (string.IsNullOrWhiteSpace(nameClaim))
            {
                throw new Exception("GetUserProfileAsync: Name claim is not available.");
            }

            // Create the user object to send in the request
            var user = new UserSignInDto
            {
                Email = emailClaim,
                UserName = nameClaim
            };

            // Use the HttpClientFactory to create the HttpClient
            var httpClient = _clientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            httpClient.Timeout = TimeSpan.FromSeconds(50);

            // The Bearer token middleware should automatically add the token, no need to manually add it here
            var response = await httpClient.PostAsJsonAsync($"{_usersApi}User/userinfo", user);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"GetUserProfileAsync: Failed to fetch user profile, HTTP {response.StatusCode}");
            }

            // Read the response and deserialize it into UserProfile
            var userProfile = await response.Content.ReadFromJsonAsync<UserProfile>();
            return userProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUserProfileAsync failed {message}", ex);
            return new UserProfile();
        }
    }

    public async Task<UserProfile> GetUserByIdAsync(string id)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var idToken = httpContext.Request.Cookies["CO-Datamarketplace"];

            if (string.IsNullOrEmpty(idToken))
                throw new ArgumentException(IdTokenErrorMessage);

            var httpClient = _clientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            var response = await httpClient.GetAsync($"{_usersApi}User/UserById?userid=" + id);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"GetUserProfileAsync: Failed to fetch user profile, HTTP {response.StatusCode}");
            }

            var userProfile = await response.Content.ReadFromJsonAsync<UserProfile>();
            return userProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetUserProfileAsync failed {message}", ex);
        }
        return new UserProfile();
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var idToken = httpContext.Request.Cookies["CO-Datamarketplace"];

            if (string.IsNullOrEmpty(idToken))
                throw new Exception("GetAllRolesAsync: ID token is not available.");

            var httpClient = _clientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);
            var response = await httpClient.GetAsync($"{_usersApi}User/AllRoles");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"GetAllRolesAsync: Failed to fetch user profile, HTTP {response.StatusCode}");
            }

            var allRoles = await response.Content.ReadFromJsonAsync<List<Role>>();
            return allRoles;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetAllRolesAsync failed {message}", ex);
        }
        return new List<Role>();
    }

    public async Task<bool> IsUserDomainEnabledAsync()
    {
        var userProfile = await GetUserProfileAsync();

        if (userProfile?.Domain == null)
        {
            return false; // Assumes domain must be explicitly enabled; returns false if not found
        }

        return userProfile.Domain.IsEnabled;
    }

    public async Task<bool> IsUserInRoleAsync(List<string> roles)
    {
        var userProfile = await GetUserProfileAsync();

        if (userProfile?.User == null)
        {
            return false;
        }

        UserProfile user = userProfile;
        if (!user.Domain.IsEnabled)
        {
            return false;
        }
        if (!user.Organisation.IsEnabled)
        {
            return false;
        }

        return userProfile.Roles.Any(userRole => roles.Contains(userRole.RoleName));
    }

    public async Task<UserProfile> AddUserToRoleAsync(string roleId, string userId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var idToken = httpContext.Request.Cookies["CO-Datamarketplace"];

        if (string.IsNullOrEmpty(idToken))
            throw new ArgumentException(IdTokenErrorMessage);

        var httpClient = _clientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

        var url = $"{_usersApi}User/AddUserToRole?roleId={roleId}&userid={userId}";

        HttpContent content = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to add user to role, HTTP {response.StatusCode}");
        }

        UserProfile rsp = await GetUserProfileAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(rsp);
        _logger.LogEventMainBase(AdminAuditEvent.AdminAddUserToRole, "UserManagement", "CDDO", "AddUserToRole", userId, roleId, userEventProperties);
        var userProfile = await GetUserByIdAsync(userId);
        return userProfile;
    }

    public async Task<bool> IsUserRoleAdmin()
    {
        var userProfile = await GetUserProfileAsync();

        if (userProfile?.User == null)
        {
            return false;
        }
        if (!userProfile.Domain.IsEnabled)
        {
            return false;
        }
        if (!userProfile.Organisation.IsEnabled)
        {
            return false;
        }

        return userProfile.Roles.Any(userRole => userRole.RoleName == "Organisation Administrator");
    }

    public async Task<bool> IsUserRoleSystemAdmin()
    {
        var userProfile = await GetUserProfileAsync();

        if (userProfile?.User == null)
        {
            return false;
        }
        if (!userProfile.Domain.IsEnabled)
        {
            return false;
        }
        if (!userProfile.Organisation.IsEnabled)
        {
            return false;
        }

        return userProfile.Roles.Any(userRole => userRole.RoleName == "System Administrator");
    }

    public async Task<bool> IsUserRoleSupplier()
    {
        var userProfile = await GetUserProfileAsync();

        if (userProfile?.User == null)
        {
            return false;
        }
        if (!userProfile.Domain.IsEnabled)
        {
            return false;
        }
        if (!userProfile.Organisation.IsEnabled)
        {
            return false;
        }

        return userProfile.Roles.Any(userRole => userRole.RoleName == "Data Request Approver" || userRole.RoleName == "Metadata Publisher");
    }
    public async Task<bool> IsUserRolePublisher()
    {
        var userProfile = await GetUserProfileAsync();

        if (userProfile?.User == null)
        {
            return false;
        }
        if (!userProfile.Domain.IsEnabled)
        {
            return false;
        }
        if (!userProfile.Organisation.IsEnabled)
        {
            return false;
        }

        return userProfile.Roles.Any(userRole => userRole.RoleName == "Metadata Publisher");
    }

    public async Task<UserProfile> RemoveUserFromRoleAsync(string roleId, string userId)
    {

        var url = $"{_usersApi}User/RemoveUserFromRole?roleId={roleId}&userid={userId}";

        HttpContent content = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        var httpContext = _httpContextAccessor.HttpContext;
        var idToken = httpContext.Request.Cookies["CO-Datamarketplace"];

        if (string.IsNullOrEmpty(idToken))
            throw new ArgumentException(RemoveUserErrorMessage);

        var httpClient = _clientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

        var response = await httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"RemoveUserFromRoleAsync: Failed to remove user from role, HTTP {response.StatusCode}");
        }

        UserProfile rsp = await GetUserProfileAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(rsp);
        _logger.LogEventMainBase(AdminAuditEvent.AdminRemoveUserFromRole, "UserManagement", "CDDO", "RemoveUserFromRole", userId, roleId, userEventProperties);

        var userProfile = await GetUserByIdAsync(userId);
        return userProfile;
    }
}
