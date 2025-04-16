using System.ComponentModel;
using System.Net;
using System.Text.Json;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.Ckan;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Configuration;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetConversion;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetMigration;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Duplication;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Exceptions;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.SpreadsheetIngestion.Validation;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Validation;
using Agm.Catalog.DotNet.Logic.Services.EmbeddedResourceProvision;
using Agm.Catalog.DotNet.Logic.Services.Lookup.Results;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.Users.Conversion;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cddo.Data.Marketplace.Logic.Services.DataAssets;

public class DataAssetService(
    ILogger<DataAssetService> logger,
    IProfiledDataAssetConverterPresenter profiledDataAssetConverterPresenter,
    ICddoDataAssetConverter cddoDataAssetConverter,
    ICkanConnection ckanConnection,
    IValidatedProfiledDataAssetSpreadsheetContentStore validatedProfiledDataAssetSpreadsheetContentStore,
    IEmbeddedResourcesProvider embeddedResourcesProvider,
    IProfiledDataAssetsMigrationV1p0ToV3p1 profiledDataAssetsMigrationV1P0ToV3P1,
    IServiceOperationResultFactory serviceOperationResultFactory,
    IDataAssetDuplicationDetermination dataAssetDuplicationDetermination,
    ICkanConfigurationPresenter ckanConfigurationPresenter,
    IAppInsightsLogger appInsightsLogger,
    IAgmUserInformationBuilder agmUserInformationBuilder)
    : IDataAssetService
{
    async Task<IServiceOperationDataResult<IAddDataAssetResult>> IDataAssetService.AddProfiledDataAssetAsync(
        IUserDetails initiatingUserDetails,
        string profileId,
        DataAssetType dataAssetType,
        string payload,
        ManagementMetadataBase managementMetadata,
        DataAssetActionSourceEnum actionSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        if (!Enum.IsDefined(typeof(DataAssetType), dataAssetType))
            throw new InvalidEnumArgumentException(nameof(dataAssetType), (int)dataAssetType, typeof(DataAssetType));
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentNullException.ThrowIfNull(managementMetadata);
        if (!Enum.IsDefined(typeof(DataAssetActionSourceEnum), actionSource))
            throw new InvalidEnumArgumentException(nameof(actionSource), (int)actionSource, typeof(DataAssetActionSourceEnum));

        try
        {
            var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var profiledDataAsset = new ProfiledDataAsset
            {
                ProfileId = profileId,
                DataAssetType = dataAssetType,
                Payload = payload,
                ManagementMetadata = managementMetadata
            };

            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            var ckanCatalogEntryWrite = profileDataAssetConverter.ConvertProfiledDataAssetPayloadToCkanCatalogEntryWrite(
                profiledDataAsset,
                agmUserDetails,
                null);

            var createdDataAssetId = await ckanConnection.AddCatalogEntryAsync(
                ckanCatalogEntryWrite);

            var dataAssetValidation = profileDataAssetConverter.GetDataAssetValidation();

            // If the call to add a profiled data asset has come from the UI then it will more than likely not contain
            // all properties required in a final asset, as values are written in stages.  So we don't include 'not provided'
            // errors in the results for that route.  For the other routes we assume that all data will be provided in
            // one shot, so we check for missing required properties
            var includeRequiredPropertiesInValidation = actionSource != DataAssetActionSourceEnum.UserInterface;

            var dataAssetValidationResult = dataAssetValidation.ValidateCkanCatalogEntryWrite(
                ckanCatalogEntryWrite, dataAssetType, includeRequiredPropertiesInValidation);

            DoRecordDataAssetUpdateValidationErrors(actionSource, dataAssetValidationResult, initiatingUserDetails);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new AddDataAssetResult
            {
                DataAssetId = createdDataAssetId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Add Profiled Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IAddDataAssetResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IPatchDataAssetResult>> IDataAssetService.PatchProfiledDataAssetAsync(
        IUserDetails initiatingUserDetails,
        string profileId,
        DataAssetType dataAssetType,
        string? payload,
        ManagementMetadataBase? managementMetadata,
        DataAssetActionSourceEnum actionSource)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        if (!Enum.IsDefined(typeof(DataAssetType), dataAssetType))
            throw new InvalidEnumArgumentException(nameof(dataAssetType), (int)dataAssetType, typeof(DataAssetType));
        if (!Enum.IsDefined(typeof(DataAssetActionSourceEnum), actionSource))
            throw new InvalidEnumArgumentException(nameof(actionSource), (int)actionSource, typeof(DataAssetActionSourceEnum));

        try
        {
            var profileDataAssetConverter =
                profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var ckanCatalogEntryWrite =
                profileDataAssetConverter.ConvertProfiledPartialDataAssetPayloadToCkanCatalogEntryWrite(
                    new ProfiledPartialDataAsset
                    {
                        ProfileId = profileId,
                        DataAssetType = dataAssetType,
                        Payload = payload,
                        ManagementMetadata = managementMetadata
                    });

            var existingCkanCatalogEntry = await ckanConnection.GetCatalogEntryWithProfileAsync(
                profileId,
                ckanCatalogEntryWrite.Id!.Value,
                new CatalogEntriesOrganisationFilter
                {
                    FilterByOrganisationDiscoverability = false,
                    FilterByOrganisationOwnership = false
                });

            var existingCddoDataAsset =
                profileDataAssetConverter.ConvertCkanCatalogEntryReadToCddoDataAsset(existingCkanCatalogEntry);

            if (!DoesInitiatingUserHaveAuthorityToAffectCkanCatalogEntry(initiatingUserDetails, existingCddoDataAsset))
            {
                throw new UnAuthorizedAccessToDataAssetException();
            }

            var createdDataAssetId = await ckanConnection.PatchCatalogEntryAsync(
                ckanCatalogEntryWrite,
                existingCkanCatalogEntry);

            var dataAssetValidation = profileDataAssetConverter.GetDataAssetValidation();

            // Validate the properties that were provided on the patched asset, and record any errors.
            // Do not include missing properties, because this is a patch, so not all properties must
            // be provided

            var dataAssetValidationResult = dataAssetValidation.ValidateCkanCatalogEntryWrite(
                ckanCatalogEntryWrite, existingCddoDataAsset.DataAssetType, false);

            DoRecordDataAssetUpdateValidationErrors(actionSource, dataAssetValidationResult, initiatingUserDetails);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new PatchDataAssetResult
            {
                DataAssetId = createdDataAssetId
            });
        }
        catch (ProfiledDataAssetNotFoundException ex)
        {
            logger.LogError(ex, "Unable to Patch Unknown Profiled Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IPatchDataAssetResult>(ex.Message, statusCode: HttpStatusCode.NotFound);

            return await Task.FromResult(response);
        }
        catch (UnAuthorizedAccessToDataAssetException ex)
        {
            logger.LogError(ex, "User is not authorized to Patch Profiled Data Asset");

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Patch Profiled Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IPatchDataAssetResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IDeleteDataAssetResult>> IDataAssetService.DeleteProfiledDataAssetAsync(
        IUserDetails initiatingUserDetails,
        string profileId,
        Guid dataAssetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        if (dataAssetId == Guid.Empty)
            throw new ArgumentException(ErrorMessages.InvalidDataAssetId, nameof(dataAssetId));

        try
        {
            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails, false, false);

            var existingCkanCatalogEntry = await ckanConnection.GetCatalogEntryAsync(
                dataAssetId,
            catalogEntriesOrganisationFilter);

            var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var existingCddoDataAsset =
                profileDataAssetConverter.ConvertCkanCatalogEntryReadToCddoDataAsset(existingCkanCatalogEntry);

            if (!DoesInitiatingUserHaveAuthorityToAffectCkanCatalogEntry(initiatingUserDetails, existingCddoDataAsset))
            {
                throw new UnAuthorizedAccessToDataAssetException();
            }

            await ckanConnection.DeleteCatalogEntryAsync(dataAssetId);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new DeleteDataAssetResult
            {
                DataAssetId = dataAssetId
            });
        }
        catch (DataAssetNotFoundException ex)
        {
            logger.LogError(ex, "Unable to Delete Unknown Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IDeleteDataAssetResult>(
                ex.Message, statusCode: HttpStatusCode.NotFound);

            return await Task.FromResult(response);
        }
        catch (UnAuthorizedAccessToDataAssetException ex)
        {
            logger.LogError(ex, "User is not authorized to Patch Profiled Data Asset");
            var response = serviceOperationResultFactory.CreateFailedDataResult<IDeleteDataAssetResult>(
                ex.Message, statusCode: HttpStatusCode.Forbidden);

            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Delete Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IDeleteDataAssetResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetProfiledDataAssetsResult>> IDataAssetService.GetProfiledDataAssetsAsync(
        IUserDetails initiatingUserDetails,
        string profileId,
        IEnumerable<DataAssetType> dataAssetTypes,
        bool? onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
        bool? onlyIncludeRecordsOwnedByOrganisationOfCallingUser,
        string? searchText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        try
        {
            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails,
                onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser == true,
                onlyIncludeRecordsOwnedByOrganisationOfCallingUser == true);

            var ckanCatalogEntrySet = await ckanConnection.GetCatalogEntriesWithProfileAsync(
                profileId,
                dataAssetTypes,
                catalogEntriesOrganisationFilter,
                searchText);

            var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var profiledDataAssets =
                profileDataAssetConverter.ConvertCkanCatalogEntryReadsToProfiledDataAssets(ckanCatalogEntrySet.Results);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetProfiledDataAssetsResult
            {
                TotalNumberOfMatchingProfiledDataAssets = ckanCatalogEntrySet.Count,
                ProfiledDataAssets = profiledDataAssets.Cast<ProfiledDataAsset>()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Profiled Data Assets");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetProfiledDataAssetsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetProfiledDataAssetIdsResult>> IDataAssetService.GetProfiledDataAssetIdsAsync(
        IUserDetails initiatingUserDetails,
        string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        try
        {
            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails, false, false);

            var ckanCatalogEntrySet = await ckanConnection.GetCatalogEntriesWithProfileAsync(
                profileId,
                Enum.GetValues<DataAssetType>(),
                catalogEntriesOrganisationFilter,
                null);

            var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var profiledDataAssetIds = profileDataAssetConverter
                .ConvertCkanCatalogEntryReadsToProfiledDataAssetIds(ckanCatalogEntrySet.Results)
                .ToList();

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetProfiledDataAssetIdsResult
            {
                TotalNumberOfMatchingProfiledDataAssetIds = profiledDataAssetIds.Count,
                ProfiledDataAssetIds = profiledDataAssetIds
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Profiled Data Asset Ids");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetProfiledDataAssetIdsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetProfiledDataAssetResult>> IDataAssetService.GetProfiledDataAssetAsync(
        IUserDetails initiatingUserDetails,
        string profileId,
        Guid dataAssetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        if (dataAssetId == Guid.Empty)
            throw new ArgumentException(ErrorMessages.InvalidDataAssetId, nameof(dataAssetId));

        try
        {
            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails, false, false);

            // TODO: IMPLEMENT: Stubbed out the call to CkanConnection.GetCatalogEntryWithProfileAsync
            //var ckanCatalogEntry = await ckanConnection.GetCatalogEntryWithProfileAsync(
            //    profileId,
            //    dataAssetId,
            //    catalogEntriesOrganisationFilter);

            var ckanCatalogEntryReadStub = new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryRead
            {
                Id = Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d23"),
                Title = "Sample Data Asset",
          
                Extras = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead>
                {
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                    {
                        Key = "profileId",
                        Value = "dcat-ukap-v3.1"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                    {
                        Key = "apiType",
                        Value = "REST"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                    {
                        Key = "serviceType",
                        Value = "Transactional"
                    }
                },
                Tags = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead>
                {
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                    {
                        Name = "Sample",
                        DisplayName = "Sample Tag",
                        State = "active"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                    {
                        Name = "Data",
                        DisplayName = "Data Tag",
                        State = "active"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                    {
                        Name = "Asset",
                        DisplayName = "Asset Tag",
                        State = "active"
                    }
                }
                  
           
            };
            var ckanCatalogEntry = ckanCatalogEntryReadStub;
            // END Stub

            var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var profiledDataAsset = profileDataAssetConverter.ConvertCkanCatalogEntryReadToProfiledDataAsset(ckanCatalogEntry);
            
            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetProfiledDataAssetResult
            {
                ProfiledDataAsset = (ProfiledDataAsset)profiledDataAsset
            });
        }
        catch (ProfiledDataAssetNotFoundException ex)
        {
            logger.LogError(ex, "Unable to Get Unknown Profiled Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetProfiledDataAssetResult>(ex.Message, statusCode: HttpStatusCode.NotFound);

            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Profiled Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetProfiledDataAssetResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetCddoDataAssetsResult>> IDataAssetService.GetCddoDataAssetsAsync(
        IUserDetails initiatingUserDetails,
        bool? onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
        bool? onlyIncludeRecordsOwnedByOrganisationOfCallingUser,
        IEnumerable<DataAssetType>? dataAssetTypes,
        IEnumerable<DataAssetStatus>? dataAssetStatuses,
        int startIndex,
        int numberOfAssets,
        DataAssetsSortField sortField,
        DataAssetsSortDirection sortDirection,
        string? searchText,
        IEnumerable<string>? publishers,
        IEnumerable<string>? themes,
        IEnumerable<string>? entryTypes,
        IEnumerable<string>? accessRights)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfAssets);

        if (!Enum.IsDefined(typeof(DataAssetsSortField), sortField))
            throw new InvalidEnumArgumentException(nameof(sortField), (int)sortField, typeof(DataAssetsSortField));

        if (!Enum.IsDefined(typeof(DataAssetsSortDirection), sortDirection))
            throw new InvalidEnumArgumentException(nameof(sortDirection), (int)sortDirection, typeof(DataAssetsSortDirection));

        try
        {
            var resultPagination = new CatalogEntriesResultPagination
            {
                StartIndex = startIndex,
                NumberOfAssets = numberOfAssets,
                SortField = sortField,
                SortDirection = sortDirection
            };

            var lookupTokens = new CatalogEntryLookupTokens
            {
                SearchText = searchText,
                Publishers = publishers,
                Themes = themes,
                EntryTypes = entryTypes,
                AccessRights = accessRights
            };

            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails,
                onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser == true,
                onlyIncludeRecordsOwnedByOrganisationOfCallingUser == true);

            // TODO: IMPLEMENT: Stubbed out the call to CkanConnection.GetCatalogEntriesAsync

            //var ckanCatalogEntrySet = await ckanConnection.GetCatalogEntriesAsync(
            //     dataAssetTypes,
            //     dataAssetStatuses,
            //     resultPagination,
            //     catalogEntriesOrganisationFilter,
            //     lookupTokens);

            var ckanCatalogEntrySet = new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanPackageSearchResponseResultSet();
            var ckanPackageSearchResponseResultSetStub = new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanPackageSearchResponseResultSet
            {
                Results = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryRead>
                {
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryRead
                    {
                        Id = Guid.NewGuid(),
                        Title = "Sample Title",
                        Extras = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead>
                        {
                            new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                            {
                                Key = "SampleKey",
                                Value = "SampleValue"
                            }
                        },
                        Tags = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead>
                        {
                            new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                            {
                                Name = "SampleTag",
                                DisplayName = "Sample Tag Display",
                                State = "active"
                            }
                        }
                    }
                },
                Count = 1
            };
            ckanCatalogEntrySet = ckanPackageSearchResponseResultSetStub;
            // END Stub

            var cddoDataAssets =
                cddoDataAssetConverter.ConvertCkanCatalogEntryReadsToCddoDataAssets(ckanCatalogEntrySet.Results);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetCddoDataAssetsResult
            {
                TotalNumberOfMatchingCddoDataAssets = ckanCatalogEntrySet.Count,
                CddoDataAssets = cddoDataAssets.Cast<CddoDataAsset>()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Cddo Data Assets");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetCddoDataAssetsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetCddoDataAssetResult>> IDataAssetService.GetCddoDataAssetAsync(
        IUserDetails initiatingUserDetails,
        Guid dataAssetId)
    {
        if (dataAssetId == Guid.Empty)
            throw new ArgumentException(ErrorMessages.InvalidDataAssetId, nameof(dataAssetId));

        try
        {
            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails, false, false);

            // TODO: IMPLEMENT: Stubbed out the call to CkanConnection.GetCatalogEntryAsync
            //var ckanCatalogEntry = await ckanConnection.GetCatalogEntryAsync(
            //    dataAssetId,
            //    catalogEntriesOrganisationFilter);

            var ckanCatalogEntryReadStub = new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryRead
            {
                Id = Guid.Parse("8d085327-21b6-4d8b-9705-88faad231d23"),
                Title = "Sample Data Asset",

                Extras = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead>
                {
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                    {
                        Key = "profileId",
                        Value = "dcat-ukap-v3.1"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                    {
                        Key = "apiType",
                        Value = "REST"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryExtraRead
                    {
                        Key = "serviceType",
                        Value = "Transactional"
                    }
                },
                Tags = new List<Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead>
                {
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                    {
                        Name = "Sample",
                        DisplayName = "Sample Tag",
                        State = "active"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                    {
                        Name = "Data",
                        DisplayName = "Data Tag",
                        State = "active"
                    },
                    new Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch.CkanCatalogEntryTagRead
                    {
                        Name = "Asset",
                        DisplayName = "Asset Tag",
                        State = "active"
                    }
                }


            };

            var ckanCatalogEntry = ckanCatalogEntryReadStub;

           // var cddoDataAsset = cddoDataAssetConverter.ConvertCkanCatalogEntryReadToCddoDataAsset(ckanCatalogEntry);

            var cddoDataAsset = new CddoDataAsset
            {
                Id = Guid.NewGuid(),
                Title = "Sample Data Asset",
                OrganisationId = 1,
                DomainId = 1,
                DataAssetType = DataAssetType.DataSet,
                DataAssetContacts = new List< CddoDataAssetContact>
                {
                    new CddoDataAssetContact
                    {
                        Name = "Sample Contact",
                        Email = "contact@example.com",
                        Role = DataAssetContactRoleType.Contact
                    }
                },
                DataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaContactPointEmailAddress,
                CustomDsrNotificationAddress = "notification@example.com"
            };
            // END stub

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetCddoDataAssetResult
            {
                CddoDataAsset = (CddoDataAsset)cddoDataAsset
            });
        }
        catch (DataAssetNotFoundException ex)
        {
            logger.LogError(ex, "Unable to Get Unknown Cddo Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetCddoDataAssetResult>(ex.Message, statusCode: HttpStatusCode.NotFound);

            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Cddo Data Asset");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetCddoDataAssetResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>> IDataAssetService.GetCddoDataAssetValidationPropertyErrorsAsync(
        IUserDetails initiatingUserDetails,
        Guid dataAssetId)
    {
        if (dataAssetId == Guid.Empty)
            throw new ArgumentException(ErrorMessages.InvalidDataAssetId, nameof(dataAssetId));

        try
        {
            var ckanCatalogEntryRead = await ckanConnection.GetCatalogEntryAsync(dataAssetId,
                new CatalogEntriesOrganisationFilter
                {
                    FilterByOrganisationDiscoverability = false,
                    FilterByOrganisationOwnership = false
                })
                ?? throw new InvalidOperationException($"No Ckan data asset found with id '{dataAssetId}'");

            var profileId = ckanCatalogEntryRead.Extras?.FirstOrDefault(x => x.Key == "profileId")?.Value.ToString()
                ?? throw new InvalidOperationException("Ckan Catalog Entry does not have a profile Id");

            var dataAssetConverter =
                profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var dataAssetValidation = dataAssetConverter.GetDataAssetValidation();

            var dataAssetValidationResult = dataAssetValidation.ValidateCkanCatalogEntryRead(ckanCatalogEntryRead);

            var validationPropertyErrors = dataAssetValidationResult.ValidationPropertyResults.Where(result => result.Errors.Any());

            return serviceOperationResultFactory.CreateSuccessfulDataResult(validationPropertyErrors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Cddo Data Asset Validation Property Errors");

            var response = serviceOperationResultFactory
                .CreateFailedDataResult<IEnumerable<IDataAssetValidationPropertyResult>>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetResult>> IDataAssetService.CheckForPotentialDuplicatesToDataAssetAsync(
        IUserDetails initiatingUserDetails,
        Guid dataAssetId)
    {
        var subjectDataAsset = await DoGetSubjectDataAssetAsync();

        var objectDataAssets = await DoGetObjectDataAssetsAsync();

        var potentialDuplicateDataAssetInformation = dataAssetDuplicationDetermination.DeterminePotentialDuplicatesToDataAsset(
            subjectDataAsset,
            objectDataAssets).ToList();

        return serviceOperationResultFactory.CreateSuccessfulDataResult(new CheckForPotentialDuplicatesToDataAssetResult
        {
            PotentialDuplicateDataAssetInformation = potentialDuplicateDataAssetInformation
        });

        async Task<ICddoDataAsset> DoGetSubjectDataAssetAsync()
        {
            var ckanCatalogEntry = await ckanConnection.GetCatalogEntryAsync(
                dataAssetId,
                new CatalogEntriesOrganisationFilter());

            return cddoDataAssetConverter.ConvertCkanCatalogEntryReadToCddoDataAsset(ckanCatalogEntry);
        }

        async Task<IList<ICddoDataAsset>> DoGetObjectDataAssetsAsync()
        {
            var dataAssetTypes = subjectDataAsset.DataAssetType.HasValue
                ? new List<DataAssetType> { subjectDataAsset.DataAssetType.Value }
                : null;

            var dataAssetStatuses = new List<DataAssetStatus> { DataAssetStatus.Published };

            // We don't need any pagination, but it has to be provided to the call
            var resultPagination = new CatalogEntriesResultPagination
            {
                StartIndex = 0,
                NumberOfAssets = int.MaxValue,
                SortField = DataAssetsSortField.Title,
                SortDirection = DataAssetsSortDirection.Descending
            };

            // Only retrieve assets that this organisation owns
            var organisationFilter = new CatalogEntriesOrganisationFilter
            {
                OrganisationId = initiatingUserDetails.UserIdSet.OrganisationId.ToString(),
                FilterByOrganisationOwnership = true
            };

            var lookupTokens = new CatalogEntryLookupTokens
            {
                SearchText = null,
                Publishers = null,
                Themes = null,
                EntryTypes = null
            };

            var ckanCatalogEntrySet = await ckanConnection.GetCatalogEntriesAsync(
                dataAssetTypes: dataAssetTypes,
                dataAssetStatuses: dataAssetStatuses,
                resultPagination: resultPagination,
                catalogEntriesOrganisationFilter: organisationFilter,
                catalogEntryLookupTokens: lookupTokens);

            var dataAssets = cddoDataAssetConverter
                .ConvertCkanCatalogEntryReadsToCddoDataAssets(ckanCatalogEntrySet.Results);

            return dataAssets
                .Where(x => x.Id != subjectDataAsset.Id)
                .ToList();
        }
    }

    async Task<IServiceOperationDataResult<IGetCddoOrganisationsResult>> IDataAssetService.GetCddoOrganisationsAsync(
        IUserDetails initiatingUserDetails,
        IEnumerable<DataAssetStatus>? dataAssetStatuses)
    {
        try
        {
            var allOrganisations = await ckanConnection.GetCatalogOrganisationsAsync(
                dataAssetStatuses ?? []);

            var aggregatedOrganisations = allOrganisations.Distinct().ToList();

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetCddoOrganisationsResult
            {
                Organisations = aggregatedOrganisations
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Cddo Organisations");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetCddoOrganisationsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetCddoTopicsResult>> IDataAssetService.GetCddoTopicsAsync(
        IUserDetails initiatingUserDetails,
        IEnumerable<DataAssetStatus>? dataAssetStatuses)
    {
        try
        {
            var allTopics = await ckanConnection.GetCatalogTopicsAsync(
                dataAssetStatuses ?? []);

            var aggregatedTopics = allTopics.Distinct().ToList();

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetCddoTopicsResult
            {
                Topics = aggregatedTopics
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Topics");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetCddoTopicsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>> IDataAssetService.GetSearchSuggestionsForPublishedDataAssetsAsync(
        IUserDetails initiatingUserDetails,
        string searchText)
    {
        try
        {
            var searchSuggestionsForPublishedDataAssets = await DoGetSearchSuggestionsAsync(
                initiatingUserDetails, searchText, includeOnlyPublishedAssets: true);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetSearchSuggestionsForPublishedDataAssetsResult
            {
                SearchSuggestionsForPublishedDataAssets = searchSuggestionsForPublishedDataAssets
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Search Suggestions For Published Data Assets");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>> IDataAssetService.GetSearchSuggestionsForOrganisationDataAssetsAsync(
        IUserDetails initiatingUserDetails,
        string searchText)
    {
        try
        {
            var searchSuggestionsForOrganisationDataAssets = await DoGetSearchSuggestionsAsync(
                initiatingUserDetails, searchText, includeOnlyAssetsManagedByOrganisationOfInitiatingUser: true);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new GetSearchSuggestionsForOrganisationDataAssetsResult
            {
                SearchSuggestionsForOrganisationDataAssets = searchSuggestionsForOrganisationDataAssets
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Search Suggestions For Organisation Data Assets");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    private async Task<IEnumerable<string>> DoGetSearchSuggestionsAsync(
        IUserDetails initiatingUserDetails,
        string searchText,
        bool includeOnlyPublishedAssets = false,
        bool includeOnlyAssetsManagedByOrganisationOfInitiatingUser = false)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return [];

        var organisationId = includeOnlyAssetsManagedByOrganisationOfInitiatingUser
            ? initiatingUserDetails.UserIdSet.OrganisationId
            : (int?)null;

        var dataAssetStatuses = includeOnlyPublishedAssets
            ? new List<DataAssetStatus> { DataAssetStatus.Published }
            : [];

        var ckanSearchSuggestionsResponse = await ckanConnection.GetSearchSuggestionsAsync(
            searchText,
            organisationId,
            dataAssetStatuses);

        return ExtractSearchSuggestionsFromResponse();

        IEnumerable<string> ExtractSearchSuggestionsFromResponse()
        {
            var responseTitles = ckanSearchSuggestionsResponse
                .Response?.Docs?.Select(x => x.Title) ?? [];

            var titles = responseTitles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!);

            var distinctTitles = titles.Distinct();

            var maximumNumberOfSearchSuggestions = ckanConfigurationPresenter.GetSolrMaximumNumberOfSearchSuggestions();

            return distinctTitles.Take(maximumNumberOfSearchSuggestions);
        }
    }


    async Task<IServiceOperationDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>> IDataAssetService.ValidateProfiledDataAssetsSpreadsheetContentAsync(
        IUserDetails initiatingUserDetails,
        IFormFile dataAssetSpreadsheet,
        string dataAssetProfileId)
    {
        ArgumentNullException.ThrowIfNull(dataAssetSpreadsheet);
        ArgumentException.ThrowIfNullOrEmpty(dataAssetProfileId);

        try
        {
            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            await validatedProfiledDataAssetSpreadsheetContentStore.ResetContentForUserAsync(agmUserDetails);

            var profiledDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(dataAssetProfileId);

            var dataAssetSpreadsheetParser = profiledDataAssetConverter.GetDataAssetSpreadsheetParser();
            var parseDataAssetSpreadsheetResult = await dataAssetSpreadsheetParser.ParseDataAssetSpreadsheetAsync(dataAssetSpreadsheet);

            var validatedProfiledDataAssets = parseDataAssetSpreadsheetResult.DataAssetSpreadsheetItems.Select(dataAssetSpreadsheetItem =>
                profiledDataAssetConverter.ConvertDataAssetSpreadsheetItemToValidatedProfiledDataAsset(
                    dataAssetSpreadsheetItem, agmUserDetails)).OfType<ValidatedProfiledDataAsset>().ToList();

            var validatedDataAssetSet = new ValidatedProfiledDataAssetSet
            {
                SpreadsheetName = dataAssetSpreadsheet.FileName,
                ValidatedProfiledDataAssets = validatedProfiledDataAssets
            };

            await validatedProfiledDataAssetSpreadsheetContentStore.StoreValidatedProfiledDataAssetSetForUserAsync(
                agmUserDetails, validatedDataAssetSet);

            foreach (var validatedProfiledDataAsset in validatedProfiledDataAssets)
            {
                DoRecordValidatedProfiledDataAssetValidationErrors(DataAssetActionSourceEnum.SpreadsheetUpload, validatedProfiledDataAsset, initiatingUserDetails);
            }

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new ValidateProfiledDataAssetsSpreadsheetContentResult
            {
                Success = parseDataAssetSpreadsheetResult.Success,
                Errors = parseDataAssetSpreadsheetResult.Errors.ToList(),
                ProfiledDataAssetsSpreadsheetValidationSummary = new ProfiledDataAssetsSpreadsheetValidationSummary
                {
                    SpreadsheetFileName = parseDataAssetSpreadsheetResult.SpreadsheetFileName,
                    ProfiledDataAssetsSpreadsheetItemValidationSummaries = validatedProfiledDataAssets.ToList()
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Validate Profiled Data Assets Spreadsheet Content");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<ValidatedProfiledDataAssetSet?>> IDataAssetService.GetValidatedProfiledDataAssetsSpreadsheetContentAsync(
        IUserDetails initiatingUserDetails)
    {
        try
        {
            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            var validatedProfiledDataAssetSet =
                await validatedProfiledDataAssetSpreadsheetContentStore.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(
                validatedProfiledDataAssetSet);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Validated Profiled Data Assets Spreadsheet Item Content");

            var response = serviceOperationResultFactory.CreateFailedDataResult<ValidatedProfiledDataAssetSet?>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IValidatedProfiledDataAsset>> IDataAssetService.GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(
        IUserDetails initiatingUserDetails,
        string recordId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            var validatedProfiledDataAsset = await validatedProfiledDataAssetSpreadsheetContentStore.GetValidatedProfiledDataAssetForUserAsync(
                agmUserDetails, recordId);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(validatedProfiledDataAsset);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Validated Profiled Data Assets Spreadsheet Item Content");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IValidatedProfiledDataAsset>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>> IDataAssetService.PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(
        IUserDetails initiatingUserDetails,
        PublishValidatedProfiledDataAssetsSpreadsheetContentRequest profiledDataAssetsSpreadsheetContentRequest)
    {
        ArgumentNullException.ThrowIfNull(profiledDataAssetsSpreadsheetContentRequest);
        try
        {
            var publishValidatedProfiledDataAssetsSpreadsheetContentResult = await DoPublishValidatedProfiledDataAssetsSpreadsheetContentAsync();

            return serviceOperationResultFactory.CreateSuccessfulDataResult(publishValidatedProfiledDataAssetsSpreadsheetContentResult);

            async Task<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult> DoPublishValidatedProfiledDataAssetsSpreadsheetContentAsync()
            {
                var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

                var validatedProfiledDataAssetSet = await validatedProfiledDataAssetSpreadsheetContentStore.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails);

                if (!validatedProfiledDataAssetSet?.ValidatedProfiledDataAssets.Any() ?? true)
                {
                    return CreateFailedResult(["Unable to Publish as their is no spreadsheet data available that has been validated for the user"]);
                }

                var validatedProfiledDataAssetsWithRemainingErrors = validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.Where(x => x.ValidationErrors.Any()).ToList();
                if (validatedProfiledDataAssetsWithRemainingErrors.Any())
                {
                    var existingErrors = validatedProfiledDataAssetsWithRemainingErrors.SelectMany(x => x.ValidationErrors.Values).ToList();
                    return CreateFailedResult([$"Unable to Publish as there are {validatedProfiledDataAssetsWithRemainingErrors.Count} records with {existingErrors.Count} validation errors remaining in the spreadsheet dataset for the user"]);
                }

                var publishedValidatedProfiledDataAssetsSpreadsheetContentItems = new List<PublishedValidatedProfiledDataAssetsSpreadsheetContentItem>();
                foreach (var validatedProfiledDataAsset in validatedProfiledDataAssetSet.ValidatedProfiledDataAssets)
                {
                    var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(
                        validatedProfiledDataAsset.ProfiledDataAsset.ProfileId);

                    var ckanCatalogEntry = profileDataAssetConverter.ConvertProfiledDataAssetPayloadToCkanCatalogEntryWrite(
                        validatedProfiledDataAsset.ProfiledDataAsset,
                        agmUserDetails,
                        profiledDataAssetsSpreadsheetContentRequest.DataShareRequestNotificationRecipient);

                    var createdDataAssetId = await ckanConnection.AddCatalogEntryAsync(ckanCatalogEntry);

                    publishedValidatedProfiledDataAssetsSpreadsheetContentItems.Add(new PublishedValidatedProfiledDataAssetsSpreadsheetContentItem
                    {
                        RecordId = validatedProfiledDataAsset.RecordId,
                        AssetTitle = validatedProfiledDataAsset.AssetTitle,
                        DataAssetType = validatedProfiledDataAsset.ProfiledDataAsset.DataAssetType,
                        PublishedId = createdDataAssetId
                    });

                    var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(initiatingUserDetails);
                    appInsightsLogger.LogAdminEventBase(EventTypes.AdminAuditEvent.MetadataSpreadsheetIngestion, "Create Data Asset", "CDDO", "Create", "Data assert add", validatedProfiledDataAsset.RecordId, userEventProperties);
                }

                await validatedProfiledDataAssetSpreadsheetContentStore.ClearContentForUserAsync(agmUserDetails);

                return CreateSuccessfulResult(publishedValidatedProfiledDataAssetsSpreadsheetContentItems);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Publish Validated Profiled Data Assets Spreadsheet Content");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>(ex.Message);

            return await Task.FromResult(response);
        }

        PublishValidatedProfiledDataAssetsSpreadsheetContentResult CreateFailedResult(IEnumerable<string> errors)
        {
            return new PublishValidatedProfiledDataAssetsSpreadsheetContentResult
            {
                Success = false,
                Errors = errors.ToList(),
                PublishedValidatedProfiledDataAssetsSpreadsheetContentItems = []
            };
        }

        PublishValidatedProfiledDataAssetsSpreadsheetContentResult CreateSuccessfulResult(
            IEnumerable<PublishedValidatedProfiledDataAssetsSpreadsheetContentItem> publishedValidatedProfiledDataAssetsSpreadsheetContentItems)
        {
            return new PublishValidatedProfiledDataAssetsSpreadsheetContentResult
            {
                Success = true,
                Errors = [],
                PublishedValidatedProfiledDataAssetsSpreadsheetContentItems = publishedValidatedProfiledDataAssetsSpreadsheetContentItems
            };
        }
    }

    async Task<IServiceOperationResult> IDataAssetService.ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(
        IUserDetails initiatingUserDetails)
    {
        try
        {
            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            await validatedProfiledDataAssetSpreadsheetContentStore.ClearContentForUserAsync(agmUserDetails);

            return serviceOperationResultFactory.CreateSuccessfulResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Clear Validated Profiled Data Assets Spreadsheet Item Content");

            var response = serviceOperationResultFactory.CreateFailedResult(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>> IDataAssetService.CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(
        IUserDetails initiatingUserDetails)
    {
        try
        {
            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            var validatedProfiledDataAssetSet = await validatedProfiledDataAssetSpreadsheetContentStore.GetValidatedProfiledDataAssetSetForUserAsync(
                agmUserDetails);

            if (validatedProfiledDataAssetSet == null || !validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.Any())
                throw new InvalidOperationException("No validated profiled data asset set found for current user");

            var profileId = validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.First().ProfiledDataAsset.ProfileId;

            if (!profiledDataAssetConverterPresenter.TryGetProfiledDataAssetConverterForProfileId(profileId, out var profiledDataAssetConverter))
            {
                throw new InvalidOperationException("Unable to find profiled data asset converter for profile id");
            }

            var spreadsheetDataAssets = validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.Select(validatedProfiledDataAsset =>
            {
                var convertedDataAsset = profiledDataAssetConverter!.ConvertProfiledDataAssetPayloadToCddoDataAsset(validatedProfiledDataAsset.ProfiledDataAsset);

                return new SpreadsheetDataAsset
                {
                    RecordId = validatedProfiledDataAsset.RecordId,
                    DataAsset = convertedDataAsset
                };
            });

            var storedDataAssets = await DoGetStoredDataAssetsForSpreadsheetContentComparisonAsync(
                initiatingUserDetails);

            var potentialDuplicatesToSpreadsheetContent = dataAssetDuplicationDetermination.DeterminePotentialDuplicatesToDataAssetsInSpreadsheet(
                spreadsheetDataAssets,
                storedDataAssets).ToList();

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new CheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult
            {
                PotentialDuplicatesToSpreadsheetContent = potentialDuplicatesToSpreadsheetContent
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Store Data Assets For Spreadsheet Content Comparison");

            var response = serviceOperationResultFactory.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>> IDataAssetService.CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(
        IUserDetails initiatingUserDetails,
        string recordId)
    {
        try
        {
            var agmUserDetails = agmUserInformationBuilder.BuildAgmUserDetails(initiatingUserDetails);

            var validatedProfiledDataAssetSet = await validatedProfiledDataAssetSpreadsheetContentStore.GetValidatedProfiledDataAssetSetForUserAsync(
                agmUserDetails);

            if (validatedProfiledDataAssetSet == null || !validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.Any())
                throw new InvalidOperationException("No validated profiled data asset set found for current user");

            var profileId = validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.First().ProfiledDataAsset.ProfileId;

            if (!profiledDataAssetConverterPresenter.TryGetProfiledDataAssetConverterForProfileId(profileId, out var profiledDataAssetConverter))
            {
                throw new InvalidOperationException("Unable to find profiled data asset converter for profile id");
            }

            var spreadsheetDataAssets = validatedProfiledDataAssetSet.ValidatedProfiledDataAssets.Select(validatedProfiledDataAsset =>
            {
                var convertedDataAsset = profiledDataAssetConverter!.ConvertProfiledDataAssetPayloadToCddoDataAsset(validatedProfiledDataAsset.ProfiledDataAsset);

                return new SpreadsheetDataAsset
                {
                    RecordId = validatedProfiledDataAsset.RecordId,
                    DataAsset = convertedDataAsset
                };
            });

            var storedDataAssets = await DoGetStoredDataAssetsForSpreadsheetContentComparisonAsync(
                initiatingUserDetails);

            var potentialDuplicatesToSpreadsheetItemInformation = dataAssetDuplicationDetermination.DeterminePotentialDuplicatesToDataAssetInSpreadsheet(
                recordId,
                spreadsheetDataAssets,
                storedDataAssets);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new CheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult
            {
                PotentialDuplicatesToSpreadsheetItem = potentialDuplicatesToSpreadsheetItemInformation
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Store Data Assets For Spreadsheet Item Comparison");

            var response = serviceOperationResultFactory.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    private async Task<IList<ICddoDataAsset>> DoGetStoredDataAssetsForSpreadsheetContentComparisonAsync(
        IUserDetails initiatingUserDetails)
    {
        var dataAssetTypes = new List<DataAssetType> { DataAssetType.DataSet };

        var dataAssetStatuses = new List<DataAssetStatus> { DataAssetStatus.Published };

        // We don't need any pagination, but it has to be provided to the call
        var resultPagination = new CatalogEntriesResultPagination
        {
            StartIndex = 0,
            NumberOfAssets = int.MaxValue,
            SortField = DataAssetsSortField.Title,
            SortDirection = DataAssetsSortDirection.Descending
        };

        // Only retrieve assets that this organisation owns
        var organisationFilter = new CatalogEntriesOrganisationFilter
        {
            OrganisationId = initiatingUserDetails.UserIdSet.OrganisationId.ToString(),
            FilterByOrganisationOwnership = true
        };

        var lookupTokens = new CatalogEntryLookupTokens
        {
            SearchText = null,
            Publishers = null,
            Themes = null,
            EntryTypes = null
        };

        var ckanCatalogEntrySet = await ckanConnection.GetCatalogEntriesAsync(
            dataAssetTypes: dataAssetTypes,
            dataAssetStatuses: dataAssetStatuses,
            resultPagination: resultPagination,
            catalogEntriesOrganisationFilter: organisationFilter,
            catalogEntryLookupTokens: lookupTokens);

        return cddoDataAssetConverter
            .ConvertCkanCatalogEntryReadsToCddoDataAssets(ckanCatalogEntrySet.Results)
            .ToList();
    }

    async Task<IServiceOperationDataResult<IEmbeddedResourceData>> IDataAssetService.GetDataAssetTemplateSpreadsheetAsync(
        IUserDetails initiatingUserDetails,
        string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        try
        {
            var profileDataAssetConverter = profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var esdaUploadTemplateSpreadsheetFileName = profileDataAssetConverter.GetDataAssetSpreadsheetParser().GetDataAssetTemplateSpreadsheetFileName();

            var thisAssembly = GetType().Assembly;

            var esdaUploadTemplateSpreadsheetData = embeddedResourcesProvider.GetEmbeddedResourceDataFromAssembly(
                esdaUploadTemplateSpreadsheetFileName, thisAssembly);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new EmbeddedResourceData
            {
                Content = esdaUploadTemplateSpreadsheetData,
                FileName = "Template For Data Asset Descriptions.xlsx",
                ContentType = "application/octet-stream"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Data Asset Template Spreadsheet");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IEmbeddedResourceData>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>> IDataAssetService.MigrateProfiledDataAssetsFrom1p0To3p1Async(
        IUserDetails initiatingUserDetails,
        IEnumerable<Guid> dataAssetIds)
    {
        try
        {
            var migrationResult = await profiledDataAssetsMigrationV1P0ToV3P1.MigrateV1P0DataAssetsAsync(
                dataAssetIds);

            var results = migrationResult.Results.Select(x => new MigrateProfiledDataAssetFrom1p0To3p1Result
            {
                DataAssetV1 = x.DataAssetV1,
                DataAssetV3 = x.DataAssetV3,
                Message = x.Message
            });

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new MigrateProfiledDataAssetsFrom1P0To3P1Result
            {
                Results = results
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Migrate Profiled Data Assets From V1.0 To V3.1");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IGetEsdaOwnershipDetailsResult>> IDataAssetService.GetEsdaOwnershipDetailsAsync(
        IUserDetails initiatingUserDetails,
        Guid dataAssetId)
    {
        try
        {
            var catalogEntriesOrganisationFilter = BuildCatalogEntriesOrganisationFilter(
                initiatingUserDetails, false, false);

            var ckanCatalogEntry = await ckanConnection.GetCatalogEntryAsync(
                dataAssetId,
                catalogEntriesOrganisationFilter);

            var cddoDataAsset = cddoDataAssetConverter.ConvertCkanCatalogEntryReadToCddoDataAsset(ckanCatalogEntry);

            var contactPoint = cddoDataAsset.DataAssetContacts.FirstOrDefault(x => x.Role == DataAssetContactRoleType.Contact)
                ?? cddoDataAsset.DataAssetContacts.FirstOrDefault(x => x.Role == null);

            var getEsdaOwnershipDetailsResult = new GetEsdaOwnershipDetailsResult
            {
                EsdaId = cddoDataAsset.Id,
                Title = cddoDataAsset.Title,
                OrganisationId = cddoDataAsset.OrganisationId,
                DomainId = cddoDataAsset.DomainId,
                ContactPointName = contactPoint?.Name,
                ContactPointEmailAddress = contactPoint?.Email,
                DataShareRequestNotificationRecipientType = cddoDataAsset.DataShareRequestNotificationRecipientType,
                CustomDsrNotificationAddress = cddoDataAsset.CustomDsrNotificationAddress
            };

            return serviceOperationResultFactory.CreateSuccessfulDataResult(getEsdaOwnershipDetailsResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Get Esda Ownership Details");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IGetEsdaOwnershipDetailsResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<IServiceOperationDataResult<IValidateCataloguedResourceResult>> IDataAssetService.ValidateCataloguedResourceAsync(
        string profileId,
        CataloguedResource cataloguedResource,
        DataAssetType dataAssetType,
        bool includeRequiredPropertiesInValidation)
    {
        try
        {
            var profileDataAssetConverter =
                profiledDataAssetConverterPresenter.GetProfiledDataAssetConverterForProfileId(profileId);

            var dataAssetValidation = profileDataAssetConverter.GetDataAssetValidation();

            var dataAssetValidationResult = dataAssetValidation.ValidateCataloguedResource(
                cataloguedResource, dataAssetType, includeRequiredPropertiesInValidation);

            return serviceOperationResultFactory.CreateSuccessfulDataResult(new ValidateCataloguedResourceResult
            {
                DataAssetValidationPropertyResults = dataAssetValidationResult.ValidationPropertyResults
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to Validate Catalogued Resource");

            var response = serviceOperationResultFactory.CreateFailedDataResult<IValidateCataloguedResourceResult>(ex.Message);

            return await Task.FromResult(response);
        }
    }

    async Task<ManagementMetadataDcatUkApV3_1> IDataAssetService.SetMetadataManagement(DataSet dataset, IUserDetails initiatingUserDetails, string profileId)
    {
        var status = dataset.Status switch
        {
            ResourceStatusEnum.Draft => DataAssetStatus.Draft,
            ResourceStatusEnum.Published => DataAssetStatus.Published,
            ResourceStatusEnum.Withdrawn => DataAssetStatus.Withdrawn,
            ResourceStatusEnum.Deleted => DataAssetStatus.Deleted,
            null => throw new ArgumentNullException(nameof(dataset), "Dataset status is null."),
            _ => throw new InvalidEnumArgumentException(nameof(dataset.Status), (int)dataset.Status!, typeof(ResourceStatusEnum))
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

        
        return managementMetadata;
    }

    #region Helpers
    private static ICatalogEntriesOrganisationFilter BuildCatalogEntriesOrganisationFilter(
        IUserDetails initiatingUserDetails,
        bool onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
        bool onlyIncludeRecordsOwnedByOrganisationOfCallingUser)
    {
        var organisationId = initiatingUserDetails.UserIdSet.OrganisationId.ToString();

        var filterByOrganisationDiscoverability = !string.IsNullOrWhiteSpace(organisationId) &&
                                                  onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser;

        return new CatalogEntriesOrganisationFilter
        {
            OrganisationId = organisationId,
            FilterByOrganisationDiscoverability = filterByOrganisationDiscoverability,
            FilterByOrganisationOwnership = onlyIncludeRecordsOwnedByOrganisationOfCallingUser
        };
    }

    private static bool DoesInitiatingUserHaveAuthorityToAffectCkanCatalogEntry(
        IUserDetails initiatingUserDetails,
        ICddoDataAsset cddoDataAsset)
    {
        return cddoDataAsset.OrganisationId == initiatingUserDetails.UserIdSet.OrganisationId;
    }

    private void DoRecordDataAssetUpdateValidationErrors(
        DataAssetActionSourceEnum actionSource,
        IDataAssetValidationResult dataAssetValidationResult,
        IUserDetails initiatingUserDetails)
    {
        if (dataAssetValidationResult.AssetIsValid) return;

        foreach (var propertyResult in dataAssetValidationResult.ValidationPropertyResults)
        {
            var dataAssetValidationPropertyErrors = propertyResult.Errors.ToList();

            if (!dataAssetValidationPropertyErrors.Any()) continue;

            foreach (var dataAssetValidationPropertyError in dataAssetValidationPropertyErrors)
            {
                appInsightsLogger.LogDataAssetValidationErrorBase(
                    catalogAssetField: dataAssetValidationPropertyError.CatalogAssetField,
                    actionSource: actionSource,
                    dataAssetPropertyName: propertyResult.PropertyName,
                    errorMessage: dataAssetValidationPropertyError.Error,
                    errorSeverity: dataAssetValidationPropertyError.ErrorSeverity,
                    errorType: dataAssetValidationPropertyError.ErrorType,
                    initiatingUserDetails: initiatingUserDetails);
            }
        }
    }

    private void DoRecordValidatedProfiledDataAssetValidationErrors(
        DataAssetActionSourceEnum actionSource,
        IValidatedProfiledDataAsset validatedProfiledDataAsset,
        IUserDetails initiatingUserDetails)
    {
        foreach (var (propertyName, propertyValidationErrors) in validatedProfiledDataAsset.ValidationErrors)
        {
            foreach (var propertyValidationError in propertyValidationErrors)
            {
                appInsightsLogger.LogDataAssetValidationErrorBase(
                    catalogAssetField: propertyValidationError.CatalogAssetField,
                    actionSource: actionSource,
                    dataAssetPropertyName: propertyName,
                    errorMessage: propertyValidationError.Error,
                    errorSeverity: propertyValidationError.ErrorSeverity,
                    errorType: propertyValidationError.ErrorType,
                    initiatingUserDetails: initiatingUserDetails);
            }
        }
    }

    public static class ErrorMessages
    {
        public const string InvalidDataAssetId = "Guid.Empty is not a valid Data Asset Id";
    }


    #endregion
}