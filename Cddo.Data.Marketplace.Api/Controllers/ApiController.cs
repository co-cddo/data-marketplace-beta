using Cddo.Data.Marketplace.Api.Controllers;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Agm.Catalog.DotNet.Core.Utilities;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Exceptions;
using DataService = Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.DataService;
using DataSet = Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.DataSet;
using ErrorMessage = Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.ErrorMessage;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Cddo.Data.Marketplace.Api.Validation;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;

namespace CDDO.DataMarketplace.Controllers.External
{

    // API Controller
    [ApiController]
    [Authorize(AuthenticationSchemes = "ApiAuthScheme")]
    [Route("[controller]")]
    public class DataMarketplaceApiController : ControllerBase
    {
        private readonly ILogger<DataMarketplaceApiController> _logger;
        private readonly IDataAssetService _dataAssetService;
        private readonly IUserProfilePresenter _users;
        private readonly IEnumMemberConverter _enumMemberConverter;
        private readonly IAppInsightsLogger _appInsightsLogger;
        private readonly IModelValidationService _modelValidationService;

        private const string profileId = "dcat-ukap-v3.1";
        private const string validationError = "ValidationError";
        private const string errorDM00010 = "DM00010";
        private const string errorDM00011 = "DM00011";
        private const string errorDM00012 = "DM00012";
        private const string errorDM00014 = "DM00014";
        private const string errorDM00015 = "DM00015";
        private const string validationFailureMessage = "Validation failures";
        private const string ingestionApiCall = "IngestionApiCall";
        private const string trigger404 = "trigger-404";
        private const string trigger500 = "trigger-500";
        private const string retrieveDataset = "RetrieveDataset";
        private const string error = "Error";
        private const string ingestionApiError = "IngestionApiError";
        private const string fatalError = "Fatal";
        private const string applicationError = "ApplicationError";
        private const string createDataset = "CreateDataset";
        private const string create = "Create";
        private const string updateDataset = "UpdateDataset";
        private const string notFoundError = "NotFoundError";
        private const string removeDataset = "RemoveDataset";
        private const string createDataService = "CreateDataService";
        private const string updateDataService = "UpdateDataService";
        private const string removeDataService = "RemoveDataService";
        private const string sandbox = "Sandbox";
        private const string metadataId = "MetadataId";
        private const string errorMessage = "Simulated internal server error for sandbox testing.";

        // Inject HttpClient and IConfiguration (for BaseUrl)
        public DataMarketplaceApiController(
            ILogger<DataMarketplaceApiController> logger,
            IDataAssetService dataAssetService,
            IUserProfilePresenter users,
            IAppInsightsLogger appInsightsLogger,
            IEnumMemberConverter enumMemberConverter,
            IModelValidationService modelValidationService)
        {
            _users = users;
            _logger = logger;
            _dataAssetService = dataAssetService;
            _appInsightsLogger = appInsightsLogger;
            _enumMemberConverter = enumMemberConverter;
            _modelValidationService = modelValidationService;
        }

        private bool IsSandboxEnvironment()
        {
            var environmentClaim = User.Claims.FirstOrDefault(c => c.Type == "environment")?.Value?.ToLower();
            return environmentClaim == "test";
        }

        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "DiscoverScope")]
        [HttpGet("catalogued-resources")]
        [ProducesResponseType(typeof(List<CataloguedResource>), 200)] // Success
        [ProducesResponseType(typeof(ErrorMessage), 400)] // Bad Request
        [ProducesResponseType(204)] // No Content
        public async Task<IActionResult> QueryCataloguedResources([FromQuery] string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                var errorResponse = new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors = [new InnerError { Detail = "The 'filter' query parameter is required.", Type = validationError }]
                };

                return BadRequest(errorResponse); // Return structured 400 response
            }

            if (IsSandboxEnvironment())
            {
                // Mocked response for sandbox
                var mockResponse = _modelValidationService.GetMockedCataloguedResources();

                return Ok(mockResponse);
            }

            //Log start Api call
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataSearchPerformed, "QueryCataloguedResources", "CDDO", "Query", ingestionApiCall, filter, userEventProperties);

            // Define the request data for the service
            var dataAssetTypes = new List<DataAssetType>
            {
                DataAssetType.DataSet,
                DataAssetType.DataService,
                DataAssetType.DataGroup,
                DataAssetType.DataShare
            };

            try
            {
                // Call the service to get profiled data assets
                var getDataAssetsResult = await _dataAssetService.GetProfiledDataAssetsAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetTypes,
                    true,
                    true,
                    filter); // Pass the search text

                if (!getDataAssetsResult.Success)
                {
                    _logger.LogError("Error occurred while querying catalogued resources");

                    return StatusCode(500, new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while querying catalogued resources.",
                        Errors = [new InnerError { Detail = getDataAssetsResult.Error! }]
                    });
                }

                var profiledDataAssets = getDataAssetsResult.Data!.ProfiledDataAssets.ToList();


                // Check the result from the service
                if (!profiledDataAssets.Any()) return Ok();

                // Map the result to CataloguedResource DTO before returning
                var cataloguedResources = profiledDataAssets.Select(profiledDataAsset =>
                {
                    var cataloguedResource = JsonSerializer.Deserialize<CataloguedResource>(profiledDataAsset.Payload, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                    return cataloguedResource;
                }).OfType<CataloguedResource>().ToList();

                // Return the resources as a 200 OK response
                return Ok(cataloguedResources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while querying catalogued resources.");
                return StatusCode(500, "Internal server error occurred.");
            }
        }

        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "PublishScope")]
        [HttpGet("datasets/{datasetId}")]
        [ProducesResponseType(typeof(CataloguedResource), 200)]  // Success
        [ProducesResponseType(typeof(ErrorMessage), 404)] // Not Found
        public async Task<IActionResult> RetrieveDataset(string datasetId)
        {
            if (!Guid.TryParse(datasetId, out _))
            {
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors =
                    [
                        new InnerError
                        {
                            Detail = "The datasetId provided is not a valid GUID.",
                            Type = validationError
                        }
                    ]
                });
            }

            if (IsSandboxEnvironment())
            {
                // Introduce controlled error responses for sandbox users
                var validDataSet = _modelValidationService.HandleSimulatedErrors(null, datasetId, true);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                // Normal mocked dataset for sandbox
                var mockDataset = _modelValidationService.GetMockedDataset(datasetId);
                return Ok(mockDataset);
            }

            // Test
            var userEventProperties = new Dictionary<string, string>();
            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

                userEventProperties.Add(metadataId, datasetId);
                _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataViewed, retrieveDataset, "CDDO", "Read", ingestionApiCall, datasetId, userEventProperties);

                var getDataAssetResult = await _dataAssetService.GetProfiledDataAssetAsync(
                    initiatingUserDetails, profileId, Guid.Parse(datasetId));

                // Check if the dataset was found
                if (!getDataAssetResult.Success)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.None, retrieveDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);
                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00010,
                        Message = $"Dataset with identifier {datasetId} does not exist.",
                        Errors = new List<InnerError>()
                    });
                }

                var cataloguedResource = JsonSerializer.Deserialize<DataSet>(getDataAssetResult.Data!.ProfiledDataAsset.Payload, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Return the dataset as a 200 OK response
                return Ok(cataloguedResource);
            }
            catch (Exception ex)
            {                
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, retrieveDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00011,
                    Message = $"Internal server error occurred while retrieving dataset with ID {datasetId}.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "PublishScope")]
        [HttpPost("datasets")]
        [ProducesResponseType(201)] // Created
        [ProducesResponseType(typeof(ErrorMessage), 400)] // Bad Request
        [ProducesResponseType(typeof(ErrorMessage), 409)] // Conflict
        public async Task<IActionResult> CreateDataset([FromBody] DataSet dataset)
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();
            if (initiatingUserDetails == null)
            {
                _logger.LogError("Failed to retrieve initiating user details.");
                return StatusCode(500, new ErrorMessage
                {
                    Code = "errorDM00015",
                    Message = "Failed to retrieve initiating user details.",
                    Errors = [new InnerError { Detail = "User details could not be retrieved.", Type = fatalError }]
                });
            }

            if (!ModelState.IsValid)
            {
                var message = _modelValidationService.RecordModelStateErrorsAndBuildErrorResponse(ControllerContext, initiatingUserDetails);

                return BadRequest(message);
            }

            if (dataset == null)
            {
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors =
                    [
                        new InnerError
                        {
                            Detail = "Dataset object is null",
                            Type = fatalError
                        }
                    ]
                });
            }

            var validateCataloguedResourceResult = await _dataAssetService.ValidateCataloguedResourceAsync(
                profileId, dataset, DataAssetType.DataSet, true);

            var validationPropertyResults =
                validateCataloguedResourceResult.Data!.DataAssetValidationPropertyResults.ToList();

            var validationErrorResponse =
                 _modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResults, initiatingUserDetails);

            if (validationErrorResponse != null)
            {
                return BadRequest(validationErrorResponse);
            }

            if (IsSandboxEnvironment())
            {

                // Simulate errors based on manual triggers
                var validDataSet = _modelValidationService.HandleSimulatedErrors(dataset, null, true);
                if(validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                // Return mock success response if no errors
                return Created($"/datasets/{Guid.NewGuid()}", new
                {
                    Message = "Dataset successfully created in sandbox mode.",
                    DatasetId = Guid.NewGuid().ToString()
                });
            }

            _logger.LogInformation("Received dataset: {Dataset}", dataset);


            //Log start Api call

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);

            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataCreated, createDataset, "CDDO", create, ingestionApiCall, dataset.Title ?? "", userEventProperties);

            // Check if SupplierIdentifier is provided (optional based on spec)
            if (!string.IsNullOrEmpty(dataset.SupplierIdentifier))
            {
                // Check for existing dataset with the same Supplier Identifier (conflict scenario)
                var getProfiledDataAssetsResult = await _dataAssetService.GetProfiledDataAssetsAsync(
                    initiatingUserDetails,
                    profileId,
                    new List<DataAssetType> { DataAssetType.DataSet },
                    true,
                    true,
                    dataset.SupplierIdentifier);

                if (!getProfiledDataAssetsResult.Success)
                {
                    return StatusCode(500, new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while getting existing datasets with the supplier Id.",
                        Errors = [new InnerError { Detail = getProfiledDataAssetsResult.Error! }]
                    });
                }

                if (getProfiledDataAssetsResult.Data?.ProfiledDataAssets.Any() ?? false)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDatasetConflict, retrieveDataset, "CDDO", error, ingestionApiError, dataset.SupplierIdentifier, userEventProperties);

                    return Conflict(new ErrorMessage
                    {
                        Code = errorDM00014,
                        Message = "A dataset with this supplier identifier already exists.",
                        Errors = []
                    });
                }
            }

            try
            {
                var metadataManagement = await _dataAssetService.SetMetadataManagement(dataset, initiatingUserDetails, profileId);

                // Serialize the dataset using custom options
                var serializedDataset = JsonSerializer.Serialize(dataset, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Call the service to create the new dataset
                var createResult = await _dataAssetService.AddProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    DataAssetType.DataSet,
                    serializedDataset,
                    metadataManagement,
                    DataAssetActionSourceEnum.Api);

                // Handle service failure case
                if (!createResult.Success)
                {
                    _logger.LogError("Error occurred while creating a new dataset");
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, createDataset, "CDDO", error, ingestionApiError, dataset.Title ?? "", userEventProperties);
                    return StatusCode(500, new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while creating the dataset.",
                        Errors = [new InnerError { Detail = createResult.Error! }]
                    });
                }

                //Log data service created
                _appInsightsLogger.LogAdminEventBase(EventTypes.AdminAuditEvent.MetadataApiIngestion, "Create Data Service", "CDDO", create, "Data asset add", createResult.Data!.DataAssetId.ToString(), userEventProperties);

                // Return 201 Created on success
                return Created($"/datasets/{createResult.Data!.DataAssetId}", null);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while creating a new dataset");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, createDataset, "CDDO", error, ingestionApiError, dataset.Title ?? "", userEventProperties);
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00015,
                    Message = "Invalid dataset format. JSON deserialization failed.",
                    Errors = [new InnerError { Detail = ex.Message, Type = fatalError }]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new dataset");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, createDataset, "CDDO", error, ingestionApiError, dataset.Title ?? "", userEventProperties);
                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00015,
                    Message = "An internal server error occurred while creating the dataset.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }


        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "PublishScope")]
        // PATCH /datasets/{dataset-id}
        [HttpPatch("datasets/{datasetId}")]
        [ProducesResponseType(typeof(DataSet), 200)] // Success
        [ProducesResponseType(typeof(ErrorMessage), 400)] // Bad Request
        [ProducesResponseType(typeof(ErrorMessage), 404)] // Not Found
        [EndpointDescription("You can update an existing dataset in the Data Marketplace.")]
        public async Task<IActionResult> UpdateDataset(string datasetId, [FromBody] DataSet patchModel)
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            if (!ModelState.IsValid)
            {
                var message = _modelValidationService.RecordModelStateErrorsAndBuildErrorResponse(ControllerContext, initiatingUserDetails);

                return BadRequest(message);
            }

            if (string.IsNullOrEmpty(datasetId))
            {
                return NotFound(new ErrorMessage
                {
                    Code = errorDM00010,
                    Message = "Dataset identifier is missing or invalid.",
                    Errors = [new InnerError { Detail = "The dataset ID provided is null or empty.", Type = validationError }]
                });
            }

            if (patchModel == null)
            {
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors =
                    [
                        new InnerError
                        {
                            Detail = "Dataset object is null",
                            Type = fatalError
                        }
                    ]
                });
            }

            var validateCataloguedResourceResult = await _dataAssetService.ValidateCataloguedResourceAsync(
                profileId, patchModel, DataAssetType.DataSet, false);

            var validationPropertyResults =
                validateCataloguedResourceResult.Data!.DataAssetValidationPropertyResults.ToList();

            var validationErrorResponse =
                 _modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResults, initiatingUserDetails);

            if (validationErrorResponse != null)
            {
                return BadRequest(validationErrorResponse);
            }

            if (IsSandboxEnvironment())
            {
                var validDataSet = _modelValidationService.HandleSimulatedErrors(null, datasetId, true);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                var updatedDataset = _modelValidationService.GetMockedUpdatedDataset(datasetId, patchModel);
                return Ok(updatedDataset);
            }

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            userEventProperties.Add(metadataId, datasetId);

            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDatasetUpdated, updateDataset, "CDDO", "Update", ingestionApiCall, datasetId, userEventProperties);

            if (patchModel.Identifier != null && patchModel.Identifier != datasetId)
            {
                _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDatasetConflict, updateDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                return Conflict(new ErrorMessage
                {
                    Code = errorDM00010,
                    Message = $"Dataset with identifier {datasetId} could not be patched.",
                    Errors = []
                });
            }
            patchModel.Identifier = datasetId;

            try
            {
                DataAssetStatus? status = patchModel.Status switch
                {
                    ResourceStatusEnum.Draft => DataAssetStatus.Draft,
                    ResourceStatusEnum.Published => DataAssetStatus.Published,
                    ResourceStatusEnum.Withdrawn => DataAssetStatus.Withdrawn,
                    ResourceStatusEnum.Deleted => DataAssetStatus.Deleted,
                    _ => null
                };

                ManagementMetadataDcatUkApV3_1? managementMetadata = null;
                // Assuming managementMetadata is optional or can be built/derived as needed
                if (status != null)
                {
                    managementMetadata = new ManagementMetadataDcatUkApV3_1
                    {
                        DataAssetStatus = status
                    };
                }

                // Serialize the patchModel to JSON
                var payload = JsonSerializer.Serialize(patchModel, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Call the service to patch the data asset
                var patchResult = await _dataAssetService.PatchProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    DataAssetType.DataSet,
                    payload,
                    managementMetadata,
                    DataAssetActionSourceEnum.Api);

                // Check if the patching was successful
                if (!patchResult.Success)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, updateDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while updating the dataset.",
                        Errors = [new InnerError { Detail = patchResult.Error! }]
                    });
                }

                var guid = Guid.Parse(datasetId);

                var getDataAssetResult = await _dataAssetService.GetProfiledDataAssetAsync(
                    initiatingUserDetails, profileId, guid);

                if (!getDataAssetResult.Success)
                {
                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred retrieving the updated dataset.",
                        Errors = [new InnerError { Detail = patchResult.Error! }]
                    });
                }

                var dataSet = JsonSerializer.Deserialize<DataSet>(getDataAssetResult.Data!.ProfiledDataAsset.Payload, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Return the updated dataset
                return Ok(dataSet);
            }
            catch (UnAuthorizedAccessToDataAssetException ex)
            {
                _logger.LogError(ex, $"Unauthorized access attempted with patch to dataset with ID {datasetId}");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.AccessError, updateDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage
                {
                    Code = "TODO",
                    Message = "TODO",
                    Errors = []
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while patching dataset with ID {DatasetId}", datasetId);
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, updateDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00011,
                    Message = $"Internal server error occurred while patching dataset with ID {datasetId}.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        //Done
        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "DeleteScope")]
        // DELETE /datasets/{dataset-id}
        [HttpDelete("datasets/{datasetId}")]
        [ProducesResponseType(204)] // No Content
        [ProducesResponseType(typeof(ErrorMessage), 404)] // Not Found
        [EndpointDescription("Delete an existing dataset in the Data Marketplace.")]
        public async Task<IActionResult> RemoveDataset(string datasetId)
        {
            if (string.IsNullOrEmpty(datasetId))
            {
                return NotFound(new ErrorMessage
                {
                    Code = errorDM00010,
                    Message = "Dataset identifier is missing or invalid.",
                    Errors =
                    [
                        new InnerError {Detail = "The dataset ID provided is null or empty.", Type = validationError}
                    ]
                });
            }

            if (IsSandboxEnvironment())
            {
                var validDataSet = _modelValidationService.HandleSimulatedErrors(null, datasetId, true);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                return NoContent();
            }

            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            userEventProperties.Add(metadataId, datasetId);
            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDeleted, removeDataset, "CDDO", "Delete", ingestionApiCall, datasetId, userEventProperties);

            try
            {
                // Call the service to delete the dataset by ID
                var deletionResult = await _dataAssetService.DeleteProfiledDataAssetAsync(
                    initiatingUserDetails, profileId, Guid.Parse(datasetId));

                // Check if the deletionResult contains an error (assuming Error is a string)
                if (!deletionResult.Success)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, removeDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);
                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while deleting the dataset.",
                        Errors = [new InnerError { Detail = deletionResult.Error! }]
                    });
                }

                // If deletion is successful, return 204 No Content
                return NoContent();
            }
            catch (UnAuthorizedAccessToDataAssetException ex)
            {
                _logger.LogError(ex, $"Unauthorized access attempted with deletion of dataset with ID {datasetId}");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.AccessError, removeDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage
                {
                    Code = "TODO",
                    Message = "TODO",
                    Errors = []
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dataset with ID {DatasetId}", datasetId);
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, removeDataset, "CDDO", error, ingestionApiError, datasetId, userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00011,
                    Message = $"Internal server error occurred while deleting dataset with ID {datasetId}.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        // Done
        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "PublishScope")]
        // GET /data-services/{data-service-id}
        [HttpGet("data-services/{dataServiceId}")]
        [ProducesResponseType(typeof(DataService), 200)] // Success
        [ProducesResponseType(typeof(ErrorMessage), 404)] // Not Found
        public async Task<IActionResult> RetrieveDataService(string dataServiceId)
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            if (!Guid.TryParse(dataServiceId, out _))
            {
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors =
                    [
                        new InnerError
                        {
                            Detail = "The dataServiceId provided is not a valid GUID.",
                            Type = validationError
                        }
                    ]
                });
            }

            if (IsSandboxEnvironment())
            {

                var validDataSet = _modelValidationService.HandleSimulatedErrors(null, dataServiceId, false);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                var mockedDataService = _modelValidationService.GetMockedDataServive(dataServiceId);

                return Ok(mockedDataService);
            }

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            userEventProperties.Add(metadataId, dataServiceId);
            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataViewed, "RetrieveDataService", "CDDO", "Read", ingestionApiCall, dataServiceId, userEventProperties);

            try
            {
                // Call the service to retrieve the data service by ID
                var getDataAssetResult = await _dataAssetService.GetProfiledDataAssetAsync(
                    initiatingUserDetails, profileId, Guid.Parse(dataServiceId));

                // Check if the data service was found
                if (!getDataAssetResult.Success)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.None, "RetrieveDataService", "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00010,
                        Message = $"Data service with identifier {dataServiceId} does not exist.",
                        Errors = []
                    });
                }

                var dataService = JsonSerializer.Deserialize<DataService>(getDataAssetResult.Data!.ProfiledDataAsset.Payload, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Return the data service as a 200 OK response
                return Ok(dataService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data service with ID {DataServiceId}", dataServiceId);
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, "RetrieveDataService", "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00011,
                    Message = $"Internal server error occurred while retrieving data service with ID {dataServiceId}.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        // Done
        // POST /data-services
        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "PublishScope")]
        [HttpPost("data-services")]
        [ProducesResponseType(201)] // Created
        [ProducesResponseType(typeof(ErrorMessage), 400)] // Bad Request
        [ProducesResponseType(typeof(ErrorMessage), 409)] // Conflict
        public async Task<IActionResult> CreateDataService([FromBody] DataService dataService)
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            if (!ModelState.IsValid)
            {
                var message = _modelValidationService.RecordModelStateErrorsAndBuildErrorResponse(ControllerContext, initiatingUserDetails);

                return BadRequest(message);
            }

            if (dataService == null)
            {
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors =
                    [
                        new InnerError
                        {
                            Detail = "DataService object is null",
                            Type = fatalError
                        }
                    ]
                });
            }

            var validateCataloguedResourceResult = await _dataAssetService.ValidateCataloguedResourceAsync(
                profileId, dataService, DataAssetType.DataService, true);

            var validationPropertyResults =
                validateCataloguedResourceResult.Data!.DataAssetValidationPropertyResults.ToList();

            var validationErrorResponse =
                 _modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResults, initiatingUserDetails);

            if (validationErrorResponse != null)
            {
                return BadRequest(validationErrorResponse);
            }

            if (IsSandboxEnvironment())
            {
                var validDataSet = _modelValidationService.HandleSimulatedErrors(dataService, null, false);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                // Mock a successful creation response
                return Created($"/data-services/{Guid.NewGuid()}", new
                {
                    Message = "Data service successfully created in sandbox mode.",
                    DataServiceId = Guid.NewGuid().ToString()
                });
            }

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataCreated, createDataService, "CDDO", create, ingestionApiCall, dataService.Title ?? "", userEventProperties);

            // Log incoming data for debugging
            _logger.LogInformation("Received dataService: {@dataService}", dataService);

            // Check if SupplierIdentifier is provided (optional based on spec)
            if (!string.IsNullOrEmpty(dataService.SupplierIdentifier))
            {
                // Check for existing data service with the same Supplier Identifier (conflict scenario)
                var getProfiledDataAssetsResult = await _dataAssetService.GetProfiledDataAssetsAsync(
                    initiatingUserDetails,
                    profileId,
                    new List<DataAssetType> { DataAssetType.DataService },
                    true,
                    true,
                    dataService.SupplierIdentifier);

                if (!getProfiledDataAssetsResult.Success)
                {
                    return StatusCode(500, new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while getting existing data services with the supplier Id.",
                        Errors = [new InnerError { Detail = getProfiledDataAssetsResult.Error! }]
                    });
                }

                if (getProfiledDataAssetsResult.Data?.ProfiledDataAssets.Any() ?? false)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDatasetConflict, createDataService, "CDDO", error, ingestionApiError, dataService.Title ?? "", userEventProperties);

                    return Conflict(new ErrorMessage
                    {
                        Code = errorDM00014,
                        Message = "A data service with this supplier identifier already exists.",
                        Errors = []
                    });
                }
            }

            try
            {
                var status = dataService.Status switch
                {
                    ResourceStatusEnum.Draft => DataAssetStatus.Draft,
                    ResourceStatusEnum.Published => DataAssetStatus.Published,
                    ResourceStatusEnum.Withdrawn => DataAssetStatus.Withdrawn,
                    ResourceStatusEnum.Deleted => DataAssetStatus.Deleted,
                    null => throw new ArgumentNullException(nameof(dataService), "Data service status is null."),
                    _ => throw new InvalidEnumArgumentException(nameof(dataService.Status), (int)dataService.Status!, typeof(ResourceStatusEnum))
                };

                var initiatingUserIdSet = initiatingUserDetails.UserIdSet;

                var managementMetadata = new ManagementMetadataDcatUkApV3_1
                {
                    DataAssetStatus = status,
                    OrganisationId = initiatingUserIdSet.OrganisationId.ToString(),
                    DomainId = initiatingUserIdSet.DomainId.ToString(),
                    DataOwnerId = initiatingUserIdSet.UserId.ToString(),
                    Permissions = new Permissions
                    {
                        ManageabilityPermissions = new ActionPermissions
                        {
                            OrganisationPermissions = new Dictionary<string, bool>
                                    {
                                        { initiatingUserIdSet.OrganisationId.ToString(), true }
                                    },
                            DomainPermissions = new Dictionary<string, bool>
                                    {
                                        { initiatingUserIdSet.DomainId.ToString(), true }
                                    }
                        }
                    },
                    DcatUK3_1Properties = new DcatUK3_1SpecificProperties
                    {
                        AllowDSRRequest = true,
                        RequiresDSR = true
                    }
                };

                // Serialize the data service using custom options
                var serializedDataService = JsonSerializer.Serialize(dataService, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Call the service to create the new data service
                var createResult = await _dataAssetService.AddProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    DataAssetType.DataService,  // We're creating a data service here
                    serializedDataService,
                    managementMetadata,
                    DataAssetActionSourceEnum.Api);

                // Handle service failure case
                if (!createResult.Success)
                {
                    _logger.LogError("Error occurred while creating a new data service");
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, createDataService, "CDDO", error, ingestionApiError, dataService.Title ?? "", userEventProperties);
                    return StatusCode(500, new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while creating the data service.",
                        Errors = [new InnerError { Detail = createResult.Error! }]
                    });
                }

                //Log data service created
                _appInsightsLogger.LogAdminEventBase(EventTypes.AdminAuditEvent.MetadataApiIngestion, "Create Data Asset", "CDDO", create, "Data asset add", createResult.Data!.DataAssetId.ToString(), userEventProperties);
                // Return 201 Created on success
                return Created($"/data-services/{createResult.Data.DataAssetId}", null);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while creating a new data service");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, createDataService, "CDDO", error, ingestionApiError, dataService.Title ?? "", userEventProperties);

                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = "Invalid data service format. JSON deserialization failed.",
                    Errors = [new InnerError { Detail = ex.Message, Type = fatalError }]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a new data service");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, createDataService, "CDDO", error, ingestionApiError, dataService.Title ?? "", userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00015,
                    Message = "An internal server error occurred while creating the data service.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        // PATCH /data-services/{data-service-id}
        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "PublishScope")]
        [HttpPatch("data-services/{dataServiceId}")]
        [ProducesResponseType(typeof(DataService), 200)] // Success
        [ProducesResponseType(typeof(ErrorMessage), 400)] // Bad Request
        [ProducesResponseType(typeof(ErrorMessage), 404)] // Not Found
        [Tags("Update dataservice")]
        [EndpointDescription("You can update an existing data service in the Data Marketplace.")]
        public async Task<IActionResult> UpdateDataService(string dataServiceId, [FromBody] DataService patchModel)
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            if (!ModelState.IsValid)
            {
                var message = _modelValidationService.RecordModelStateErrorsAndBuildErrorResponse(ControllerContext, initiatingUserDetails);

                return BadRequest(message);
            }

            if (string.IsNullOrEmpty(dataServiceId))
            {
                return NotFound(new ErrorMessage
                {
                    Code = errorDM00010,
                    Message = "DataService identifier is missing or invalid.",
                    Errors = [new InnerError { Detail = "The data service ID provided is null or empty.", Type = validationError }]
                });
            }

            if (patchModel == null)
            {
                return BadRequest(new ErrorMessage
                {
                    Code = errorDM00012,
                    Message = validationFailureMessage,
                    Errors =
                    [
                        new InnerError
                        {
                            Detail = "DataService object is null",
                            Type = fatalError
                        }
                    ]
                });
            }

            var validateCataloguedResourceResult = await _dataAssetService.ValidateCataloguedResourceAsync(
                profileId, patchModel, DataAssetType.DataSet, false);

            var validationPropertyResults =
                validateCataloguedResourceResult.Data!.DataAssetValidationPropertyResults.ToList();

            var validationErrorResponse =
                _modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResults, initiatingUserDetails);

            if (validationErrorResponse != null)
            {
                return BadRequest(validationErrorResponse);
            }

            if (IsSandboxEnvironment())
            {
                var validDataSet = _modelValidationService.HandleSimulatedErrors(patchModel, dataServiceId, false);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                var updatedDataService = _modelValidationService.GetMockedUpdatedDataService(dataServiceId);

                return Ok(updatedDataService);
            }

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            userEventProperties.Add(metadataId, dataServiceId);

            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataEdited, updateDataService, "CDDO", "Update", ingestionApiCall, dataServiceId, userEventProperties);

            if (patchModel.Identifier != null && patchModel.Identifier != dataServiceId)
            {
                _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDatasetConflict, updateDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                return Conflict(new ErrorMessage
                {
                    Code = errorDM00010,
                    Message = $"Data service with identifier {dataServiceId} could not be patched.",
                    Errors = []
                });
            }
            patchModel.Identifier = dataServiceId;

            try
            {
                DataAssetStatus? status = patchModel.Status switch
                {
                    ResourceStatusEnum.Draft => DataAssetStatus.Draft,
                    ResourceStatusEnum.Published => DataAssetStatus.Published,
                    ResourceStatusEnum.Withdrawn => DataAssetStatus.Withdrawn,
                    ResourceStatusEnum.Deleted => DataAssetStatus.Deleted,
                    _ => null
                };
                // Assuming managementMetadata is optional or can be built/derived as needed

                ManagementMetadataDcatUkApV3_1? managementMetadata = null;
                // Assuming managementMetadata is optional or can be built/derived as needed
                if (status != null)
                {
                    managementMetadata = new ManagementMetadataDcatUkApV3_1
                    {
                        DataAssetStatus = status
                    };
                }

                // Serialize the patchModel to JSON using the same custom serialization options
                var payload = JsonSerializer.Serialize(patchModel, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Call the service to patch the data asset
                var patchResult = await _dataAssetService.PatchProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    DataAssetType.DataService,
                    payload,
                    managementMetadata,
                    DataAssetActionSourceEnum.Api);

                // Check if the patching was successful
                if (!patchResult.Success)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, updateDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while updating the data service.",
                        Errors = [new InnerError { Detail = patchResult.Error! }]
                    });
                }

                var guid = Guid.Parse(dataServiceId);

                var getDataAssetResult = await _dataAssetService.GetProfiledDataAssetAsync(
                    initiatingUserDetails, profileId, guid);

                var dataService = JsonSerializer.Deserialize<DataService>(getDataAssetResult.Data!.ProfiledDataAsset.Payload, DataMarketplaceApiControllerHelpers.JsonSerializationOptions);

                // Return the updated data service
                return Ok(dataService);
            }
            catch (UnAuthorizedAccessToDataAssetException ex)
            {
                _logger.LogError(ex, $"Unauthorized access attempted with patch to data service with ID {dataServiceId}");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.AccessError, updateDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);
                return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage
                {
                    Code = "TODO",
                    Message = "TODO",
                    Errors = []
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while patching data service with ID {DataServiceId}", dataServiceId);
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, updateDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00011,
                    Message = $"Internal server error occurred while patching data service with ID {dataServiceId}.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        // Done
        [Authorize(AuthenticationSchemes = "ApiAuthScheme", Policy = "DeleteScope")]
        // DELETE /data-services/{data-service-id}
        [HttpDelete("data-services/{dataServiceId}")]
        [ProducesResponseType(204)] // No Content
        [ProducesResponseType(typeof(ErrorMessage), 404)] // Not Found
        [Tags("Delete dataservice")]
        [EndpointDescription("Delete an existing data service in the Data Marketplace.")]
        public async Task<IActionResult> RemoveDataService(string dataServiceId)
        {
            if (string.IsNullOrEmpty(dataServiceId))
            {
                return NotFound(new ErrorMessage
                {
                    Code = errorDM00010,
                    Message = "Data service identifier is missing or invalid.",
                    Errors =
                    [
                        new InnerError {Detail = "The data service ID provided is null or empty.", Type = validationError}
                    ]
                });
            }

            if (IsSandboxEnvironment())
            {
                var validDataSet = _modelValidationService.HandleSimulatedErrors(null, dataServiceId, false);
                if (validDataSet != null)
                {
                    return StatusCode(validDataSet.Value.Item1, validDataSet.Value.Item2);
                }

                return NoContent(); // Simulate a successful deletion with 204 No Content
            }

            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
            userEventProperties.Add(metadataId, dataServiceId);
            _appInsightsLogger.LogEventMainBase(EventTypes.MetadataEvent.MetadataDeleted, removeDataService, "CDDO", "Delete", ingestionApiCall, dataServiceId, userEventProperties);

            try
            {
                // Call the service to delete the data service by ID
                var deletionResult = await _dataAssetService.DeleteProfiledDataAssetAsync(
                    initiatingUserDetails, profileId, Guid.Parse(dataServiceId));

                // Check if the deletionResult contains an error (assuming Error is a string)
                if (!deletionResult.Success)
                {
                    _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, removeDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);
                    return NotFound(new ErrorMessage
                    {
                        Code = errorDM00015,
                        Message = "An internal server error occurred while deleting the data service.",
                        Errors = [new InnerError { Detail = deletionResult.Error! }]
                    });
                }

                // If deletion is successful, return 204 No Content
                return NoContent();
            }
            catch (UnAuthorizedAccessToDataAssetException ex)
            {
                _logger.LogError(ex, $"Unauthorized access attempted with deletion of data service with ID {dataServiceId}");
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.AccessError, removeDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage
                {
                    Code = "TODO",
                    Message = "TODO",
                    Errors = []
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting data service with ID {DataServiceId}", dataServiceId);
                _appInsightsLogger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, removeDataService, "CDDO", error, ingestionApiError, dataServiceId, userEventProperties);

                return StatusCode(500, new ErrorMessage
                {
                    Code = errorDM00011,
                    Message = $"Internal server error occurred while deleting data service with ID {dataServiceId}.",
                    Errors = [new InnerError { Detail = ex.Message }]
                });
            }
        }

        private async Task<IUserDetails?> DoGetInitiatingUserDetailsAsync()
        {
            var initiatingUserDetails = await _users.GetInitiatingUserDetailsAsync();

            if (initiatingUserDetails == null)
            {
                _logger.LogError("Unable to get user details for initiating user");
            }

            return initiatingUserDetails;
        }

       
    }
}

