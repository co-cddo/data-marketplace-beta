using System.ComponentModel.DataAnnotations;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Exceptions;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "InteractiveScheme")]
    [ApiController]
    [Route("[controller]")]
    public class DataAssetController(
        ILogger<DataAssetController> logger,
        IDataAssetService dataAssetService,
        IDataAssetResponseFactory dataAssetResponseFactory,
        IAppInsightsLogger appInsightsLogger,
        IUserProfilePresenter userRoleService,
        IUserProfilePresenter userProfilePresenter) : ControllerBase
    {
        [Authorize]
        [HttpPost("add-profiled-data-asset")]
        [ProducesResponseType(typeof(AddProfiledDataAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddProfiledDataAsset(AddProfiledDataAssetRequest addProfiledDataAssetRequest)
        {
            ArgumentNullException.ThrowIfNull(addProfiledDataAssetRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var addProfiledDataAssetResult = await dataAssetService.AddProfiledDataAssetAsync(
                    initiatingUserDetails,
                    addProfiledDataAssetRequest.ProfileId,
                    addProfiledDataAssetRequest.DataAssetType,
                    addProfiledDataAssetRequest.Payload,
                    addProfiledDataAssetRequest.ManagementMetadata,
                    addProfiledDataAssetRequest.ActionSource);

                if (!addProfiledDataAssetResult.Success)
                {
                    logger.LogError("Failed to Add Profiled Data Description in DataAssetsService: {Error}", addProfiledDataAssetResult.Error);
                    return BadRequest(addProfiledDataAssetResult.Error);
                }

                var response = dataAssetResponseFactory.CreateAddProfiledDataAssetResponse(
                    addProfiledDataAssetResult.Data!.DataAssetId);

                var userProfile = await userRoleService.GetInitiatingUserDetailsAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userProfile);
                appInsightsLogger.LogAdminEventBase(EventTypes.AdminAuditEvent.MetadataWebIngestion, "Create Data Asset", "CDDO", "Create", "Data assert add", addProfiledDataAssetResult.Data!.DataAssetId.ToString(), userEventProperties);
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPatch("patch-profiled-data-asset")]
        [ProducesResponseType(typeof(PatchProfiledDataAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PatchProfiledDataAsset(PatchProfiledDataAssetRequest patchProfiledDataAssetRequest)
        {
            ArgumentNullException.ThrowIfNull(patchProfiledDataAssetRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var patchProfiledDataAssetResult = await dataAssetService.PatchProfiledDataAssetAsync(
                    initiatingUserDetails,
                    patchProfiledDataAssetRequest.ProfileId,
                    patchProfiledDataAssetRequest.DataAssetType,
                    patchProfiledDataAssetRequest.Payload,
                    patchProfiledDataAssetRequest.ManagementMetadata,
                    patchProfiledDataAssetRequest.ActionSource);

                if (!patchProfiledDataAssetResult.Success)
                {
                    return BuildFailedResultResponse("Failed to Patch Profiled Data Description in DataAssetsService", patchProfiledDataAssetResult);
                }

                var response = dataAssetResponseFactory.CreatePatchProfiledDataAssetResponse(
                    patchProfiledDataAssetResult.Data!.DataAssetId);

                return Ok(response);
            }
            catch (UnAuthorizedAccessToDataAssetException ex)
            {
                logger.LogError(ex, message: ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-profiled-data-assets")]
        [ProducesResponseType(typeof(GetProfiledDataAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfiledDataAssets([FromQuery] GetProfiledDataAssetsRequest getProfiledDataAssetsRequest)
        {
            ArgumentNullException.ThrowIfNull(getProfiledDataAssetsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getProfiledDataAssetsResult = await dataAssetService.GetProfiledDataAssetsAsync(
                    initiatingUserDetails,
                    getProfiledDataAssetsRequest.ProfileId,
                    getProfiledDataAssetsRequest.DataAssetTypes,
                    getProfiledDataAssetsRequest.OnlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
                    getProfiledDataAssetsRequest.OnlyIncludeRecordsOwnedByOrganisationOfCallingUser,
                    getProfiledDataAssetsRequest.SearchText);

                if (!getProfiledDataAssetsResult.Success)
                {
                    logger.LogError("Failed to Get Profiled Data Descriptions from DataAssetsService: {Error}", getProfiledDataAssetsResult.Error);
                    return BadRequest(getProfiledDataAssetsResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetProfiledDataAssetsResponse(
                    getProfiledDataAssetsResult.Data!.TotalNumberOfMatchingProfiledDataAssets,
                    getProfiledDataAssetsResult.Data.ProfiledDataAssets);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-profiled-data-asset-ids")]
        [ProducesResponseType(typeof(GetProfiledDataAssetIdsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfiledDataAssetIds([FromQuery] GetProfiledDataAssetIdsRequest getProfiledDataAssetIdsRequest)
        {
            ArgumentNullException.ThrowIfNull(getProfiledDataAssetIdsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getProfiledDataAssetIdsResult = await dataAssetService.GetProfiledDataAssetIdsAsync(
                    initiatingUserDetails,
                    getProfiledDataAssetIdsRequest.ProfileId);

                if (!getProfiledDataAssetIdsResult.Success)
                {
                    logger.LogError("Failed to Get Profiled Data Description Ids from DataAssetsService: {Error}", getProfiledDataAssetIdsResult.Error);
                    return BadRequest(getProfiledDataAssetIdsResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetProfiledDataAssetIdsResponse(
                    getProfiledDataAssetIdsResult.Data!.TotalNumberOfMatchingProfiledDataAssetIds,
                    getProfiledDataAssetIdsResult.Data!.ProfiledDataAssetIds);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-profiled-data-asset")]
        [ProducesResponseType(typeof(GetProfiledDataAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfiledDataAsset([FromQuery] GetProfiledDataAssetRequest getProfiledDataAssetRequest)
        {
            ArgumentNullException.ThrowIfNull(getProfiledDataAssetRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getProfiledDataAssetResult = await dataAssetService.GetProfiledDataAssetAsync(
                    initiatingUserDetails,
                    getProfiledDataAssetRequest.ProfileId,
                    getProfiledDataAssetRequest.DataAssetId);

                if (!getProfiledDataAssetResult.Success)
                {
                    return BuildFailedResultResponse("Failed to Get Profiled Data Description from DataAssetsService", getProfiledDataAssetResult);
                }

                var response = dataAssetResponseFactory.CreateGetProfiledDataAssetResponse(
                    getProfiledDataAssetResult.Data!.ProfiledDataAsset);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("delete-profiled-data-asset")]
        [ProducesResponseType(typeof(DeleteProfiledDataAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteProfiledDataAsset([FromQuery] DeleteProfiledDataAssetRequest deleteProfiledDataAssetRequest)
        {
            ArgumentNullException.ThrowIfNull(deleteProfiledDataAssetRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var deleteProfiledDataAssetResult = await dataAssetService.DeleteProfiledDataAssetAsync(
                    initiatingUserDetails,
                    deleteProfiledDataAssetRequest.ProfileId,
                    deleteProfiledDataAssetRequest.DataAssetId);

                if (!deleteProfiledDataAssetResult.Success)
                {
                    return BuildFailedResultResponse("Failed to Delete Profiled Data Description from DataAssetsService", deleteProfiledDataAssetResult);
                }

                var response = dataAssetResponseFactory.CreateDeleteProfiledDataAssetResponse(
                    deleteProfiledDataAssetResult.Data!.DataAssetId);

                return Ok(response);
            }
            catch (UnAuthorizedAccessToDataAssetException ex)
            {
                logger.LogError(ex, message: ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-cddo-data-assets")]
        [ProducesResponseType(typeof(GetCddoDataAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCddoDataAssets([FromQuery] GetCddoDataAssetsRequest getCddoDataAssetsRequest)
        {
            ArgumentNullException.ThrowIfNull(getCddoDataAssetsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getCddoDataAssetsResult = await dataAssetService.GetCddoDataAssetsAsync(
                    initiatingUserDetails,
                    getCddoDataAssetsRequest.OnlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
                    getCddoDataAssetsRequest.OnlyIncludeRecordsOwnedByOrganisationOfCallingUser,
                    getCddoDataAssetsRequest.DataAssetTypes,
                    getCddoDataAssetsRequest.DataAssetStatuses,
                    getCddoDataAssetsRequest.StartRecordIndex,
                    getCddoDataAssetsRequest.NumberOfRecords,
                    getCddoDataAssetsRequest.SortField,
                    getCddoDataAssetsRequest.SortDirection,
                    getCddoDataAssetsRequest.SearchText,
                    getCddoDataAssetsRequest.Creator,
                    getCddoDataAssetsRequest.Themes,
                    getCddoDataAssetsRequest.EntryTypes,
                    getCddoDataAssetsRequest.AccessRights);

                if (!getCddoDataAssetsResult.Success)
                {
                    logger.LogError("Failed to Get Cddo Data Descriptions from DataAssetsService: {Error}", getCddoDataAssetsResult.Error);
                    return BadRequest(getCddoDataAssetsResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetCddoDataAssetsResponse(
                    getCddoDataAssetsResult.Data!.TotalNumberOfMatchingCddoDataAssets,
                    getCddoDataAssetsResult.Data!.CddoDataAssets);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-cddo-data-asset")]
        [ProducesResponseType(typeof(GetCddoDataAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCddoDataAsset([FromQuery] GetCddoDataAssetRequest getSearchAutoCompleteSuggestionsRequest)
        {
            ArgumentNullException.ThrowIfNull(getSearchAutoCompleteSuggestionsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getCddoDataAssetResult = await dataAssetService.GetCddoDataAssetAsync(
                    initiatingUserDetails,
                    getSearchAutoCompleteSuggestionsRequest.DataAssetId);

                if (!getCddoDataAssetResult.Success)
                {
                    return BuildFailedResultResponse("Failed to Get Cddo Data Description from DataAssetsService", getCddoDataAssetResult);
                }

                var response = dataAssetResponseFactory.CreateGetCddoDataAssetResponse(
                    getCddoDataAssetResult.Data!.CddoDataAsset);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-search-suggestions-for-published-data-assets")]
        [ProducesResponseType(typeof(GetSearchSuggestionsForPublishedDataAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSearchSuggestionsForPublishedDataAssets(
            [FromQuery] GetSearchSuggestionsForPublishedDataAssetsRequest getSearchSuggestionsForPublishedDataAssetsRequest)
        {
            ArgumentNullException.ThrowIfNull(getSearchSuggestionsForPublishedDataAssetsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getSearchSuggestionsForPublishedDataAssetsResult = await dataAssetService.GetSearchSuggestionsForPublishedDataAssetsAsync(
                    initiatingUserDetails,
                    getSearchSuggestionsForPublishedDataAssetsRequest.SearchText);

                if (!getSearchSuggestionsForPublishedDataAssetsResult.Success)
                {
                    logger.LogError("Failed to Get Search Suggestions For Published Data Assets from DataAssetsService: {Error}", getSearchSuggestionsForPublishedDataAssetsResult.Error);
                    return BadRequest(getSearchSuggestionsForPublishedDataAssetsResult.Error);
                }

                var getSearchSuggestionsForPublishedDataAssetsResponse = dataAssetResponseFactory.CreateGetSearchSuggestionsForPublishedDataAssetsResponse(
                    getSearchSuggestionsForPublishedDataAssetsResult.Data!.SearchSuggestionsForPublishedDataAssets);

                return Ok(getSearchSuggestionsForPublishedDataAssetsResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-search-suggestions-for-organisation-data-assets")]
        [ProducesResponseType(typeof(GetSearchSuggestionsForOrganisationDataAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSearchSuggestionsForOrganisationDataAssets(
            [FromQuery] GetSearchSuggestionsForOrganisationDataAssetsRequest getSearchSuggestionsForOrganisationDataAssetsRequest)
        {
            ArgumentNullException.ThrowIfNull(getSearchSuggestionsForOrganisationDataAssetsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getSearchSuggestionsForOrganisationDataAssetsResult = await dataAssetService.GetSearchSuggestionsForOrganisationDataAssetsAsync(
                    initiatingUserDetails,
                    getSearchSuggestionsForOrganisationDataAssetsRequest.SearchText);

                if (!getSearchSuggestionsForOrganisationDataAssetsResult.Success)
                {
                    logger.LogError("Failed to Get Search Suggestions For Organisation Data Assets from DataAssetsService{Error}", getSearchSuggestionsForOrganisationDataAssetsResult.Error);
                    return BadRequest(getSearchSuggestionsForOrganisationDataAssetsResult.Error);
                }

                var getSearchSuggestionsForOrganisationDataAssetsResponse = dataAssetResponseFactory.CreateGetSearchSuggestionsForOrganisationDataAssetsResponse(
                    getSearchSuggestionsForOrganisationDataAssetsResult.Data!.SearchSuggestionsForOrganisationDataAssets);

                return Ok(getSearchSuggestionsForOrganisationDataAssetsResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("validate-cddo-data-asset")]
        [ProducesResponseType(typeof(GetCddoDataAssetValidationErrorsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateCddoDataAsset(Guid dataAssetId)
        {
            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getCddoDataAssetValidationPropertyErrorsResult = await dataAssetService.GetCddoDataAssetValidationPropertyErrorsAsync(
                    initiatingUserDetails,
                    dataAssetId);

                if (!getCddoDataAssetValidationPropertyErrorsResult.Success)
                {
                    logger.LogError("Failed to Get Cddo Data Asset Validation Property Errors from DataAssetsService{Error}", getCddoDataAssetValidationPropertyErrorsResult.Error);
                    return BadRequest(getCddoDataAssetValidationPropertyErrorsResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetCddoDataAssetValidationErrorsResponse(
                    getCddoDataAssetValidationPropertyErrorsResult.Data!);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error with your request {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("check-for-potential-duplicates-to-data-asset")]
        [ProducesResponseType(typeof(CheckForPotentialDuplicatesToDataAssetResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckForPotentialDuplicatesToDataAsset(
            [FromQuery] CheckForPotentialDuplicatesToDataAssetRequest checkForPotentialDuplicatesToDataAssetRequest)
        {
            ArgumentNullException.ThrowIfNull(checkForPotentialDuplicatesToDataAssetRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var checkForPotentialDuplicatesToDataAssetResult = await dataAssetService.CheckForPotentialDuplicatesToDataAssetAsync(
                    initiatingUserDetails,
                    checkForPotentialDuplicatesToDataAssetRequest.DataAssetId);

                if (!checkForPotentialDuplicatesToDataAssetResult.Success)
                {
                    logger.LogError("Failed to Check For Potential Duplicates To Data Asset with DataAssetsService{Error}", checkForPotentialDuplicatesToDataAssetResult.Error);
                    return BadRequest(checkForPotentialDuplicatesToDataAssetResult.Error);
                }

                var response = dataAssetResponseFactory.CreateCheckForPotentialDuplicatesToDataAssetResponse(
                    checkForPotentialDuplicatesToDataAssetResult.Data!.PotentialDuplicateDataAssetInformation);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("validate-profiled-data-assets-spreadsheet-content")]
        [ProducesResponseType(typeof(ValidateProfiledDataAssetsSpreadsheetContentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateProfiledDataAssetsSpreadsheetContent(
            [Required] IFormFile file,
            [Required] string dataAssetProfileId)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentException.ThrowIfNullOrWhiteSpace(dataAssetProfileId);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var validateProfiledDataAssetsSpreadsheetContentResult = await dataAssetService.ValidateProfiledDataAssetsSpreadsheetContentAsync(
                    initiatingUserDetails,
                    file,
                    dataAssetProfileId);

                if (!validateProfiledDataAssetsSpreadsheetContentResult.Success)
                {
                    logger.LogError("Failed to Validate Profiled Data Descriptions Spreadsheet Content with DataAssetsService{Error}", validateProfiledDataAssetsSpreadsheetContentResult.Error);
                    return BadRequest(validateProfiledDataAssetsSpreadsheetContentResult.Error);
                }

                var response = dataAssetResponseFactory.CreateValidateProfiledDataAssetsSpreadsheetContentResponse(
                    validateProfiledDataAssetsSpreadsheetContentResult.Data!.Success,
                    validateProfiledDataAssetsSpreadsheetContentResult.Data.Errors,
                    validateProfiledDataAssetsSpreadsheetContentResult.Data.ProfiledDataAssetsSpreadsheetValidationSummary);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-validated-profiled-data-assets-spreadsheet-content")]
        [ProducesResponseType(typeof(GetValidatedProfiledDataAssetsSpreadsheetContentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetValidatedProfiledDataAssetsSpreadsheetContent()
        {
            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getValidatedProfiledDataAssetsSpreadsheetContentResult = await dataAssetService.GetValidatedProfiledDataAssetsSpreadsheetContentAsync(
                    initiatingUserDetails);

                if (!getValidatedProfiledDataAssetsSpreadsheetContentResult.Success)
                {
                    logger.LogError("Failed to Get Validated Profiled Data Assets Spreadsheet Content with DataAssetsService: {Error}", getValidatedProfiledDataAssetsSpreadsheetContentResult.Error);
                    return BadRequest(getValidatedProfiledDataAssetsSpreadsheetContentResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetValidatedProfiledDataAssetsSpreadsheetContentResponse(
                    getValidatedProfiledDataAssetsSpreadsheetContentResult.Data!.SpreadsheetName,
                    getValidatedProfiledDataAssetsSpreadsheetContentResult.Data!.ValidatedProfiledDataAssets);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-validated-profiled-data-assets-spreadsheet-item-content")]
        [ProducesResponseType(typeof(GetValidatedProfiledDataAssetsSpreadsheetItemContentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetValidatedProfiledDataAssetsSpreadsheetItemContent([FromQuery] GetValidatedProfiledDataAssetsSpreadsheetItemContentRequest getValidatedProfiledDataAssetsSpreadsheetItemContentRequest)
        {
            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getValidatedProfiledDataAssetsSpreadsheetItemContentResult = await dataAssetService.GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(
                    initiatingUserDetails,
                    getValidatedProfiledDataAssetsSpreadsheetItemContentRequest.RecordId);

                if (!getValidatedProfiledDataAssetsSpreadsheetItemContentResult.Success)
                {
                    logger.LogError("Failed to Get Validated Profiled Data Descriptions Spreadsheet Item Content with DataAssetsService: {Error}", getValidatedProfiledDataAssetsSpreadsheetItemContentResult.Error);
                    return BadRequest(getValidatedProfiledDataAssetsSpreadsheetItemContentResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetValidatedProfiledDataAssetsSpreadsheetItemContentResponse(
                    (ValidatedProfiledDataAsset)getValidatedProfiledDataAssetsSpreadsheetItemContentResult.Data!);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("publish-validated-profiled-data-assets-spreadsheet-content")]
        [ProducesResponseType(typeof(PublishValidatedProfiledDataAssetsSpreadsheetContentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishValidatedProfiledDataAssetsSpreadsheetContent(
            PublishValidatedProfiledDataAssetsSpreadsheetContentRequest profiledDataAssetsSpreadsheetContentRequest)
        {
            ArgumentNullException.ThrowIfNull(profiledDataAssetsSpreadsheetContentRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var publishValidatedProfiledDataAssetsSpreadsheetContentResult = await dataAssetService.PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(
                    initiatingUserDetails,
                    profiledDataAssetsSpreadsheetContentRequest);

                if (!publishValidatedProfiledDataAssetsSpreadsheetContentResult.Success)
                {
                    logger.LogError("Failed to Publish Validated Profiled Data Descriptions Spreadsheet Content with DataAssetsService: {Error}", publishValidatedProfiledDataAssetsSpreadsheetContentResult.Error);
                    return BadRequest(publishValidatedProfiledDataAssetsSpreadsheetContentResult.Error);
                }

                var response = dataAssetResponseFactory.CreatePublishValidatedProfiledDataAssetsSpreadsheetContentResponse(
                    publishValidatedProfiledDataAssetsSpreadsheetContentResult.Data!.Success,
                    publishValidatedProfiledDataAssetsSpreadsheetContentResult.Data.Errors,
                    publishValidatedProfiledDataAssetsSpreadsheetContentResult.Data.PublishedValidatedProfiledDataAssetsSpreadsheetContentItems);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("clear-validated-profiled-data-assets-spreadsheet-content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ClearValidatedProfiledDataAssetsSpreadsheetContent()
        {
            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var clearValidatedProfiledDataAssetsSpreadsheetContentResult = await dataAssetService.ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(
                    initiatingUserDetails);

                if (!clearValidatedProfiledDataAssetsSpreadsheetContentResult.Success)
                {
                    logger.LogError("Failed to Clear Validated Profiled Data Descriptions Spreadsheet Content with DataAssetsService: {Error}", clearValidatedProfiledDataAssetsSpreadsheetContentResult.Error);
                    return BadRequest(clearValidatedProfiledDataAssetsSpreadsheetContentResult.Error);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("check-for-potential-duplicates-in-validated-spreadsheet-content")]
        [ProducesResponseType(typeof(CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckForPotentialDuplicatesToDataAssetSpreadsheetContent(
            [FromQuery] CheckForPotentialDuplicatesInValidatedSpreadsheetContentRequest checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest)
        {
            ArgumentNullException.ThrowIfNull(checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var checkForPotentialDuplicatesToDataAssetSpreadsheetContentResult = await dataAssetService.CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(
                    initiatingUserDetails);

                if (!checkForPotentialDuplicatesToDataAssetSpreadsheetContentResult.Success)
                {
                    logger.LogError("Failed to Check For Potential Duplicates In Validated Spreadsheet Content with DataAssetsService: {Error}", checkForPotentialDuplicatesToDataAssetSpreadsheetContentResult.Error);
                    return BadRequest(checkForPotentialDuplicatesToDataAssetSpreadsheetContentResult.Error);
                }

                var response = dataAssetResponseFactory.CreateCheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse(
                    checkForPotentialDuplicatesToDataAssetSpreadsheetContentResult.Data!.PotentialDuplicatesToSpreadsheetContent);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("check-for-potential-duplicates-in-validated-spreadsheet-item")]
        [ProducesResponseType(typeof(CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckForPotentialDuplicatesToDataAssetSpreadsheetItem(
            [FromQuery] CheckForPotentialDuplicatesInValidatedSpreadsheetItemRequest checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest)
        {
            ArgumentNullException.ThrowIfNull(checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var checkForPotentialDuplicatesToDataAssetSpreadsheetItemResult = await dataAssetService.CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(
                    initiatingUserDetails,
                    checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest.RecordId);

                if (!checkForPotentialDuplicatesToDataAssetSpreadsheetItemResult.Success)
                {
                    logger.LogError("Failed to Check For Potential Duplicates In Validated Spreadsheet Item with DataAssetsService: {Error}", checkForPotentialDuplicatesToDataAssetSpreadsheetItemResult.Error);
                    return BadRequest(checkForPotentialDuplicatesToDataAssetSpreadsheetItemResult.Error);
                }

                var response = dataAssetResponseFactory.CreateCheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse(
                    checkForPotentialDuplicatesToDataAssetSpreadsheetItemResult.Data!.PotentialDuplicatesToSpreadsheetItem);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-data-asset-template-spreadsheet")]
        [ProducesResponseType(typeof(Stream), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDataAssetTemplateSpreadsheet([FromQuery] GetDataAssetTemplateSpreadsheetRequest getDataAssetTemplateSpreadsheetRequest)
        {
            ArgumentNullException.ThrowIfNull(getDataAssetTemplateSpreadsheetRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getDataAssetTemplateSpreadsheetResult = await dataAssetService.GetDataAssetTemplateSpreadsheetAsync(
                    initiatingUserDetails,
                    getDataAssetTemplateSpreadsheetRequest.ProfileId);

                if (!getDataAssetTemplateSpreadsheetResult.Success)
                {
                    logger.LogError("Failed to Get Data Description Template Spreadsheet from DataAssetsService: {Error}", getDataAssetTemplateSpreadsheetResult.Error);
                    return BadRequest(getDataAssetTemplateSpreadsheetResult.Error);
                }

                var response = dataAssetResponseFactory.CreateGetDataAssetTemplateSpreadsheetResponse(
                    getDataAssetTemplateSpreadsheetResult.Data!);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpPost("migrate-profiled-data-asset-from-1p0-to-3p1")]
        [ProducesResponseType(typeof(MigrateProfiledDataAssetsFrom1p0To3p1Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MigrateProfiledDataAssetsFrom1p0To3p1(
            MigrateProfiledDataAssetsFrom1p0To3p1Request migrateProfiledDataAssetsFrom1P0To3P1Request)
        {
            ArgumentNullException.ThrowIfNull(migrateProfiledDataAssetsFrom1P0To3P1Request);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var migrateProfiledDataAssetFrom1p0To3p1Result = await dataAssetService.MigrateProfiledDataAssetsFrom1p0To3p1Async(
                    initiatingUserDetails,
                    migrateProfiledDataAssetsFrom1P0To3P1Request.DataAssetIds);

                if (!migrateProfiledDataAssetFrom1p0To3p1Result.Success)
                {
                    logger.LogError("Failed to Migrate Profiled Data Description from v1.0 to v3.1 in DataAssetsService: {Error}", migrateProfiledDataAssetFrom1p0To3p1Result.Error);

                    return BadRequest(migrateProfiledDataAssetFrom1p0To3p1Result.Error);
                }

                var response = dataAssetResponseFactory.CreateMigrateProfiledDataAssetsFrom1p0To3p1Response(
                    migrateProfiledDataAssetFrom1p0To3p1Result.Data!.Results);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("get-esda-ownership-details")]
        [ProducesResponseType(typeof(GetEsdaOwnershipDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetEsdaOwnershipDetails([FromQuery] GetEsdaOwnershipDetailsRequest getEsdaOwnershipDetailsRequest)
        {
            ArgumentNullException.ThrowIfNull(getEsdaOwnershipDetailsRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getEsdaOwnershipDetailsResult = await dataAssetService.GetEsdaOwnershipDetailsAsync(
                    initiatingUserDetails,
                    getEsdaOwnershipDetailsRequest.DataAssetId);

                if (!getEsdaOwnershipDetailsResult.Success)
                {
                    return BuildFailedResultResponse("Failed to Get Esda Ownership Details from DataAssetsService", getEsdaOwnershipDetailsResult);
                }

                var response = dataAssetResponseFactory.CreateGetEsdaOwnershipDetailsResponse(
                    getEsdaOwnershipDetailsResult.Data!.EsdaId,
                    getEsdaOwnershipDetailsResult.Data!.Title,
                    getEsdaOwnershipDetailsResult.Data!.OrganisationId,
                    getEsdaOwnershipDetailsResult.Data!.DomainId,
                    getEsdaOwnershipDetailsResult.Data!.ContactPointName,
                    getEsdaOwnershipDetailsResult.Data!.ContactPointEmailAddress,
                    getEsdaOwnershipDetailsResult.Data!.DataShareRequestNotificationRecipientType,
                    getEsdaOwnershipDetailsResult.Data!.CustomDsrNotificationAddress);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }

        private IActionResult BuildFailedResultResponse<T>(
            string message,
            IServiceOperationDataResult<T> failedResult)
        {
            logger.LogError("{Message}: {Error}", message, failedResult.Error);

            return failedResult.StatusCode.HasValue
                ? new ObjectResult(failedResult.Error)
                { StatusCode = (int)failedResult.StatusCode.Value }
                : BadRequest(failedResult.Error);
        }

        private async Task<IUserDetails> DoGetInitiatingUserDetailsAsync()
        {
            var initiatingUserDetails = await userProfilePresenter.GetInitiatingUserDetailsAsync();

            if (initiatingUserDetails == null)
            {
                logger.LogError("Unable to get user details for initiating user");
            }

            return initiatingUserDetails!;
        }
    }
}
