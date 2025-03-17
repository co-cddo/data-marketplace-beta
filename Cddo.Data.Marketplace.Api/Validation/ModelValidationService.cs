using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Schema;
using System.Text.RegularExpressions;
using Cddo.Data.Marketplace.Audit;
using Agm.Catalog.DotNet.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;

namespace Cddo.Data.Marketplace.Api.Validation
{
    public class ModelValidationService : IModelValidationService
    {
        private readonly IAppInsightsLogger _appInsightsLogger;
        private readonly IEnumMemberConverter _enumMemberConverter;
        private const string trigger404 = "trigger-404";
        private const string trigger500 = "trigger-500";
        private const string errorDM00010 = "DM00010";
        private const string errorDM00011 = "DM00011";
        private const string errorDM00012 = "DM00012";
        private const string errorDM00014 = "DM00014";
        private const string errorDM00015 = "DM00015";
        private const string validationFailureMessage = "Validation failures";
        private const string validationError = "ValidationError";
        private const string errorMessage = "Simulated internal server error for sandbox testing.";
        private const string applicationError = "ApplicationError";
        private const string sandbox = "Sandbox";

        public ModelValidationService(IAppInsightsLogger appInsightsLogger, IEnumMemberConverter enumMemberConverter)
        {
            _appInsightsLogger = appInsightsLogger;
            _enumMemberConverter = enumMemberConverter;
        }
        public ErrorMessage? RecordModelStateErrorsAndBuildErrorResponse(
            ActionContext context,
            IUserDetails initiatingUserDetails)
        {
            var modelStateEntryErrors =
                context.ModelState.Where(x => x.Value?.Errors.Any() == true).ToList();

            var innerErrors = new List<InnerError>();

            foreach (var modelStateEntryError in modelStateEntryErrors)
            {
                var propertyPath = modelStateEntryError.Key;

                var modelErrors = modelStateEntryError.Value!.Errors;
                if (!modelErrors.Any()) continue;

                foreach (var modelError in modelErrors)
                {
                    var catalogAssetField = MapDataAssetPropertyPathToCatalogAssetField(propertyPath);

                    DoLogValidationError(propertyPath, catalogAssetField, modelError);

                    innerErrors.Add(DoBuildInnerError(propertyPath, catalogAssetField, modelError));
                }
            }

            if (!innerErrors.Any()) return null;

            return new ErrorMessage
            {
                Code = errorDM00012,
                Message = validationFailureMessage,
                Errors = innerErrors
            };

            CatalogAssetField? MapDataAssetPropertyPathToCatalogAssetField(
                string dataAssetJsonPropertyName)
            {
                var dataAssetPropertyName = SanitisePropertyName();

                switch (dataAssetPropertyName.ToLower())
                {
                    // CataloguedResource
                    case var s when s == nameof(CataloguedResource.Type).ToLower(): return CatalogAssetField.DataAssetType;
                    case var s when s == nameof(CataloguedResource.Identifier).ToLower(): return CatalogAssetField.DataAssetId;
                    case var s when s == nameof(CataloguedResource.Title).ToLower(): return CatalogAssetField.Title;
                    case var s when s == nameof(CataloguedResource.AccessRights).ToLower(): return CatalogAssetField.AccessRights;
                    case var s when s == $"{nameof(CataloguedResource.ContactPoint)}.{nameof(Contact.Name)}".ToLower(): return CatalogAssetField.ContactPointName;
                    case var s when s == $"{nameof(CataloguedResource.ContactPoint)}.{nameof(Contact.Email)}".ToLower(): return CatalogAssetField.ContactPointEmailAddress;
                    case var s when s == nameof(CataloguedResource.Description).ToLower(): return CatalogAssetField.Description;
                    case var s when s == nameof(CataloguedResource.Keyword).ToLower(): return CatalogAssetField.Keywords;
                    case var s when s == nameof(CataloguedResource.Modified).ToLower(): return CatalogAssetField.Modified;
                    case var s when s == nameof(CataloguedResource.Publisher).ToLower(): return CatalogAssetField.Publisher;
                    case var s when s == nameof(CataloguedResource.SecurityClassification).ToLower(): return CatalogAssetField.SecurityClassification;
                    case var s when s == nameof(CataloguedResource.Status).ToLower(): return CatalogAssetField.DataAssetStatus;
                    case var s when s == nameof(CataloguedResource.Theme).ToLower(): return CatalogAssetField.Themes;

                    // DataSet Specifics
                    case var s when s == $"{nameof(DataSet.Distribution)}.{nameof(Distribution.AccessService)}".ToLower(): return CatalogAssetField.DistributionAccessService;
                    case var s when s == $"{nameof(DataSet.Distribution)}.{nameof(Distribution.DownloadUrl)}".ToLower(): return CatalogAssetField.DistributionDownloadUrl;
                    case var s when s == $"{nameof(DataSet.Distribution)}.{nameof(Distribution.MediaType)}".ToLower(): return CatalogAssetField.DistributionMediaType;
                    case var s when s == nameof(DataSet.Issued).ToLower(): return CatalogAssetField.Issued;
                    case var s when s == nameof(DataSet.UpdateFrequency).ToLower(): return CatalogAssetField.UpdateFrequency;

                    // DataService Specifics
                    case var s when s == nameof(DataService.ApiType).ToLower(): return CatalogAssetField.ApiType;
                    case var s when s == nameof(DataService.EndpointDescription).ToLower(): return CatalogAssetField.EndpointDescription;
                    case var s when s == nameof(DataService.EndpointUrl).ToLower(): return CatalogAssetField.EndpointUrl;
                    case var s when s == nameof(DataService.ServesDataset).ToLower(): return CatalogAssetField.ServesData;
                    case var s when s == nameof(DataService.ServiceType).ToLower(): return CatalogAssetField.ServiceType;

                    default: return null;
                }

                string SanitisePropertyName()
                {
                    var pathWithoutIndices = Regex.Replace(dataAssetJsonPropertyName, @"\[\d*\]", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(500));

                    var pathWithoutPrefix = pathWithoutIndices.StartsWith("$.")
                        ? pathWithoutIndices[2..]
                        : pathWithoutIndices;

                    return pathWithoutPrefix;
                }
            }

            void DoLogValidationError(
                string propertyName,
                CatalogAssetField? catalogAssetField,
                ModelError modelError)
            {
                if (!catalogAssetField.HasValue) return;

                var errorType = MapErrorToValidationErrorType(modelError);
                if (!errorType.HasValue) return;

                _appInsightsLogger.LogDataAssetValidationErrorBase(
                    catalogAssetField: catalogAssetField.Value,
                    actionSource: DataAssetActionSourceEnum.Api,
                    dataAssetPropertyName: propertyName,
                    errorMessage: modelError.ErrorMessage,
                    errorSeverity: DataAssetValidationPropertyErrorSeverity.Blocking,
                    errorType: errorType.Value,
                    initiatingUserDetails: initiatingUserDetails);
            }

            DataAssetPropertyValidationErrorType? MapErrorToValidationErrorType(
                ModelError modelError)
            {
                var message = modelError.ErrorMessage;

                switch (message)
                {
                    case var s when s.StartsWith("Unknown enum value"): return DataAssetPropertyValidationErrorType.EnumValueHasInvalidFormat;
                    case var s when s.StartsWith("The JSON value could not be converted to System.Nullable`1[System.DateTime]"): return DataAssetPropertyValidationErrorType.DateTimeValueHasInvalidFormat;
                }

                return null;
            }

            InnerError DoBuildInnerError(
                string? propertyPath,
                CatalogAssetField? catalogAssetField,
                ModelError modelError)
            {
                var message = catalogAssetField.HasValue
                    ? $"{_enumMemberConverter.GetEnumMemberValue(catalogAssetField)} - {modelError.ErrorMessage}"
                    : modelError.ErrorMessage;

                return new InnerError
                {
                    Detail = message,
                    Type = validationError,
                    Location = propertyPath
                };
            }
        }

        public ErrorMessage? RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(
           IEnumerable<IDataAssetValidationPropertyResult> validationPropertyResults,
           IUserDetails initiatingUserDetails)
        {
            var innerErrors = new List<InnerError>();

            foreach (var validationPropertyResult in validationPropertyResults)
            {
                var dataAssetValidationPropertyErrors = validationPropertyResult.Errors.ToList();

                if (!dataAssetValidationPropertyErrors.Any()) continue;

                foreach (var dataAssetValidationPropertyError in dataAssetValidationPropertyErrors)
                {
                    DoLogValidationError(validationPropertyResult.PropertyName, dataAssetValidationPropertyError);

                    innerErrors.Add(DoBuildInnerError(dataAssetValidationPropertyError));
                }
            }

            if (!innerErrors.Any()) return null;

            return new ErrorMessage
            {
                Code = errorDM00012,
                Message = validationFailureMessage,
                Errors = innerErrors
            };

            void DoLogValidationError(
                string propertyName,
                IDataAssetValidationPropertyError propertyError)
            {
                _appInsightsLogger.LogDataAssetValidationErrorBase(
                    catalogAssetField: propertyError.CatalogAssetField,
                    actionSource: DataAssetActionSourceEnum.Api,
                    dataAssetPropertyName: propertyName,
                    errorMessage: propertyError.Error,
                    errorSeverity: propertyError.ErrorSeverity,
                    errorType: propertyError.ErrorType,
                    initiatingUserDetails: initiatingUserDetails);
            }

            InnerError DoBuildInnerError(IDataAssetValidationPropertyError propertyError)
            {
                var propertyDescription = _enumMemberConverter.GetEnumMemberValue(propertyError.CatalogAssetField);

                var message = $"{propertyDescription} - {propertyError.Error}";

                return new InnerError
                {
                    Detail = message,
                    Type = validationError
                };
            }
        }

        public (int, ErrorMessage)? HandleSimulatedErrors(CataloguedResource? dataset, string? datasetId, bool isDataset)
        {
            var datasetText = "Dataset";
            if (!isDataset)
            {
                datasetText = "Data service";
            }

            var datasetIdErrorMappings = new Dictionary<string, (int StatusCode, string ErrorCode, string Message, string Detail, string ErrorType)>
            {
                { "trigger-404", (404, "DM00010", $"{datasetText} with identifier {datasetId} does not exist.", "Triggered by {datasetText} ID for sandbox testing.", "NotFoundError") },
                { "trigger-500", (500, "DM00011", $"Internal server error occurred while deleting {datasetText} with ID {datasetId}.", "Simulated server-side exception for testing purposes.", applicationError) }
            };


            //Lest check the datasetIds first since the dataset/dataservice can be null
            if (!string.IsNullOrEmpty(datasetId) && datasetIdErrorMappings.TryGetValue(datasetId, out var datasetIdErrorResponse))
            {
                return  (datasetIdErrorResponse.StatusCode, CreateErrorMessage(datasetIdErrorResponse.ErrorCode, datasetIdErrorResponse.Message, datasetIdErrorResponse.Detail, datasetIdErrorResponse.ErrorType));
            }

            //Errormapping
            var errorMappings = new Dictionary<string, (int StatusCode, string ErrorCode, string Message, string ErrorType)>
            {
                { "trigger-400", (400, "DM00012", "Simulated validation error for sandbox testing.", "ValidationError")},
                { "trigger-409", (409, "DM00014", "Simulated conflict error for sandbox testing.", "ConflictError")},
            };

            if (errorMappings.TryGetValue(dataset.Title, out var errorResponse))
            {
                return (errorResponse.StatusCode, CreateErrorMessage(errorResponse.ErrorCode, errorResponse.Message, $"Triggered by {datasetText} title.", errorResponse.ErrorType));
            }

            if (dataset.SupplierIdentifier == "trigger-409")
            {
                var conflictResponse = errorMappings["trigger-409"];
                return (conflictResponse.StatusCode, CreateErrorMessage(conflictResponse.ErrorCode, conflictResponse.Message, "Triggered by supplier identifier.", conflictResponse.ErrorType));
            }

            if (dataset.Title == trigger500)
            {
                return (500, CreateErrorMessage(errorDM00015, errorMessage, $"Triggered by {datasetText} title.", applicationError));
            }

            if (dataset.Identifier != null && dataset.Identifier != datasetId && datasetId != null)
            {
                return (500, CreateErrorMessage(errorDM00012, "Patch model identifier mismatch.", $"The patch model's identifier does not match the dataset ID.", validationError));
            }

                return null;
        }

        public List<CataloguedResource> GetMockedCataloguedResources()
        {
            var mockResponse = new List<CataloguedResource>
                {
                    new()
                    {
                        Type = ResourceEnum.DataSet,
                        Identifier = Guid.NewGuid().ToString(),
                        Title = "Mocked Dataset Title",
                        AccessRights = AccessRightsEnum.Open,
                        ContactPoint = new List<Contact>
                        {
                            new() { Name = "John Doe", Email = "contact@example.com", Role = ContactRoleEnum.Contact }
                        },
                        Description = "This is a mocked dataset description for sandbox.",
                        Keyword = new List<string> { "mock", "dataset", sandbox },
                        Modified = DateTime.UtcNow.AddDays(-1),
                        Publisher = "Mock Publisher",
                        SecurityClassification = SecurityClassificationEnum.Official,
                        Status = ResourceStatusEnum.Published,
                        SupplierIdentifier = "supplier-12345",
                        License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                        { 
                            Title = "Open Data Set",
                            LicenseUrl = "https://openlicense.gov.uk"
                        },
                        Theme = new List<string> { nameof(ThemeEnum.Education), nameof(ThemeEnum.ScienceAndTechnology) }
                    },
                    new()
                    {
                        Type = ResourceEnum.DataService,
                        Identifier = Guid.NewGuid().ToString(),
                        Title = "Mocked Data Service Title",
                        AccessRights = AccessRightsEnum.Restricted,
                        ContactPoint = new List<Contact>
                        {
                            new() { Name = "Jane Smith", Email = "servicecontact@example.com", Role = ContactRoleEnum.Contact }
                        },
                        Description = "This is a mocked data service description for sandbox.",
                        Keyword = new List<string> { "mock", "data-service", sandbox },
                        Modified = DateTime.UtcNow.AddDays(-3),
                        Publisher = "Another Mock Publisher",
                        SecurityClassification = SecurityClassificationEnum.Official,
                        Status = ResourceStatusEnum.Draft,
                        SupplierIdentifier = "supplier-67890",
                        Theme = new List<string> { nameof(ThemeEnum.HealthAndCare), nameof(ThemeEnum.TransportAndInfrastructure) }
                    }
                };

            return mockResponse;

        }

        public DataSet GetMockedDataset(string datasetId)
        {
            var mockDataset = new DataSet
            {
                Type = ResourceEnum.DataSet,
                Identifier = datasetId,
                Title = "Mocked Dataset Title",
                AccessRights = AccessRightsEnum.Open,
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mock Dataset Owner", Email = "owner@example.com", Role = ContactRoleEnum.Contact }
                    },
                Description = "This is a mocked dataset description tailored for the sandbox environment.",
                Keyword = new List<string> { sandbox, "mocked", "dataset" },
                Modified = DateTime.UtcNow.AddDays(-7),
                Publisher = "Mock Dataset Publisher",
                SecurityClassification = SecurityClassificationEnum.Official,
                Status = ResourceStatusEnum.Published,
                SupplierIdentifier = "supplier-12345-dataset",
                Theme = new List<string> { nameof(ThemeEnum.BusinessEconomicsAndFinance), nameof(ThemeEnum.EnvironmentAndNature) },
                Issued = DateTime.Now,
                Distribution = new List<Distribution>
                    {
                        new()
                        {
                            AccessService = ["17554d2c-7251-4822-8813-872effcc5650"],
                            DownloadUrl = "https://testing.com/api",
                            MediaType = ["application/xml"]
                        }
                    },
                UpdateFrequency = "Yearly",
                License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                {
                    Title = "Open Data Set",
                    LicenseUrl = "https://openlicense.gov.uk"
                },
            };

            return mockDataset;
        }

        public DataSet GetMockedUpdatedDataset(string datasetId, DataSet patchModel)
        {
            var updatedDataset = new DataSet
            {
                Identifier = datasetId,
                Title = patchModel.Title ?? "Mocked Updated Dataset",
                Description = patchModel.Description ?? "This is a mocked description for the sandbox environment.",
                Status = patchModel.Status ?? ResourceStatusEnum.Published,
                SupplierIdentifier = patchModel.SupplierIdentifier ?? "mocked-supplier-123",
                Modified = DateTime.Now,
                Keyword = new List<string> { sandbox, "mock", "updated" },
                Publisher = patchModel.Publisher ?? "Mocked Publisher",
                SecurityClassification = SecurityClassificationEnum.Official,
                Type = ResourceEnum.DataSet,
                Theme = new List<string> { nameof(ThemeEnum.BusinessEconomicsAndFinance), nameof(ThemeEnum.EnvironmentAndNature) },
                Issued = DateTime.Now,
                License = new Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.License
                {
                    Title = "Open Data Set",
                    LicenseUrl = "https://openlicense.gov.uk"
                },
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mock Dataset Owner", Email = "owner@example.com", Role = ContactRoleEnum.Contact }
                    },
                Distribution = new List<Distribution>
                    {
                        new()
                        {
                            AccessService = ["17554d2c-7251-4822-8813-872effcc5650"],
                            DownloadUrl = "https://testing.com/api",
                            MediaType = ["application/xml"],
                        }
                    },
                AccessRights = AccessRightsEnum.Internal,
                UpdateFrequency = "Yearly"
            };

            return updatedDataset;
        }

        public DataService GetMockedDataServive(string dataServiceId)
        {
            var mockedDataService = new DataService
            {
                Identifier = dataServiceId,
                Title = "Mocked Data Service",
                Description = "This is a mocked description for the sandbox environment.",
                Keyword = new List<string> { sandbox, "mock", "data-service" },
                Publisher = "Mocked Publisher",
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mocked Contact", Email = "mocked.contact@example.com", Role = ContactRoleEnum.Contact }
                    },
                AccessRights = AccessRightsEnum.Open,
                Status = ResourceStatusEnum.Published,
                Modified = DateTime.Now,
                Type = ResourceEnum.DataService,
                Theme = new List<string> { nameof(ThemeEnum.AgricultureFisheriesAndForestry), nameof(ThemeEnum.BusinessEconomicsAndFinance) },
                SecurityClassification = SecurityClassificationEnum.Official,
                EndpointDescription = "Endpoint Description",
                EndpointUrl = "https://testingurl.com",
                ServesDataset = new List<string> { "1da70e32-2762-465e-b00a-28664af24264" },
                ApiType = ApiTypeEnum.Rest,
                ServiceType = ServiceTypeEnum.Transactional
            };

            return mockedDataService;
        }

        public DataService GetMockedUpdatedDataService(string dataServiceId)
        {
            var updatedDataService = new DataService
            {
                Identifier = dataServiceId,
                Title = "Mocked Data Service",
                Description = "This is a mocked description for the sandbox environment.",
                Keyword = new List<string> { sandbox, "mock", "data-service" },
                Publisher = "Mocked Publisher",
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mocked Contact", Email = "mocked.contact@example.com", Role = ContactRoleEnum.Owner }
                    },
                AccessRights = AccessRightsEnum.Open,
                Status = ResourceStatusEnum.Published,
                Modified = DateTime.UtcNow,
                Type = ResourceEnum.DataService,
                Theme = new List<string> { nameof(ThemeEnum.AgricultureFisheriesAndForestry), nameof(ThemeEnum.BusinessEconomicsAndFinance) },
                SecurityClassification = SecurityClassificationEnum.Official,
                EndpointDescription = "Endpoint Description",
                EndpointUrl = "https://testingurl.com",
                ServesDataset = new List<string> { "1da70e32-2762-465e-b00a-28664af24264" },
                ApiType = ApiTypeEnum.Rest,
                ServiceType = ServiceTypeEnum.Transactional
            };

            return updatedDataService;
        }

        private ErrorMessage CreateErrorMessage(string code, string message, string detail, string type)
        {
            return new ErrorMessage
            {
                Code = code,
                Message = message,
                Errors = [new InnerError { Detail = detail, Type = type }]
            };
        }
    }
}
