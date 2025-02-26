using Agm.Catalog.DotNet.Core.Utilities;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.UI.Pages.DataDescription;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("[controller]")]
[Authorize]
public class DataDescriptionController : Controller
{
    private readonly ICatalogDataService _catalogDataService;
    private readonly ICatalogQuestionsService _catalogQuestionsService;
    private readonly IUserRoleService _userRoleService;
    private readonly IAppInsightsLogger _appInsightlogger;
    private readonly IUserProfilePresenter _userProfilePresenter;
    private readonly IDataShareRequestMailboxAddressValidation _dataShareRequestMailboxAddressValidation;
    private const string ManualViewPath = "~/Pages/DataDescription/NewDescription/Manual/";
    private static readonly string AccessDeniedPage = "/Error/403";

    private static readonly Dictionary<string, string> ViewPaths = new Dictionary<string, string>
        {

            { "DataShareRequestNotificationsSelection", "DataShareRequestNotificationsSelection.cshtml"}
        };

    public DataDescriptionController(ILogger<DataDescriptionController> logger,
        ICatalogDataService catalogDataService,
        ICatalogQuestionsService catalogQuestionsService,
        IUserRoleService userRoleService,
        IEnumMemberConverter enumMemberConverter,
        IAppInsightsLogger appInsightlogger,
        IUserProfilePresenter userProfilePresenter,
        IDataShareRequestMailboxAddressValidation dataShareRequestMailboxAddressValidation)
    {        
        ArgumentNullException.ThrowIfNull(enumMemberConverter, nameof(enumMemberConverter));
        ArgumentNullException.ThrowIfNull(appInsightlogger, nameof(appInsightlogger));

        _catalogDataService = catalogDataService;
        _catalogQuestionsService = catalogQuestionsService;
        _userRoleService = userRoleService;
        _appInsightlogger = appInsightlogger;
        _userProfilePresenter = userProfilePresenter;
        _dataShareRequestMailboxAddressValidation = dataShareRequestMailboxAddressValidation;
    }


    [Route("New/Manual/Dsr-Notifications-Selection")]
    public async Task<IActionResult> DataShareRequestNotificationsSelection(
        string identifier,
        DataShareRequestNotificationRecipientType? selectedRecipientType,
        string? enteredCustomAddress)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for DataShareRequestNotificationsSelection.");
        }

        var userDomainInformation = await _userProfilePresenter.GetDomainInformationOfInitiatingUserAsync();

        var getDataAssetResponse = await _catalogDataService.GetDataAssetAsync(new Guid(identifier));

        var contactPoint = getDataAssetResponse?.CddoDataAsset.DataAssetContacts.FirstOrDefault(x =>
            x.Role == DataAssetContactRoleType.Contact);

        return View(ManualViewPath + ViewPaths["DataShareRequestNotificationsSelection"], new DataShareRequestNotificationsSelectionRequest
        {
            Identifier = identifier,
            SelectedRecipientType = selectedRecipientType,
            EnteredCustomAddress = enteredCustomAddress,
            MaintainerEmailAddress = contactPoint?.Email,
            DomainDsrNotificationMailboxAddress = userDomainInformation!.DataShareRequestMailboxAddress
        });
    }

    [HttpPost("New/Manual/Dsr-Notifications-Selection-Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DataShareRequestNotificationsSelectionSubmit(
        DataShareRequestNotificationsRequest dataShareRequestNotificationsRequest,
        DataAssetType dataAssetType)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for DataShareRequestNotificationsSelectionSubmit.");
        }

        ModelState.Clear();

        if (!ValidateDataShareRequestNotificationsRequest())
        {
            var dataShareRequestNotificationsSelectionPageName = ManualViewPath + ViewPaths["DataShareRequestNotificationsSelection"];
            return View(dataShareRequestNotificationsSelectionPageName, new DataShareRequestNotificationsSelectionRequest
            {
                Identifier = dataShareRequestNotificationsRequest.Identifier,
                SelectedRecipientType = dataShareRequestNotificationsRequest.SelectedDataShareRequestNotificationRecipientType,
                EnteredCustomAddress = dataShareRequestNotificationsRequest.CustomDsrNotificationAddress,
                MaintainerEmailAddress = dataShareRequestNotificationsRequest.MaintainerEmailAddress,
                DomainDsrNotificationMailboxAddress = dataShareRequestNotificationsRequest.DomainDsrNotificationMailboxAddress
            });
        }

        PatchProfiledDataAssetResponse? result;
        try
        {
            result = await _catalogQuestionsService.UpdateDataShareRequestNotificationsSelectionAsync(
                dataShareRequestNotificationsRequest, dataAssetType);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (result == null)
        {
            return RedirectToAction(nameof(DataShareRequestNotificationsSelection), new
            {
                dataAssetId = dataShareRequestNotificationsRequest.Identifier,
                selectedRecipientType = dataShareRequestNotificationsRequest.SelectedDataShareRequestNotificationRecipientType,
                enteredCustomAddress = dataShareRequestNotificationsRequest.CustomDsrNotificationAddress
            });
        }

        return await PublishDataAssetSubmit(new Guid(dataShareRequestNotificationsRequest.Identifier), dataAssetType);

        bool ValidateDataShareRequestNotificationsRequest()
        {
            var recipientType = dataShareRequestNotificationsRequest.SelectedDataShareRequestNotificationRecipientType;
            if (recipientType == null)
            {
                ModelState.AddModelError(nameof(DataShareRequestNotificationRecipient.DataShareRequestNotificationRecipientType),
                    "Select where data share request notifications should be sent to");
                return false;
            }

            if (recipientType == DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress)
            {
                const string customAddressInputName = nameof(DataShareRequestNotificationRecipient.CustomDsrNotificationAddress);

                var customAddress = dataShareRequestNotificationsRequest.CustomDsrNotificationAddress ?? string.Empty;
                if (customAddress.Length > 255)
                {
                    ModelState.AddModelError(customAddressInputName,
                        "Data share request mailbox address is too long.");
                    return false;
                }

                if (!_dataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                        customAddress, out var validationError))
                {
                    ModelState.AddModelError(customAddressInputName,
                        validationError??"");
                    return false;
                }
            }

            return true;
        }
    }

    private async Task<IActionResult> PublishDataAssetSubmit(Guid dataAssetId, DataAssetType dataAssetType)
    {
        const DataAssetStatus dataAssetStatus = DataAssetStatus.Published;

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateDataAssetStatusAsync(dataAssetId.ToString(), dataAssetStatus, dataAssetType);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        if (response != null && User.Identity!.IsAuthenticated)
        {
            var userresponse = await _userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userresponse);
            userEventProperties.Add("dataAssetId", response.DataAssetId.ToString());
            userEventProperties.Add("dataAssetStatus", dataAssetStatus.ToString());
            _appInsightlogger.LogEvent(EventTypes.MetadataEvent.MetadataPublishedStatusChange, userEventProperties);
        }

        return RedirectToAction("CheckAnswers", "CatalogDataDescription", new { Identifier = response!.DataAssetId.ToString() });

    }

    [HttpPost("UpdateDataAssetStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDataAssetStatusSubmit(string identifier, DataAssetStatus dataAssetStatus, DataAssetType dataAssetType)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Pages/DataDescription/ViewDataAssetSummary.cshtml");
        }

        PatchProfiledDataAssetResponse? response;

        try
        {
            response = await _catalogQuestionsService.UpdateDataAssetStatusAsync(identifier, dataAssetStatus, dataAssetType);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        var result = await _catalogDataService.GetDataAssetAsync(response!.DataAssetId);
        if (User.Identity!.IsAuthenticated)
        {
            var userresponse = await _userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userresponse);
            userEventProperties.Add("dataAssetId", identifier);
            userEventProperties.Add("dataAssetStatus", dataAssetStatus.ToString());
            _appInsightlogger.LogEvent(EventTypes.MetadataEvent.MetadataStatusChange, userEventProperties);
        }
        return View("~/Pages/DataDescription/ViewDataAssetSummary.cshtml", result!.CddoDataAsset);
    }


    [Route("Manage-Data-Asset")]
    public async Task<IActionResult> EditDataAssetManagementSettings(Guid dataAssetId)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for EditDataAssetManagementSettings.");
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        return await DoShowEditDataAssetManagementSettings(dataAssetId, null, null);
    }

    private async Task<IActionResult> DoShowEditDataAssetManagementSettings(
        Guid dataAssetId,
        DataShareRequestNotificationRecipientType? selectedDataShareRequestNotificationRecipientType,
        string? enteredCustomDsrNotificationAddress)
    {
        var getDataAssetResponse = await _catalogDataService.GetDataAssetAsync(dataAssetId);
        var cddoDataAsset = getDataAssetResponse!.CddoDataAsset;

        if (selectedDataShareRequestNotificationRecipientType != null)
        {
            cddoDataAsset.DataShareRequestNotificationRecipientType = selectedDataShareRequestNotificationRecipientType;
        }

        if (enteredCustomDsrNotificationAddress != null)
        {
            cddoDataAsset.CustomDsrNotificationAddress = enteredCustomDsrNotificationAddress;
        }

        var esdaDomainInformation = await _userProfilePresenter.GetOrganisationDomainInformationAsync(
            cddoDataAsset.OrganisationId, cddoDataAsset.DomainId);

        var manageDataAssetModel = new EditDataAssetManagementSettingsModel
        {
            CddoDataAsset = cddoDataAsset,
            EsdaDomainInformation = esdaDomainInformation!
        };

        return View("~/Pages/DataDescription/EditDataAssetManagementSettings.cshtml", manageDataAssetModel);
    }

    [HttpPost("Manage-Data-Asset-Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDataAssetManagementSettingsSubmit(DataAssetManagementSettingsRequest dataAssetManagementSettingsRequest)
    {
        if (!ModelState.IsValid)
        {
            _appInsightlogger.LogWarning("Model state is invalid for EditDataAssetManagementSettingsSubmit.");
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        ModelState.Clear();

        if (!ValidateDataShareRequestNotificationsRequest())
        {
            return await DoShowEditDataAssetManagementSettings(
                new Guid(dataAssetManagementSettingsRequest.Identifier),
                dataAssetManagementSettingsRequest.SelectedDataShareRequestNotificationRecipientType,
                dataAssetManagementSettingsRequest.CustomDsrNotificationAddress);
        }

        try
        {
            await UpdateDataShareRequestNotificationsSelectionAsync();
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToPage(AccessDeniedPage);
        }

        var result = await _catalogDataService.GetDataAssetAsync(new Guid(dataAssetManagementSettingsRequest.Identifier));
        return View("~/Pages/DataDescription/ViewDataAssetSummary.cshtml", result!.CddoDataAsset);

        bool ValidateDataShareRequestNotificationsRequest()
        {
            var recipientType = dataAssetManagementSettingsRequest.SelectedDataShareRequestNotificationRecipientType;
            if (recipientType == null)
            {
                ModelState.AddModelError(nameof(DataShareRequestNotificationRecipient.DataShareRequestNotificationRecipientType),
                    "Select where data share request notifications should be sent to");
                return false;
            }

            if (recipientType == DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress)
            {
                const string customAddressInputName = nameof(DataShareRequestNotificationRecipient.CustomDsrNotificationAddress);

                var customAddress = dataAssetManagementSettingsRequest.CustomDsrNotificationAddress ?? string.Empty;
                if (customAddress.Length > 255)
                {
                    ModelState.AddModelError(customAddressInputName,
                        "Data share request mailbox address is too long.");
                    return false;
                }

                if (!_dataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                        customAddress, out var validationError))
                {
                    ModelState.AddModelError(customAddressInputName,
                        validationError??"");
                    return false;
                }
            }

            return true;
        }

        async Task UpdateDataShareRequestNotificationsSelectionAsync()
        {
            var dataShareRequestNotificationsRequest = new DataShareRequestNotificationsRequest
            {
                Identifier = dataAssetManagementSettingsRequest.Identifier,
                SelectedDataShareRequestNotificationRecipientType = dataAssetManagementSettingsRequest.SelectedDataShareRequestNotificationRecipientType,
                CustomDsrNotificationAddress = dataAssetManagementSettingsRequest.CustomDsrNotificationAddress,
                MaintainerEmailAddress = dataAssetManagementSettingsRequest.MaintainerEmailAddress,
                DomainDsrNotificationMailboxAddress = dataAssetManagementSettingsRequest.DomainDsrNotificationMailboxAddress
            };

            await _catalogQuestionsService.UpdateDataShareRequestNotificationsSelectionAsync(
                dataShareRequestNotificationsRequest,
                dataAssetManagementSettingsRequest.DataAssetType);
        }
    }
}
