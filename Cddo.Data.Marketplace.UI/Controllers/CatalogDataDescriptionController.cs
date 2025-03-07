using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Model.Enum;
using Cddo.Data.Marketplace.UI.Pages.DataDescription.NewDescription.Manual;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.Audit;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using Agm.Catalog.DotNet.Core.Validation.EmailAddress;
using Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Linq;
using System.Data;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("publish")]
[Authorize]
public class CatalogDataDescriptionController(
    ICatalogDataService catalogDataService,
    ICatalogQuestionsService catalogQuestionsService,
    IUserRoleService userRoleService,
    AppInsightsLogger insightsLogger,
    ICddoEmailAddressValidation emailValidator)
    : Controller
{
    private readonly ICatalogDataService _catalogDataService = catalogDataService ?? throw new ArgumentNullException(nameof(catalogDataService));
    private readonly ICatalogQuestionsService _catalogQuestionsService = catalogQuestionsService ?? throw new ArgumentNullException(nameof(catalogQuestionsService));
    private readonly IUserRoleService _userRoleService = userRoleService ?? throw new ArgumentNullException(nameof(userRoleService));
    private readonly AppInsightsLogger _insightsLogger = insightsLogger ?? throw new ArgumentNullException(nameof(insightsLogger));
    private readonly ICddoEmailAddressValidation _emailValidator = emailValidator;

    private static readonly string AccessDeniedPage = "/Error/403";
    private const string LogValidationErrors = "validationErrors";
    private const string TitleViewPath = "~/Pages/DataDescription/NewDescription/Manual/Title.cshtml";
    private const string DescriptionViewPath = "~/Pages/DataDescription/NewDescription/Manual/Description.cshtml";
    private const string KeywordsViewPath = "~/Pages/DataDescription/NewDescription/Manual/Keywords.cshtml";
    private const string DataOwnerViewPath = "~/Pages/DataDescription/NewDescription/Manual/DataOwner.cshtml";
    private const string PublishedDateViewPath = "~/Pages/DataDescription/NewDescription/Manual/PublishedDate.cshtml";
    private const string ContactPointViewPath = "~/Pages/DataDescription/NewDescription/Manual/ContactPoint.cshtml";

    // Centralised roles definition
    private static readonly List<string> RequiredRoles =
    [
        "AGM Administrator",
        "Organisation Administrator",
        "Metadata Publisher"
    ];

    private async Task<bool> UserHasRequiredRoleAsync()
    {
        return User.Identity?.IsAuthenticated == true && await _userRoleService.IsUserInRoleAsync(RequiredRoles);
    }
    private ViewResult ViewOrRedirect(string viewPath, object? model = null) => View(viewPath, model);

    private async Task<IActionResult> SecureActionAsync(string viewPath, object? model = null)
    {
        return await UserHasRequiredRoleAsync() ? ViewOrRedirect(viewPath, model) : RedirectToPage("/Error/NoPermissions", new { requiredPermission = "publisher" });
    }

    private async Task LogUserActionAsync(EventTypes.AdminAuditEvent eventType, string pageName, string summary)
    {
        var userProfile = await _userRoleService.GetUserProfileAsync();
        var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userProfile);
        _insightsLogger.LogAdminEvent(eventType, pageName, "CDDO", "", summary, "", userEventProperties);
    }

    [Route("Dashboard")]
    public async Task<IActionResult> DataDescriptionDashboard()
    {
        var userProfile = await _userRoleService.GetUserProfileAsync();
        ViewBag.OrganisationName = userProfile?.Organisation?.OrganisationName;
        return await SecureActionAsync("~/Pages/DataDescription/DataDescriptionDashboard.cshtml");
    }

    [Route("choose-method")]
    public Task<IActionResult> AddNewDataDescription() =>
        SecureActionAsync("~/Pages/DataDescription/AddNewDataDescription.cshtml");

    [HttpPost("choose-method")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDataDescriptionMethodSubmit(NewDataDescriptionMethod? method)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }

        if (!await UserHasRequiredRoleAsync()) return RedirectToPage("/Index");

        if (method is not null)
            return method switch
            {
                NewDataDescriptionMethod.Manual => RedirectToAction(nameof(DataDescriptionType)),
                NewDataDescriptionMethod.API => RedirectToAction(nameof(DataDescriptionApiStart)),
                NewDataDescriptionMethod.Spreadsheet => RedirectToAction(
                    nameof(CatalogSpreadsheetController.AddNewUploadSpreadsheet), "CatalogSpreadsheet"),
                _ => ViewOrRedirect("~/Pages/DataDescription/AddNewDataDescription.cshtml")
            };
        ModelState.AddModelError("dataDescriptionMethod", "Select how you want to add your data description");
        return ViewOrRedirect("~/Pages/DataDescription/AddNewDataDescription.cshtml");

    }

    [Route("api/start")]
    public Task<IActionResult> DataDescriptionApiStart() =>
        SecureActionAsync("~/Pages/DataDescription/NewDescription/Api/Start.cshtml");

    [Route("Add-Data-Description-Type")]
    public Task<IActionResult> DataDescriptionType() =>
        SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/DataDescriptionType.cshtml");

    [HttpPost("Add-Data-Description-Type")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DataDescriptionTypeSubmit(bool confirmDataDescription)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }
        if (!await UserHasRequiredRoleAsync()) return RedirectToPage("/Index");

        if (confirmDataDescription)
            return RedirectToAction(nameof(SecurityClassification));

        ModelState.AddModelError("confirmDataDescription", "Confirm that you're describing a data set");
        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/DataDescriptionType.cshtml");
    }

    [Route("Add-Security-Classification")]
    public async Task<IActionResult> SecurityClassification(QuestionSecurityClassificationRequest questionSecurityClassificationRequest, string? identifier, bool isCheckList, bool isCheckAnswers, bool isEditMode)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionSecurityClassificationRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null && Enum.TryParse<SecurityClassificationEnum>(dataAsset.CddoDataAsset.SecurityClassification!, out var securityClassification))
            {
                questionSecurityClassificationRequest.SecurityClassification = securityClassification;
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/SecurityClassification.cshtml", questionSecurityClassificationRequest);
    }

    [HttpPost("Add-Security-Classification")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SecurityClassificationSubmit(QuestionSecurityClassificationRequest questionSecurityClassificationRequest, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        if (questionSecurityClassificationRequest.SecurityClassification is null)
        {
            ModelState.Clear();
            ModelState.AddModelError("SecurityClassification", "Confirm that the security classification is data set");
            ViewBag.isEditMode = isEditMode;
            return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/SecurityClassification.cshtml", questionSecurityClassificationRequest);
        }

        if (string.IsNullOrEmpty(questionSecurityClassificationRequest.Identifier))
        {
            return RedirectToAction(nameof(AddTitle), new { securityClassification = questionSecurityClassificationRequest.SecurityClassification });
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateSecurityClassificationAsync(questionSecurityClassificationRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddTitle), isEditMode)
                   ?? RedirectToAction(nameof(AddTitle), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/SecurityClassification.cshtml", questionSecurityClassificationRequest);
    }

    [Route("Add-Title")]
    public async Task<IActionResult> AddTitle(QuestionTitleRequest questionTitleRequest, string? identifier, bool isCheckList, bool isCheckAnswers, bool isEditMode, string? securityClassification = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.isCheckList = isCheckList;
            ViewBag.isCheckAnswers = isCheckAnswers;
            ViewBag.isEditMode = isEditMode;
            if (securityClassification is not null) ViewBag.securityClassification = securityClassification;

            return View(TitleViewPath, questionTitleRequest);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionTitleRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                questionTitleRequest.Title = dataAsset.CddoDataAsset.Title;
            }
        }

        ModelState.Clear();
        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        if (securityClassification is not null) ViewBag.securityClassification = securityClassification;

        return await SecureActionAsync(TitleViewPath, questionTitleRequest);
    }

    [HttpPost("Add-Title")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTitleSubmit(QuestionTitleRequest questionTitleRequest, string? isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode, SecurityClassificationEnum? securityClassification = null)
    {

        if (string.IsNullOrWhiteSpace(questionTitleRequest.Title) && !ModelState.IsValid)
        {
            ModelState.Clear();
            ModelState.AddModelError("Title", "Enter the title of the data set");
            ViewBag.isEditMode = isEditMode;
            if (securityClassification is not null) ViewBag.securityClassification = securityClassification;
            return ViewOrRedirect(TitleViewPath, questionTitleRequest);
        }

        Guid? dataAssetId = null;


        if (string.IsNullOrEmpty(questionTitleRequest.Identifier))
        {
            var userProfile = await _userRoleService.GetUserProfileAsync();

            var response = await _catalogQuestionsService.CreateProfiledDataAssetTitleAsync(
                new QuestionFirstCreationRequest
                {
                    Identifier = questionTitleRequest.Identifier,
                    Title = questionTitleRequest.Title,
                    SecurityClassification = securityClassification,
                    Publisher = userProfile.Organisation?.OrganisationName
                },
                DataAssetType.DataSet);

            dataAssetId = response?.DataAssetId;
        }
        else
        {
            try
            {
                var response = await _catalogQuestionsService.UpdateTitleAsync(questionTitleRequest, DataAssetType.DataSet);

                dataAssetId = response?.DataAssetId;
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage(AccessDeniedPage);
            }
        }

        if (dataAssetId is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewTitle, "Add-Title", $"Updated the title of data set {dataAssetId}");
            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, dataAssetId.ToString(), nameof(AddDescription), isEditMode)
                           ?? RedirectToAction(nameof(AddDescription), new { identifier = dataAssetId.ToString() });
        }

        ModelState.Clear();
        ModelState.AddModelError("Title", "Failed to create the data set");
        return ViewOrRedirect(TitleViewPath, questionTitleRequest);
    }

    [Route("Add-Description")]
    public async Task<IActionResult> AddDescription(QuestionDescriptionRequest questionDescriptionRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Description. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewDescription, "Add-Description", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionDescriptionRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                questionDescriptionRequest.Description = dataAsset.CddoDataAsset.Description!;
                ViewBag.DataAssetTitle = dataAsset.CddoDataAsset.Title;
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        
        return await SecureActionAsync(DescriptionViewPath, questionDescriptionRequest);
    }

    [HttpPost("Add-Description")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDescriptionSubmit(QuestionDescriptionRequest questionDescriptionRequest, string isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            string summary = $"Validation failed for Add-Description. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewDescription, "Add-Description", summary);
        }

        PatchProfiledDataAssetResponse? response;
        try
        {
            response = await _catalogQuestionsService.UpdateDescriptionAsync(questionDescriptionRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewDescription, "Add-Description", $"Updated the description of data set {response.DataAssetId}");
            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddInternalIdentifier), isEditMode)
                   ?? RedirectToAction(nameof(AddInternalIdentifier), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect(DescriptionViewPath, questionDescriptionRequest);
    }

    [Route("Add-Internal-Identifier")]
    public async Task<IActionResult> AddInternalIdentifier(QuestionSupplierIdentifierRequest questionSupplierIdentifierRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Internal-Identifier. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewIdentifier, "Add-Internal-Identifier", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionSupplierIdentifierRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                questionSupplierIdentifierRequest.SupplierIdentifier = dataAsset.CddoDataAsset.InternalIdentifier!;
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;

        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/Identifier.cshtml", questionSupplierIdentifierRequest);
    }

    [HttpPost("Add-Internal-Identifier")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddInternalIdentifierSubmit(QuestionSupplierIdentifierRequest questionSupplierIdentifierRequest, string isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            if (validationErrors.Count() > 0)
            {
                _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataEdited, new Dictionary<string, string>
            {
                { LogValidationErrors, string.Join(", ", validationErrors) },
                { "identifier", questionSupplierIdentifierRequest.Identifier }
            });
            }
        }

        PatchProfiledDataAssetResponse? response;
        try
        {
            response = await _catalogQuestionsService.UpdateIdentifierAsync(questionSupplierIdentifierRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewIdentifier, "Add-Supplier-Identifier", $"Updated the supplier identifier of data set {response.DataAssetId}");
            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddThemes), isEditMode)
                   ?? RedirectToAction(nameof(AddThemes), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/Identifier.cshtml", questionSupplierIdentifierRequest);
    }

    public static class EnumHelper
    {
        // Method to get the enum value based on the EnumMember attribute
        public static ThemeEnum GetEnumValueFromString(string value)
        {
            foreach (var field in typeof(ThemeEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<EnumMemberAttribute>();
                if (attribute != null && attribute.Value == value)
                {
                    return (ThemeEnum)field.GetValue(null);
                }
            }

            throw new ArgumentException($"No matching enum value for {value}");
        }
    }

    [Route("Add-Themes")]
    public async Task<IActionResult> AddThemes(QuestionThemeRequest questionThemeRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Themes. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewIdentifier, "Add-Themes", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionThemeRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null && dataAsset.CddoDataAsset.Themes.Any())
            {
                questionThemeRequest.Theme = dataAsset.CddoDataAsset.Themes.ToList();
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;

        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/Themes.cshtml", questionThemeRequest);
    }

    [HttpPost("Add-Themes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddThemesSubmit(QuestionThemeRequest questionThemeRequest, string isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            if (validationErrors.Count > 0)
            {
                var themeAsString = questionThemeRequest.Theme != null ? string.Join(", ", questionThemeRequest.Theme) : string.Empty;

                _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataEdited, new Dictionary<string, string>
                    {
                        { LogValidationErrors, string.Join(", ", validationErrors) },
                        { "Theme", themeAsString }
                    });
            }
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateThemesAsync(questionThemeRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewThemes, "Add-Themes", $"Updated the themes of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddKeywords), isEditMode)
                   ?? RedirectToAction(nameof(AddKeywords), new { response.DataAssetId });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/Themes.cshtml", questionThemeRequest);
    }

    [Route("Add-Keywords")]
    public async Task<IActionResult> AddKeywords(QuestionKeywordRequest questionKeywordRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Keywords. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewKeywords, "Add-Keywords", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionKeywordRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                questionKeywordRequest.Keyword = dataAsset.CddoDataAsset.Keywords;
            }
        }

        ModelState.Clear();
        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync(KeywordsViewPath, questionKeywordRequest);
    }

    [HttpPost("Add-Keywords")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddKeywordsSubmit(QuestionKeywordRequest questionKeywordRequest, string isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode)
    {
        questionKeywordRequest.Keyword = NormalizeKeywords(questionKeywordRequest.Keyword);

        if (questionKeywordRequest.Keyword != null)
        {
            var invalidKeywords = ValidateKeywords(questionKeywordRequest.Keyword);
            if (invalidKeywords.Any())
            {
                return HandleInvalidKeywords(questionKeywordRequest, invalidKeywords, isEditMode);
            }
        }

        var updateRequest = CreateUpdateRequest(questionKeywordRequest);

        try
        {
            var response = await _catalogQuestionsService.UpdateKeywordsAsync(updateRequest, DataAssetType.DataSet);
            if (response != null)
            {
                await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewKeywords, "Add-Keywords", $"Updated the keywords of data set {response.DataAssetId}");
                return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddContactPoint), isEditMode)
                       ?? RedirectToAction(nameof(AddContactPoint), new { response.DataAssetId });
            }
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, questionKeywordRequest.Identifier, nameof(AddContactPoint), isEditMode)
               ?? ViewOrRedirect(KeywordsViewPath, questionKeywordRequest);
    }
    private static List<string> NormalizeKeywords(List<string> keywords)
    {
        if (keywords != null && keywords.All(string.IsNullOrWhiteSpace))
        {
            return new List<string>();
        }
        return keywords ?? new List<string>();
    }

    private static List<string> ValidateKeywords(List<string> keywords)
    {
        var regex = new global::System.Text.RegularExpressions.Regex(
            @"^[a-zA-Z0-9 _-]+$",
            RegexOptions.None,
            TimeSpan.FromMilliseconds(500)
        );

        return keywords
            .Where(k => string.IsNullOrWhiteSpace(k) ||
                        k.Length < 2 ||
                        k.Length > 100 ||
                        !regex.IsMatch(k))
            .ToList();
    }

    private IActionResult HandleInvalidKeywords(QuestionKeywordRequest questionKeywordRequest, List<string> invalidKeywords, string isEditMode)
    {
        ModelState.Clear();
        ViewBag.isEditMode = isEditMode;

        for (int i = 0; i < questionKeywordRequest.Keyword.Count; i++)
        {
            if (invalidKeywords.Contains(questionKeywordRequest.Keyword[i]))
            {
                ModelState.AddModelError($"Keyword_{i}", "Keywords can only contain alphanumeric characters, spaces, hyphens, and underscores, and must be at least two characters, and less than 100 characters long");
            }
        }

        return ViewOrRedirect(KeywordsViewPath, questionKeywordRequest);
    }

    private QuestionKeywordRequest CreateUpdateRequest(QuestionKeywordRequest questionKeywordRequest)
    {
        return new QuestionKeywordRequest
        {
            Identifier = questionKeywordRequest.Identifier,
            Keyword = questionKeywordRequest.Keyword ?? []
        };
    }

    [Route("Add-Contact-Point")]
    public async Task<IActionResult> AddContactPoint(QuestionContactPointRequest questionContactPointRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Contact-Point. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewContactPoint, "Add-Contact-Point", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionContactPointRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset?.CddoDataAsset is not null)
            {
                questionContactPointRequest.ContactPoint ??= [];
                foreach (var contact in dataAsset.CddoDataAsset.DataAssetContacts)
                {
                    questionContactPointRequest.ContactPoint.Add(new Contact()
                    {
                        Name = contact.Name,
                        Email = contact.Email,
                        Role = (ContactRoleEnum)contact.Role!
                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;

        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/ContactPoint.cshtml", questionContactPointRequest);
    }

    [HttpPost("Add-Contact-Point")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddContactPointSubmit(Contact contact, string? identifier, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        QuestionContactPointRequest questionContactPoint = new() { Identifier = identifier, ContactPoint = new List<Contact> { contact } };

        if (!string.IsNullOrEmpty(contact.Email) &&  !_emailValidator.IsEmailAddressValid(contact.Email))
        {
            ModelState.AddModelError(nameof(contact.Email), "Email is invalid");     
        }
        if (string.IsNullOrEmpty(contact.Name) && !string.IsNullOrEmpty(contact.Email))
        {
            ModelState.AddModelError(nameof(contact.Name), "Please enter a name to continue");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.isEditMode = isEditMode;

            insightsLogger.LogWarning("Model state is invalid for AddContactPointSubmit.");
            return ViewOrRedirect(ContactPointViewPath, questionContactPoint);
        }

        var questionContactPointRequest = CreateQuestionContactPointRequest(contact, identifier);

        if (questionContactPointRequest.ContactPoint != null && questionContactPointRequest.ContactPoint.Any())
        {
            var response = await UpdateContactPointAsync(questionContactPointRequest);

            if (response is not null)
            {
                await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewContactPoint, "Add-Contact-Point", $"Updated the contact point of data set {response.DataAssetId}");

                return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddDataOwner), isEditMode)
                       ?? RedirectToAction(nameof(AddDataOwner), new { identifier });
            }
        }

        return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, questionContactPointRequest.Identifier,
                   nameof(AddDataOwner), isEditMode)
               ?? ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/ContactPoint.cshtml", questionContactPointRequest);
    }

    private QuestionContactPointRequest CreateQuestionContactPointRequest(Contact contact, string? identifier)
    {
        var request = new QuestionContactPointRequest
        {
            Identifier = identifier
        };

        if (!string.IsNullOrEmpty(contact.Name) && !string.IsNullOrEmpty(contact.Email))
        {
            AddContactToRequest(request, contact);
        }

        return request;
    }

    private void AddContactToRequest(QuestionContactPointRequest request, Contact contact)
    {
        if (contact.Role == ContactRoleEnum.Contact)
        {
            request.ContactPoint ??= new List<Contact>();
            request.ContactPoint.Add(new Contact()
            {
                Name = contact.Name,
                Email = contact.Email,
                Role = ContactRoleEnum.Contact!
            });
        }

        AddOwnerContactToRequest(request);
    }

    private void AddOwnerContactToRequest(QuestionContactPointRequest request)
    {
        var dataAsset = _catalogDataService.GetDataAssetAsync(new Guid(request.Identifier)).Result;
        if (dataAsset?.CddoDataAsset?.DataAssetContacts.Any(x => x.Role == DataAssetContactRoleType.Owner) == true)
        {
            var contactRole = dataAsset.CddoDataAsset.DataAssetContacts.FirstOrDefault(x => x.Role == DataAssetContactRoleType.Owner);
            if (contactRole != null)
            {
                request.ContactPoint.Add(new Contact()
                {
                    Name = contactRole.Name,
                    Email = contactRole.Email,
                    Role = (ContactRoleEnum)contactRole.Role!
                });
            }
        }
    }

    private async Task<PatchProfiledDataAssetResponse?> UpdateContactPointAsync(QuestionContactPointRequest request)
    {
        try
        {
            return await _catalogQuestionsService.UpdateContactPointAsync(request, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    [Route("Add-Data-Owner")]
    public async Task<IActionResult> AddDataOwner(QuestionContactPointRequest questionContactPointRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Data-Owner. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewContactPoint, "Add-Data-Owner", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionContactPointRequest.Identifier = identifier;

            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset?.CddoDataAsset is not null)
            {
                questionContactPointRequest.ContactPoint ??= [];
                foreach (var contact in dataAsset.CddoDataAsset.DataAssetContacts)
                {
                    questionContactPointRequest.ContactPoint.Add(new Contact()
                    {
                        Name = contact.Name,
                        Email = contact.Email,
                        Role = (ContactRoleEnum)contact.Role!
                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;

        return await SecureActionAsync(DataOwnerViewPath, questionContactPointRequest);
    }

    [HttpPost("Add-Data-Owner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDataOwnerSubmit(Contact contact, string? identifier, string isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            if (validationErrors.Count() > 0)
            {
                _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataEdited, new Dictionary<string, string>
                {
                    { LogValidationErrors, string.Join(", ", validationErrors) },
                    { "contact", contact.Email }
                });
            }
        }

        var questionContactPointRequest = new QuestionContactPointRequest
        {
            Identifier = identifier
        };

        var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
        if (dataAsset?.CddoDataAsset is not null)
        {
            questionContactPointRequest.ContactPoint ??= new List<Contact>();

            var contactList = dataAsset.CddoDataAsset.DataAssetContacts;

            if (contact.Role == ContactRoleEnum.Owner && !string.IsNullOrEmpty(contact.Name) && !string.IsNullOrEmpty(contact.Email))
            {
                questionContactPointRequest.ContactPoint.Add(new Contact()
                {
                    Name = contact.Name,
                    Email = contact.Email,
                    Role = ContactRoleEnum.Owner!
                });
            }


            if (contactList.Any(x => x.Role == DataAssetContactRoleType.Contact))
            {
                var contactRole = contactList.FirstOrDefault(x => x.Role == DataAssetContactRoleType.Contact);
                if (contactRole != null)
                {
                    questionContactPointRequest.ContactPoint.Add(new Contact()
                    {
                        Name = contactRole.Name,
                        Email = contactRole.Email,
                        Role = (ContactRoleEnum)contactRole.Role!
                    });
                }
            }

            var response = await _catalogQuestionsService.UpdateContactPointAsync(questionContactPointRequest, DataAssetType.DataSet);

            if (response is not null)
            {
                await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewContactPoint, "Add-Data-OwnerName", $"Updated the data owner of data set {response.DataAssetId}");

                return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddPublishedDate), isEditMode)
                       ?? RedirectToAction(nameof(AddPublishedDate), new { identifier });
            }

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, questionContactPointRequest.Identifier, nameof(AddPublishedDate), isEditMode)
                   ?? ViewOrRedirect(DataOwnerViewPath, questionContactPointRequest);
        }

        ModelState.AddModelError("", "The data owner must have both a name and email.");
        return ViewOrRedirect(DataOwnerViewPath, contact);
    }

    [Route("Add-Published-Date")]
    public async Task<IActionResult> AddPublishedDate(QuestionIssuedRequestModel questionIssuedRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Published-Date. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewIssued, "Add-Published-Date", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionIssuedRequest.Identifier = identifier;

            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset!.CddoDataAsset.Issued.HasValue)
            {
                questionIssuedRequest.metadataIssuedDay = dataAsset.CddoDataAsset.Issued.Value.Day.ToString();
                questionIssuedRequest.metadataIssuedMonth = dataAsset.CddoDataAsset.Issued.Value.Month.ToString();
                questionIssuedRequest.metadataIssuedYear = dataAsset.CddoDataAsset.Issued.Value.Year.ToString();
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        ModelState.Clear();
        return await SecureActionAsync(PublishedDateViewPath, questionIssuedRequest);
    }

    [HttpPost("Add-Published-Date")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPublishedDateSubmit(QuestionIssuedRequestModel questionIssuedRequestModel, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        if (IsDateEmpty(questionIssuedRequestModel) && !ModelState.IsValid)
        {
            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, questionIssuedRequestModel.Identifier, nameof(AddFrequency), isEditMode)
                   ?? RedirectToAction(nameof(AddFrequency), new { questionIssuedRequestModel.Identifier });
        }

        var dateString = $"{questionIssuedRequestModel.metadataIssuedMonth}/{questionIssuedRequestModel.metadataIssuedDay}/{questionIssuedRequestModel.metadataIssuedYear}";
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            QuestionIssuedRequest questionIssuedRequest = new()
            {
                metadataIssuedDay = int.Parse(questionIssuedRequestModel.metadataIssuedDay),
                metadataIssuedMonth = int.Parse(questionIssuedRequestModel.metadataIssuedMonth),
                metadataIssuedYear = int.Parse(questionIssuedRequestModel.metadataIssuedYear),
                Identifier = questionIssuedRequestModel.Identifier
            };

            questionIssuedRequest.metadataIssuedDate = date;

            if (date.Date > DateTime.Now.Date)
            {
                ModelState.Clear();
                ModelState.AddModelError(nameof(questionIssuedRequest.metadataIssuedDate), "The date cannot be in the future");
                ViewBag.isEditMode = isEditMode;
                return ViewOrRedirect(PublishedDateViewPath, questionIssuedRequestModel);
            }

            PatchProfiledDataAssetResponse? response;

            try
            {
                response = await _catalogQuestionsService.UpdateIssuedAsync(questionIssuedRequest, DataAssetType.DataSet);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage(AccessDeniedPage);
            }

            if (response is not null)
            {
                await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddNewIssued, "Add-Published-Date", $"Updated the published date of data set {response.DataAssetId}");

                return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddFrequency), isEditMode)
                       ?? RedirectToAction(nameof(AddFrequency), new { identifier = response.DataAssetId.ToString() });
            }
        }

        AddModelErrorsForInvalidDate(questionIssuedRequestModel);
        ViewBag.isEditMode = isEditMode;
        return ViewOrRedirect(PublishedDateViewPath, questionIssuedRequestModel);
    }

    private static bool IsDateEmpty(QuestionIssuedRequestModel questionIssuedRequest)
    {
        if (questionIssuedRequest.metadataIssuedDay.IsNullOrEmpty() || questionIssuedRequest.metadataIssuedMonth.IsNullOrEmpty() || questionIssuedRequest.metadataIssuedYear.IsNullOrEmpty()) return false;
        return true;
    }

    private void AddModelErrorsForInvalidDate(QuestionIssuedRequestModel questionIssuedRequest)
    {
        ModelState.Clear();

        ModelState.AddModelError("metadataIssuedDate", "Provide a valid date");
        

        if (!questionIssuedRequest.metadataIssuedDay.IsNullOrEmpty() && !int.TryParse(questionIssuedRequest.metadataIssuedDay, out int dayResult))
        {
            ModelState.AddModelError(nameof(questionIssuedRequest.metadataIssuedDay), "Day is invalid");
        }

        if (!questionIssuedRequest.metadataIssuedMonth.IsNullOrEmpty() && !int.TryParse(questionIssuedRequest.metadataIssuedMonth, out int monthResult ))
        {
            ModelState.AddModelError(nameof(questionIssuedRequest.metadataIssuedMonth), "Month is invalid");
        }

        if (!questionIssuedRequest.metadataIssuedYear.IsNullOrEmpty() && !int.TryParse(questionIssuedRequest.metadataIssuedYear, out int yearResult))
        {
            ModelState.AddModelError(nameof(questionIssuedRequest.metadataIssuedYear), "Year is invalid");
        }
    }

    [Route("Add-Frequency")]
    public async Task<IActionResult> AddFrequency(QuestionUpdateFrequencyRequest questionUpdateFrequencyRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Frequency. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddFrequency, "Frequency", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionUpdateFrequencyRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));

            if (dataAsset is not null)
            {
                questionUpdateFrequencyRequest.UpdateFrequency = dataAsset.CddoDataAsset.UpdateFrequencyString;
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/UpdateFrequency.cshtml", questionUpdateFrequencyRequest);
    }

    [HttpPost("Add-Frequency")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFrequencySubmit(QuestionUpdateFrequencyRequest questionUpdateFrequencyRequest, string? otherFrequency, string isCheckList, string isCheckAnswers, string showNextQuestion, string isEditMode)
    {
        if (questionUpdateFrequencyRequest.UpdateFrequency == "Other" && !string.IsNullOrEmpty(otherFrequency))
        {
            questionUpdateFrequencyRequest.UpdateFrequency = otherFrequency;
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            if (validationErrors.Count() > 0)
            {
                _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataEdited, new Dictionary<string, string>
        {
            { LogValidationErrors, string.Join(", ", validationErrors) },
            { "Frequency", questionUpdateFrequencyRequest.UpdateFrequency }
        });
            }
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateUpdateFrequencyAsync(questionUpdateFrequencyRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddFrequency, "Add-Frequency", $"Updated the update frequency of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddSupplyFormat), isEditMode)
                   ?? RedirectToAction(nameof(AddSupplyFormat), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/UpdateFrequency.cshtml", questionUpdateFrequencyRequest);
    }

    [Route("access-rights")]
    public async Task<IActionResult> AccessRights(QuestionAccessRightsRequest accessRightsRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? isEditMode)
    {

        if (!string.IsNullOrEmpty(identifier))
        {
            accessRightsRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));

            if (dataAsset is not null)
            {
                accessRightsRequest.AccessRights = dataAsset.CddoDataAsset.AccessRights;
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/AccessRights.cshtml", accessRightsRequest);
    }

    [HttpPost("access-rights")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AccessRights(QuestionAccessRightsRequest accessRightsRequest, bool? accessRightsSelection, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        if (accessRightsSelection != null)
        {
            accessRightsRequest.AccessRights = accessRightsSelection.Value ? "OPEN" : "RESTRICTED";
        }
       
        var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(accessRightsRequest.Identifier));

        

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            if (validationErrors.Count() > 0)
            {
                _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataEdited, new Dictionary<string, string>
                {
                    { LogValidationErrors, string.Join(", ", validationErrors) },
                    { "AccessRights", accessRightsRequest.AccessRights }
                });
            }
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            if (dataAsset?.CddoDataAsset.AccessRights != accessRightsRequest.AccessRights)
            {
                var questionDistributionRequest = new QuestionDistributionRequest()
                {
                    Identifier = accessRightsRequest.Identifier,
                    Distribution = new List<Distribution>()
                };
                var responseUpdate = await _catalogQuestionsService.UpdateDistributionAsync(questionDistributionRequest, DataAssetType.DataSet);
            }
            response = await _catalogQuestionsService.UpdateAccessRightsAsync(accessRightsRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddFrequency, "Access-Rights", $"Updated the access rights of data set {response.DataAssetId}");
            if(accessRightsSelection != null && accessRightsSelection.Value)
            {
                return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(Licence), isEditMode)
                   ?? RedirectToAction(nameof(Licence), new { identifier = response.DataAssetId.ToString() });
            }else if(showNextQuestion != null && ParseBoolean(showNextQuestion))
            {
                return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/AccessRights.cshtml", accessRightsRequest);
            }

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddRestrictedSupplyFormatSubmit), isEditMode)
                   ?? RedirectToAction(nameof(AddRestrictedSupplyFormatSubmit), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/AccessRights.cshtml", accessRightsRequest);
    }

    [Route("Add-Supply-Format")]
    public async Task<IActionResult> AddSupplyFormat(QuestionDistributionRequest questionKeywordRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionKeywordRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
                {
                    questionKeywordRequest.Distribution?.Add(new Distribution
                    {
                        MediaType = [distribution?.MediaType!],
                        DownloadUrl = distribution?.DownloadUrl,
                        Title = distribution?.Title,
                        Format = distribution?.Format,
                        AccessUrl = distribution?.AccessUrl,

                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/SupplyFormat.cshtml", questionKeywordRequest);
    }

    [HttpPost("Add-Supply-Format")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSupplyFormatSubmit(QuestionDistributionRequest questionDistributionRequest, string? mediaType, string? distribution, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        
        if(distribution != null)
        {
            var distributionList = JsonConvert.DeserializeObject<List<Distribution>>(distribution);
            questionDistributionRequest.Distribution = distributionList;
        }
        if (mediaType != null && !questionDistributionRequest.Distribution.Any())
        {
            questionDistributionRequest.Distribution = new List<Distribution>() { new Distribution() { MediaType = new List<string>() { mediaType } } };
        }
        else if(mediaType != null)
        {
            questionDistributionRequest.Distribution.Add(new Distribution() { MediaType = new List<string>() { mediaType } });
        }
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateDistributionAsync(questionDistributionRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", $"Updated the update supply format of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddDistributionLinks), isEditMode)
                   ?? RedirectToAction(nameof(AddDistributionLinks), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/SupplyFormat.cshtml", questionDistributionRequest);
    }

    [HttpPost("access-method-restricted")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRestrictedSupplyFormatSubmit(QuestionDistributionRequest questionDistributionRequest, List<string>? supplyFormat, string? distribution, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {

        if (distribution != null)
        {
            var distributionList = JsonConvert.DeserializeObject<List<Distribution>>(distribution);
            questionDistributionRequest.Distribution = distributionList;
        }
        if (supplyFormat != null && !questionDistributionRequest.Distribution.Any())
        {
            questionDistributionRequest.Distribution = new List<Distribution>();
            foreach (var mediaType in supplyFormat)
            {
                questionDistributionRequest.Distribution.Add(new Distribution() { MediaType = new List<string>() { mediaType } });
            }

        }
        else if(supplyFormat != null)
        {
           
            var currentMediaTypes = questionDistributionRequest?.Distribution?.Select(x => x?.MediaType?.FirstOrDefault()).ToList();

            var unCommonElements = supplyFormat?.Where(item => !currentMediaTypes.Contains(item)).ToList();
            foreach (var item in unCommonElements)
            {
                questionDistributionRequest?.Distribution?.Add(new Distribution() { MediaType = new List<string>() { item } });
            }
           
        }
        //else if (mediaType != null)
        //{
        //    questionDistributionRequest.Distribution.FirstOrDefault().MediaType = new List<string>() { mediaType };
        //}
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateDistributionAsync(questionDistributionRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", $"Updated the update supply format of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddFormatsSubmit), isEditMode)
                   ?? RedirectToAction(nameof(AddFormatsSubmit), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/SupplyFormat.cshtml", questionDistributionRequest);
    }

    [Route("access-method-restricted")]
    public async Task<IActionResult> AddRestrictedSupplyFormatSubmit(QuestionDistributionRequest questionKeywordRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionKeywordRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {

                 foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
                {
                    questionKeywordRequest.Distribution.Add(new Distribution
                    {
                        MediaType = [distribution?.MediaType!],
                        DownloadUrl = distribution?.DownloadUrl,
                        Title = distribution?.Title,
                        Format = distribution?.Format,
                        AccessUrl = distribution?.AccessUrl,

                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/AccessRightsRestricted.cshtml", questionKeywordRequest);
    }

    [Route("Task-List")]
    public async Task<IActionResult> TaskList(string? identifier, bool isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors)
                                                    .Select(e => e.ErrorMessage)
                                                    .ToList();
            _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataAccessDenied, new Dictionary<string, string>
        {
            { "ValidationErrors", string.Join(", ", validationErrors) },
            { "Identifier", identifier ?? "null" }
        });
        }

        ArgumentNullException.ThrowIfNull(identifier);

        var dataAssetId = new Guid(identifier);

        var getCddoDataAssetResponse = await
            _catalogDataService.GetDataAssetAsync(dataAssetId);

        var checkForPotentialDuplicatesToDataAssetResponse = await
            _catalogDataService.CheckForPotentialDuplicatesToDataAssetAsync(dataAssetId);

        ViewBag.isEditMode = isEditMode;

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/TaskList.cshtml", new TaskListModel
        {
            DataAsset = getCddoDataAssetResponse!.CddoDataAsset,
            PotentialDuplicatesToDataAsset = checkForPotentialDuplicatesToDataAssetResponse!.PotentialDuplicatesToDataAsset
        });
    }

    [Route("Check-Answers")]
    public async Task<IActionResult> CheckAnswers(string? identifier)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors)
                                                    .Select(e => e.ErrorMessage)
                                                    .ToList();
            _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataAccessDenied, new Dictionary<string, string>
        {
            { "ValidationErrors", string.Join(", ", validationErrors) },
            { "Identifier", identifier ?? "null" }
        });
        }

        ArgumentNullException.ThrowIfNull(identifier);

        var dataAssetId = new Guid(identifier);

        var dataAssetResponse = await
            _catalogDataService.GetDataAssetAsync(dataAssetId);

        var validationErrorsResponse = await
            _catalogDataService.GetDataAssetValidationErrorsAsync(dataAssetId);

        var checkForPotentialDuplicatesToDataAssetResponse = await
            _catalogDataService.CheckForPotentialDuplicatesToDataAssetAsync(dataAssetId);

        var model = new CheckAnswersModel
        {
            CddoDataAsset = dataAssetResponse?.CddoDataAsset,
            PropertyValidationErrors = validationErrorsResponse?.PropertyValidationErrors ?? [],
            PotentialDuplicatesToDataAsset = checkForPotentialDuplicatesToDataAssetResponse?.PotentialDuplicatesToDataAsset ?? []
        };

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/CheckAnswers.cshtml", model);
    }

    [Route("licence")]
    public async Task<IActionResult> Licence(QuestionLicenceRequest licenceRequest, string? identifier, string isCheckList, string isCheckAnswers, string isEditMode)
    {
        if (!string.IsNullOrEmpty(identifier))
        {
            licenceRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));

            if (dataAsset is not null)
            {
                licenceRequest.Licence = new Licence() {Text = dataAsset.CddoDataAsset.LicenseTitle, Url = dataAsset.CddoDataAsset.EndpointUrl };
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/Licence.cshtml", licenceRequest);

    }

    [HttpPost("licence")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Licence(QuestionLicenceRequest questionLicenceRequest,string? licence, string? accessRightsSelection, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        //if (accessRightsSelection != null)
        //{
        //    questionLicenceRequest.Licence = accessRightsSelection.Value ? "OPEN" : "RESTRICTED";
        //}

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            if (validationErrors.Count() > 0)
            {
                _insightsLogger.LogEvent(EventTypes.MetadataEvent.MetadataEdited, new Dictionary<string, string>
                {
                    { LogValidationErrors, string.Join(", ", validationErrors) },
                    { "Licence", questionLicenceRequest.Licence.Text }
                });
            }
        }

        PatchProfiledDataAssetResponse? response = new PatchProfiledDataAssetResponse();

        try
        {
            //response = await _catalogQuestionsService.UpdateLicenceAsync(questionLicenceRequest, DataAssetType.DataSet);
            //TODO: Stub the licence update
            response.DataAssetId = new Guid(questionLicenceRequest.Identifier);

        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddFrequency, "licence", $"Updated the licence of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddSupplyFormat), isEditMode)
                   ?? RedirectToAction(nameof(AddSupplyFormat), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/Licence.cshtml", questionLicenceRequest);
    }

    [Route("distribution-links")]
    public async Task<IActionResult> AddDistributionLinks(QuestionDistributionRequest questionKeywordRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionKeywordRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
                {
                    questionKeywordRequest.Distribution?.Add(new Distribution
                    {
                        MediaType = [distribution?.MediaType!],
                        DownloadUrl = distribution?.DownloadUrl,
                        Title = distribution?.Title,
                        Format = distribution?.Format,
                        AccessUrl = distribution?.AccessUrl,

                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/DistributionLink.cshtml", questionKeywordRequest);
    }

    [HttpPost("distribution-links")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDistributionLinks(QuestionDistributionRequest questionDistributionRequest, string? mediaType, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        if (mediaType != null)
        {
            questionDistributionRequest.Distribution.FirstOrDefault().MediaType = new List<string>() { mediaType };
        }
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Distribution-Link. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Distribution-Link", summary);
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateDistributionAsync(questionDistributionRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", $"Updated the update supply format of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddAccessLinksSubmit), isEditMode)
                   ?? RedirectToAction(nameof(AddAccessLinksSubmit), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/AccessLinks.cshtml", questionDistributionRequest);
    }
    [Route("distribution-links-restricted")]
    public async Task<IActionResult> AddDistributionLinksRestricted(QuestionDistributionRequest questionKeywordRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionKeywordRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
                {
                    questionKeywordRequest.Distribution?.Add(new Distribution
                    {
                        MediaType = [distribution?.MediaType!],
                        DownloadUrl = distribution?.DownloadUrl,
                        Title = distribution?.Title,
                        Format = distribution?.Format,
                        AccessUrl = distribution?.AccessUrl,

                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/DistributionApiRestricted.cshtml", questionKeywordRequest);
    }

    [HttpPost("distribution-links-restricted")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDistributionLinksRestricted(QuestionDistributionRequest questionDistributionRequest, string? title, string? accessUrl, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
       if(questionDistributionRequest.Identifier == null)
        {
            return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/DistributionApiRestricted.cshtml", questionDistributionRequest);
        }

        var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(questionDistributionRequest.Identifier));
        if (dataAsset != null)
        {
            foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
            {
                questionDistributionRequest.Distribution?.Add(new Distribution
                {
                    MediaType = [distribution?.MediaType!],
                    DownloadUrl = distribution?.DownloadUrl,
                    Title = distribution?.Title,
                    Format = distribution?.Format,
                    AccessUrl = distribution?.AccessUrl,

                });
            }

            var updateDistribution = questionDistributionRequest?.Distribution?.Where(t => t.MediaType.FirstOrDefault() == "API").FirstOrDefault();

            if (updateDistribution != null && questionDistributionRequest?.Distribution != null)
            {
                var index = questionDistributionRequest?.Distribution?.IndexOf(updateDistribution);
                if (index != null && index >= 0)
                {
                    updateDistribution.Title = title;
                    updateDistribution.AccessUrl = accessUrl;
                    questionDistributionRequest.Distribution[(int)index] = updateDistribution;
                }
            }

            PatchProfiledDataAssetResponse? response;
            try
            {
                response = await _catalogQuestionsService.UpdateDistributionAsync(questionDistributionRequest, DataAssetType.DataSet);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToPage(AccessDeniedPage);
            }

            if (response is not null)
            {
                await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", $"Updated the update supply format of data set {response.DataAssetId}");

                return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, questionDistributionRequest.Identifier, nameof(CheckAnswers), isEditMode)
            ?? RedirectToAction(nameof(CheckAnswers), new { identifier = questionDistributionRequest.Identifier });
            }
        }
        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/DistributionApiRestricted.cshtml", questionDistributionRequest);
    }
    [Route("accesslinks")]
    public async Task<IActionResult> AddAccessLinksSubmit(QuestionDistributionRequest questionFormatsRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionFormatsRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
                {
                    questionFormatsRequest.Distribution?.Add(new Distribution
                    {
                        MediaType = [distribution?.MediaType!],
                        DownloadUrl = distribution?.DownloadUrl,
                        Title = distribution?.Title,
                        Format = distribution?.Format,
                        AccessUrl = distribution?.AccessUrl,

                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/AccessLinks.cshtml", questionFormatsRequest);

    }
    [HttpPost("accesslinks")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAccessLinksSubmit(QuestionDistributionRequest questionFormatsRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {
        return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, questionFormatsRequest.Identifier, nameof(CheckAnswers), isEditMode)
               ?? RedirectToAction(nameof(CheckAnswers), new { identifier = questionFormatsRequest.Identifier });

    }
    [Route("formats")]
    public async Task<IActionResult> AddFormatsSubmit(QuestionDistributionRequest questionFormatsRequest, string? identifier, string? isCheckList, string? isCheckAnswers, string? showNextQuestion,  string? isEditMode)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Supply-Format. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", summary);
        }

        if (!string.IsNullOrEmpty(identifier))
        {
            questionFormatsRequest.Identifier = identifier;
            var dataAsset = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));
            if (dataAsset is not null)
            {
                foreach (var distribution in dataAsset.CddoDataAsset.DataAssetDistribution ?? Enumerable.Empty<CddoDataAssetDistribution>())
                {
                    questionFormatsRequest.Distribution?.Add(new Distribution
                    {
                        MediaType = [distribution?.MediaType!],
                        DownloadUrl = distribution?.DownloadUrl,
                        Title = distribution?.Title,
                        Format = distribution?.Format,
                        AccessUrl = distribution?.AccessUrl,

                    });
                }
            }
        }

        ViewBag.isCheckList = isCheckList;
        ViewBag.isCheckAnswers = isCheckAnswers;
        ViewBag.isEditMode = isEditMode;
        return await SecureActionAsync("~/Pages/DataDescription/NewDescription/Manual/Formats.cshtml", questionFormatsRequest);

    }

    [HttpPost("formats")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFormatsSubmit(QuestionDistributionRequest questionFormatsRequest, string? distribution, List<string>? formats, string? isCheckList, string? isCheckAnswers, string? showNextQuestion, string? isEditMode)
    {

        if (distribution != null)
        {
            var distributionList = JsonConvert.DeserializeObject<List<Distribution>>(distribution);
            questionFormatsRequest.Distribution = distributionList;
        }

        if(formats != null && formats.Any()) 
        {
            questionFormatsRequest.Distribution.Where(f=>f.MediaType.FirstOrDefault() == "File download").FirstOrDefault().Format = String.Join(",", formats);
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

            string summary = $"Validation failed for Add-Distribution-Link. Errors: {string.Join(", ", validationErrors)}";
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Distribution-Link", summary);
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateDistributionAsync(questionFormatsRequest, DataAssetType.DataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response is not null)
        {
            await LogUserActionAsync(EventTypes.AdminAuditEvent.AdminAddSupplyFormat, "Add-Supply-Format", $"Updated the update supply format of data set {response.DataAssetId}");

            return RedirectBasedOnFlags(showNextQuestion, isCheckAnswers, isCheckList, response.DataAssetId.ToString(), nameof(AddDistributionLinksRestricted), isEditMode)
                   ?? RedirectToAction(nameof(AddDistributionLinksRestricted), new { identifier = response.DataAssetId.ToString() });
        }

        return ViewOrRedirect("~/Pages/DataDescription/NewDescription/Manual/Formats.cshtml", questionFormatsRequest);
    }
    private static bool ParseBoolean(string input)
    {
        return !string.IsNullOrEmpty(input) && bool.TryParse(input, out var result) && result;
    }

    private IActionResult? RedirectBasedOnFlags(string showNextQuestion, string isCheckAnswers, string isCheckList, string? identifier,
        string nextQuestionAction, string isEditMode)
    {
        if (ParseBoolean(showNextQuestion))
        {
            return RedirectToAction(nextQuestionAction, new { identifier });
        }
        if (ParseBoolean(isCheckAnswers))
        {
            return RedirectToAction(nameof(CheckAnswers), new { identifier });
        }
        return ParseBoolean(isCheckList) ? RedirectToAction(nameof(TaskList), new { identifier, isEditMode }) : null;
    }
}
