using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.UI.Pages.DataDescription.NewDescription.Upload;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers;
[Authorize]
public class CatalogSpreadsheetController : Controller
{
    private readonly ICatalogSpreadsheetService _catalogSpreadsheetService;
    private readonly IUserRoleService _userRoleService;
    private readonly IUserProfilePresenter _userProfilePresenter;
    private readonly IAppInsightsLogger _appInsightlogger;
    private const string UploadSpreadsheetPath = "~/Pages/DataDescription/NewDescription/Upload/UploadSpreadsheet.cshtml";
    public CatalogSpreadsheetController(
        ICatalogSpreadsheetService catalogSpreadsheetService,
        IUserRoleService userRoleService,
        IUserProfilePresenter userProfilePresenter,
        IAppInsightsLogger appInsightlogger)
    {
        _catalogSpreadsheetService = catalogSpreadsheetService;
        _userRoleService = userRoleService;
        _userProfilePresenter = userProfilePresenter;
        _appInsightlogger = appInsightlogger;
    }

    [HttpGet("DownloadSpreadsheetTemplate")]
    public async Task<IActionResult?> DownloadSpreadsheetTemplate()
    {
        if (User.Identity!.IsAuthenticated)
        {
            var content = await _catalogSpreadsheetService.DownloadSpreadsheetTemplateAsync();
            var userresponse = await _userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userresponse);
            _appInsightlogger.LogAdminEventBase(EventTypes.AdminAuditEvent.AdminDownloadTemplate, "UploadFile", "CDDO", "AddFile", "AssertUpload", "", userEventProperties);
            return File(content, "application/octet-stream", "Template For Data Descriptions.xlsx");
        }
        return null;
    }    

    [Route("New/UploadSpreadsheet")]
    public IActionResult AddNewUploadSpreadsheet()
    {
        return View(UploadSpreadsheetPath);
    }

    [HttpPost("New/Upload")]
    public async Task<IActionResult> AddNewUploadSubmit(IFormFile fileUpload)
    {
        if (!ModelState.IsValid)
        {
            return View(UploadSpreadsheetPath);
        }

        if (fileUpload == null || fileUpload.Length == 0)
        {
            ModelState.Clear();
            ModelState.AddModelError("fileUpload", "Select a file to upload");
           
            return View(UploadSpreadsheetPath);
        }

        if (User.Identity!.IsAuthenticated)
        {
            var result = await _catalogSpreadsheetService.UploadSpreadsheetAsync(fileUpload);
            var userresponse = await _userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userresponse);
            _appInsightlogger.LogAdminEventBase(EventTypes.AdminAuditEvent.AdminUploadCsv, "UploadSpreadsheet", "CDDO", "AddFile", "AssertUpload", "", userEventProperties);
            if (result != null)
            {
                return RedirectToAction(nameof(GetValidatedDataAssetsSpreadsheet));
            }
        }

        ModelState.AddModelError("fileUpload", "Your file failed to upload");
        return View(UploadSpreadsheetPath);
    }

    [Route("Upload/SpreadsheetDataAssets")]
    public async Task<IActionResult> GetValidatedDataAssetsSpreadsheet()
    {
        var getValidatedDataAssetsSpreadsheetResponse = await
            _catalogSpreadsheetService.GetValidatedDataAssetsSpreadsheetAsync();

        var checkForPotentialDuplicatesInValidatedSpreadsheetContentResponse = await
            _catalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync();

        var spreadsheetDataAssetsModel = new SpreadsheetDataAssetsModel
        {
            ValidationSummary = getValidatedDataAssetsSpreadsheetResponse!.ProfiledDataAssetsSpreadsheetValidationSummary,
            PotentialDuplicatesToSpreadsheetContentItems = checkForPotentialDuplicatesInValidatedSpreadsheetContentResponse!.PotentialDuplicatesToSpreadsheetContent
        };

        return View("~/Pages/DataDescription/NewDescription/Upload/SpreadsheetDataAssets.cshtml", spreadsheetDataAssetsModel);
    }

    [Route("Upload/SpreadsheetDataAsset")]
    public async Task<IActionResult> GetValidatedDataAssetSpreadsheet(string recordId)
    {
        var validatedDataAssetSpreadsheetItemSummary = await
            _catalogSpreadsheetService.GetValidatedDataAssetSpreadsheetItemAsync(recordId);

        var checkForPotentialDuplicatesInValidatedSpreadsheetItemResponse = await
            _catalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(recordId);

        return View("~/Pages/DataDescription/NewDescription/Upload/SpreadsheetDataAssetSummary.cshtml", new SpreadsheetDataAssetSummaryModel
        {
            ItemSummary = validatedDataAssetSpreadsheetItemSummary,
            PotentialDuplicatesToSpreadsheetItem = checkForPotentialDuplicatesInValidatedSpreadsheetItemResponse!.PotentialDuplicatesToSpreadsheetItem
        });
    }

    [Route("Upload/DataShareRequestNotificationsSelection")]
    public async Task<IActionResult> DataShareRequestNotificationsSelection()
    {
        return await ShowDataShareRequestNotificationsSelectionOptionsAsync();
    }

    private async Task<IActionResult> ShowDataShareRequestNotificationsSelectionOptionsAsync(
        DataShareRequestNotificationRecipientType? selectedRecipientType = null,
        string? enteredCustomAddress = null)
    {
        var userDomainInformation = await _userProfilePresenter.GetDomainInformationOfInitiatingUserAsync();

        var dataShareRequestNotificationsSelectionModel = new DataShareRequestNotificationsSelectionModel
        {
            SelectedRecipientType = selectedRecipientType,
            EnteredCustomAddress = enteredCustomAddress,
            UserDomainInformation = userDomainInformation!
        };

        return View("~/Pages/DataDescription/NewDescription/Upload/DataShareRequestNotificationsSelection.cshtml",
            dataShareRequestNotificationsSelectionModel);
    }

    [HttpPost("Upload/PublishSpreadsheetDataAssets")]
    public async Task<IActionResult> PublishSpreadsheetDataAssets(
        IFormCollection formData)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for RequestTasksListQuestions.");
        }

        var result = await _catalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(formData);

        ModelState.Clear();

        var dataShareRequestNotificationAddressValidationResult = result.DataShareRequestNotificationAddressValidationResult;
        if (!dataShareRequestNotificationAddressValidationResult.RequestWasValid)
        {
            foreach (var validationError in dataShareRequestNotificationAddressValidationResult.ValidationErrors)
            {
                ModelState.AddModelError(validationError.Key, validationError.Value);
            }
            
            return await ShowDataShareRequestNotificationsSelectionOptionsAsync(
                selectedRecipientType: dataShareRequestNotificationAddressValidationResult.SelectedRecipientType,
                enteredCustomAddress: dataShareRequestNotificationAddressValidationResult.EnteredCustomAddress);
        }

        var response = result.Response;
        if (response == null || !response.Success)
        {
            if (response?.Errors != null)
            {
                foreach (var error in response.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            return RedirectToAction(nameof(GetValidatedDataAssetsSpreadsheet));
        }

        return View("~/Pages/DataDescription/NewDescription/Upload/UploadConfirmation.cshtml");
    }

    [Route("Upload/ClearSpreadsheetDataAssets")]
    public async Task<IActionResult> ClearSpreadsheetDataAssets()
    {
        await _catalogSpreadsheetService.ClearSpreadsheetDataAssets();

        return RedirectToAction(nameof(AddNewUploadSpreadsheet));
    }

    public IFormFile ConvertByteArrayToFormFile(byte[] fileBytes, string fileName, string contentType = null)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for ConvertByteArrayToFormFile.");
        }

        // Create a memory stream from the byte array
        var stream = new MemoryStream(fileBytes);

        // Create a FormFile object
        IFormFile formFile = new FormFile(stream, 0, fileBytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType ?? "application/octet-stream"
        };

        return formFile;
    }
}
