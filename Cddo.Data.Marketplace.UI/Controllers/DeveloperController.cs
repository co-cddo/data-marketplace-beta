using Cddo.Data.Marketplace.Api.Dto.Requests.ClientAuth;
using Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.UI.Pages.Developer;
using Cddo.Data.Marketplace.UI.Services;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Extensions.Logging;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("developer")]
public class DeveloperController : Controller
{
    private readonly ILogger<DeveloperController> _logger;
    private readonly IDeveloperService _developerService;
    private readonly IUserRoleService _userRoleService;
    private readonly IAppInsightsLogger _appInsightsLogger;
    private readonly IUserProfilePresenter _users;
    private readonly string apiLandingPageLink = "~/Pages/APIPortal/APILandingPage.cshtml";
    private readonly string developerApi = "DeveloperAPI";
    private readonly string update = "Update";
    public const string ViewApiCredential = "~/Pages/Developer/ViewApiCredential.cshtml";
    public const string CreateCredential = "~/Pages/Developer/CreateApiCredential.cshtml";

    public DeveloperController(ILogger<DeveloperController> logger,
            IDeveloperService developerService, IUserRoleService userRoleService, IAppInsightsLogger appInsightsLogger, IUserProfilePresenter users)
    {
        _logger = logger;
        _developerService = developerService;
        _userRoleService = userRoleService;
        _appInsightsLogger = appInsightsLogger;
        _users = users;
    }

    [Route("api-keys")]
    public async Task<IActionResult> ApiCredentials(CancellationToken cancellationToken = default)
    {
        if (!User.Identity!.IsAuthenticated)
        {
            return Challenge(); 
        }

        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        try
        {
            var credentials = await _developerService.GetClientAuthCredentialsAsync(cancellationToken);

            return View("~/Pages/Developer/ApiCredentials.cshtml", credentials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching API keys.");
            return View("~/Pages/Developer/ApiCredentials.cshtml", null);
        }
    }

    [Route("api-keys/{id}")]
    public async Task<IActionResult> GetApiCredentialById(string id, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        try
        {
            var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

            if (credential == null)
            {
                _logger.LogWarning("API credential with ID {Id} not found.", id);
                return View(ViewApiCredential, null);
            }

            return View(ViewApiCredential, credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching the API credential with ID {Id}.", id);
            return View(ViewApiCredential, null);
        }
    }

    [Route("create-api-keys")]
    public async Task<IActionResult> GotoCreateApiCredential(CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        return View(CreateCredential);
    }

    [HttpPost]
    [Route("create-api-key")]
    public async Task<IActionResult> CreateApiCredential(ClientAuthCredentialsRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        if (request.ScopeList != null && request.ScopeList.Any())
        {
            request.Scope = string.Join(", ", request.ScopeList);
        }
        if (!ModelState.IsValid)
        {
            return View(CreateCredential, request);
        }

        var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

        try
        {
            var response = await _developerService.CreateClientAuthCredentialAsync(request, cancellationToken);

            if (response == null)
            {
                _logger.LogError("Failed to create API credential.");
                ModelState.AddModelError("", "An error occurred while creating the API credential. Please try again.");
                _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.Error, developerApi, "CDDO", "Create", developerApi, "Failed to create API credential.", userEventProperties);
                return View(CreateCredential, request);
            }

            //Log start Api call
            _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.Create, developerApi, "CDDO", "Create", developerApi, response.AppName, userEventProperties);

            return View("~/Pages/Developer/StoreApiCredentials.cshtml", response);
        }
        catch (Exception ex)
        {
            _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.Error, developerApi, "CDDO", "Create", developerApi, ex.Message, userEventProperties);
            _logger.LogError(ex, "An unexpected error occurred while creating API credential.");
            ModelState.AddModelError("", "An unexpected error occurred. Please try again.");

            return View(CreateCredential, request);
        }
    }

    [Route("api-confirmation")]
    public async Task<IActionResult> ApiCredentialConfirmation(CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        return View("~/Pages/Developer/StoreApiCredentials.cshtml");
    }

    [Route("revoke-credentials")]
    public async Task<IActionResult> GotoRevokeCredentials(string id, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

        return View("~/Pages/Developer/RevokeCredentials.cshtml", credential);
    }

    [HttpPost("revoke-credentials")]
    public async Task<IActionResult> RevokeCredentials(string id, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        var result = await _developerService.DeleteClientAuthCredentialByIdAsync(id, cancellationToken);

        var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

        if (result)
        {
            _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.Revoked, developerApi, "CDDO", "Revoke", developerApi, result.ToString(), userEventProperties);
        }
        return RedirectToAction(nameof(DeveloperController.ApiCredentials));
    }

    [Route("update-api-name")]
    public async Task<IActionResult> GotoUpdateApiName(string id, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

        return View("~/Pages/Developer/EditCredentialName.cshtml", credential);
    }

    [HttpPost("update-api-name")]
    public async Task<IActionResult> UpdateApiName(string id, string appName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            ModelState.Clear();
            ModelState.AddModelError("AppName", "Enter the API name");            

            return View("~/Pages/Developer/EditCredentialName.cshtml", new ClientAuthCredentialsResponse());
        }

        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }

        var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

        var request = new ClientAuthCredentialsRequest
        {
            AppName = appName,
            Scopes = credential.Scopes,
            Environment = credential.Environment,
            Domain = credential.Domain,
            OrganisationID = credential.OrganisationId,
            UserId = credential.UserId,
            Expiration = credential.Expiration,
            ClientSecret = credential.ClientSecret,
            ClientId = credential.ClientId,
            Status = credential.Status
        };


        await _developerService.UpdateClientAuthCredentialByIdAsync(id, request, cancellationToken);

        var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

        var updatedCredential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);


        _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.UpdateName, developerApi, "CDDO", update, developerApi, updatedCredential.AppName, userEventProperties);

        ViewBag.IsSuccess = true;


        return View(ViewApiCredential, updatedCredential);
    }


    [Route("update-api-scope")]
    public async Task<IActionResult> GotoUpdateApiScope(string id, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

        return View("~/Pages/Developer/EditCredentialScope.cshtml", credential);
    }

    [HttpPost("update-api-scope/{id}")]
    public async Task<IActionResult> UpdateApiScope(string id, List<string> scopes, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }

        var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

        if (scopes == null || !scopes.Any())
        {
            ModelState.AddModelError("Scopes", "You must select at least one scope.");
            return View("~/Pages/Developer/EditCredentialScope.cshtml", credential);
        }

        var scopesAsString = string.Join(",", scopes);
        var request = new ClientAuthCredentialsRequest
        {
            AppName = credential.AppName,
            Scopes = scopesAsString,
            Environment = credential.Environment,
            Domain = credential.Domain,
            OrganisationID = credential.OrganisationId,
            UserId = credential.UserId,
            Expiration = credential.Expiration,
            ClientId = credential.ClientId,
            ClientSecret = credential.ClientSecret,
            Status = credential.Status

        };

        await _developerService.UpdateClientAuthCredentialByIdAsync(id, request, cancellationToken);
        var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

        _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.UpdateScope, developerApi, "CDDO", update, developerApi, scopesAsString, userEventProperties
        );

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.UpdateScope, developerApi, "CDDO", update, developerApi, scopesAsString, new Dictionary<string, string>
                {{ "ValidationErrors", string.Join(", ", validationErrors) },{ "id", id }}
            );
        }

        ViewBag.IsSuccess = true;
        var updatedCredential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

        return View(ViewApiCredential, updatedCredential);
    }


    [HttpGet("edit-api-expiry/{id}")]
    public async Task<IActionResult> GotoUpdateApiExpiry(string id, CancellationToken cancellationToken = default)
    {
        if (!await _userRoleService.IsUserRolePublisher() && !await _userRoleService.IsUserRoleAdmin())
        {
            return View(apiLandingPageLink);
        }
        var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);
        if (credential == null)
        {
            return NotFound("Credential not found.");
        }

        return View("~/Pages/Developer/EditCredentialExpiry.cshtml", credential);
    }

    [HttpPost("update-api-expiry/{id}")]
    public async Task<IActionResult> UpdateApiExpiry(
    string id, string expiryDay, string expiryMonth, string expiryYear, CancellationToken cancellationToken = default)
    {
        if (ModelState.ContainsKey("expiryDay"))
        {
            ModelState["expiryDay"].Errors.Clear();
        }
        if (ModelState.ContainsKey("expiryMonth"))
        {
            ModelState["expiryMonth"].Errors.Clear();
        }
        if (ModelState.ContainsKey("expiryYear"))
        {
            ModelState["expiryYear"].Errors.Clear();
        }

        bool dayValid = int.TryParse(expiryDay, out int day) && day >= 1 && day <= 31;
        bool monthValid = int.TryParse(expiryMonth, out int month) && month >= 1 && month <= 12;
        bool yearValid = int.TryParse(expiryYear, out int year) && year >= 1900 && year <= 9999;

        if (!dayValid)
        {
            ModelState.AddModelError("expiryDay", "Please enter a valid day.");
        }
        if (!monthValid)
        {
            ModelState.AddModelError("expiryMonth", "Please enter a valid month.");
        }
        if (!yearValid)
        {
            ModelState.AddModelError("expiryYear", "Please enter a valid year (e.g., 2025).");
        }

        if (!dayValid || !monthValid || !yearValid)
        {
            var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);
            return View("~/Pages/Developer/EditCredentialExpiry.cshtml", credential);
        }

        var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

        try
        {
            DateTime newExpiry = new DateTime(year, month, day);
            var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);
            if (newExpiry < DateTime.UtcNow)
            {
                ModelState.AddModelError("expiryDate", "The expiry date cannot be in the past");
            }
            else if (newExpiry > DateTime.UtcNow.AddMonths(12))
            {
                ModelState.AddModelError("expiryDate", "The expiration date cannot be more than 12 months in the future");
            }

            if (!ModelState.IsValid)
            {

                return View("~/Pages/Developer/EditCredentialExpiry.cshtml", credential);
            }

            var request = new ClientAuthCredentialsRequest
            {
                AppName = credential.AppName,
                Environment = credential.Environment,
                Domain = credential.Domain,
                OrganisationID = credential.OrganisationId,
                UserId = credential.UserId,
                Expiration = newExpiry,
                ClientId = credential.ClientId,
                ClientSecret = credential.ClientSecret,
                Scopes = credential.Scopes,
                Status = credential.Status
            };

            await _developerService.UpdateClientAuthCredentialByIdAsync(id, request, cancellationToken);

            var updatedCredential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);

            _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.UpdateExpiry, developerApi, "CDDO", update, developerApi, request.Expiration.ToString(), userEventProperties);

            ViewBag.IsSuccess = true;

            return View(ViewApiCredential, updatedCredential);
        }
        catch
        {
            ModelState.AddModelError("expiryDate", "Invalid date. Please ensure the day, month, and year form a valid date.");
            var credential = await _developerService.GetClientAuthCredentialByIdAsync(id, cancellationToken);
            _appInsightsLogger.LogEventMainBase(EventTypes.DeveloperApiEvents.Error, developerApi, "CDDO", update, developerApi, credential.AppName, userEventProperties);
            return View("~/Pages/Developer/EditCredentialExpiry.cshtml", credential);
        }
    }


    private async Task<IUserDetails> DoGetInitiatingUserDetailsAsync()
    {
        var initiatingUserDetails = await _users.GetInitiatingUserDetailsAsync();

        if (initiatingUserDetails == null)
        {
            _logger.LogError("Unable to get user details for initiating user");
        }

        return initiatingUserDetails;
    }
}
