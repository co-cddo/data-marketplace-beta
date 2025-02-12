using Moq;
using NUnit.Framework;
using AutoFixture;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Configuration;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageCreate;
using Agm.Catalog.DotNet.Logic.Services.Ckan;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetConversion;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetMigration;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Duplication;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.SpreadsheetIngestion.Validation;
using Agm.Catalog.DotNet.Logic.Services.EmbeddedResourceProvision;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Conversion;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using System.ComponentModel;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.UserData;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetConversion.ProfiledDataAssetConversion;
using System.Net;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Validation;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Model.PackageSearch;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Exceptions;
using Microsoft.Azure.Cosmos.Linq;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets.Models;
using Agm.Catalog.DotNet.Logic.Services.Lookup.Results;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Model.SearchAutoComplete;
using Microsoft.AspNetCore.Http;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.SpreadsheetIngestion;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using Cddo.Data.Marketplace.Logic.Test;

namespace Cddo.Data.Marketplace.Api.Test.Services
{


    [TestFixture]
    public class DataAssetServiceTests
    {
        private UserDetails _userProfile;

        public DataAssetServiceTests()
        {
            var userProfileMock = new Mock<IUserDetails>(MockBehavior.Loose);
            var userIdSet = new UserIdSet()
            {
                UserId = 1,
                DomainId = 1,
                OrganisationId = 1
            };

            var userContactDetails = new UserContactDetails()
            {
                UserName = "test user",
                EmailAddress = "test@email.com"
            };
            var organisationDetails = new OrganisationInformation()
            {
                OrganisationId = 1,
                OrganisationName = "Test Org",
                Domains = new List<IDomainInformation>()

            };

            _userProfile = new UserDetails
            {
                UserIdSet = userIdSet,
                UserContactDetails = userContactDetails,
                OrganisationInformation = organisationDetails
            };
        }
        [Test]
        public async Task AddProfiledDataAssetAsync_ShouldReturnSuccess_WhenValidInputs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var createdDataAssetId = new Guid();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(p => p.GetProfiledDataAssetConverterForProfileId(profileId))
                .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertProfiledDataAssetPayloadToCkanCatalogEntryWrite(It.IsAny<ProfiledDataAsset>(), It.IsAny<AgmUserDetails>(), It.IsAny<DataShareRequestNotificationRecipient>()))
                .Returns(ckanCatalogEntryWrite);

            profileDataAssetConverterMock
                .Setup(p => p.GetDataAssetValidation())
                .Returns(dataAssetValidationMock.Object);

            dataAssetValidationMock
                .Setup(p => p.ValidateCkanCatalogEntryWrite(It.IsAny<CkanCatalogEntryWrite>(), It.IsAny<DataAssetType>(), It.IsAny<bool>()))
                .Returns(iDataAssetValidationResultMock.Object);

            testItems._ckanConnectionMock
                .Setup(c => c.AddCatalogEntryAsync(It.IsAny<CkanCatalogEntryWrite>()))
                .ReturnsAsync(createdDataAssetId);

            var successfulDataResult = new Mock<IServiceOperationDataResult<AddDataAssetResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<AddDataAssetResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).AddProfiledDataAssetAsync(
            initiatingUserDetails,
            profileId,
            dataAssetType,
            payload,
            managementMetadata,
            actionSource
            );

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }
        [Test]
        public void AddProfiledDataAssetAsync_ShouldThrowArgumentException_WhenProfileIdIsNullOrWhitespace()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            string? profileId = null;
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).AddProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetType,
                    payload,
                    managementMetadata,
                    actionSource
                ));
        }

        [Test]
        public void AddProfiledDataAssetAsync_ShouldThrowInvalidEnumArgumentException_WhenInvalidDataAssetType()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = (DataAssetType)999;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            // Act & Assert
            Assert.ThrowsAsync<InvalidEnumArgumentException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).AddProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetType,
                    payload,
                    managementMetadata,
                    actionSource
                ));
        }
        [Test]
        public void AddProfiledDataAssetAsync_ShouldThrowArgumentException_WhenPayloadIsNullOrWhitespace()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            string payload = null;
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).AddProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetType,
                    payload,
                    managementMetadata,
                    actionSource
                ));
        }
        [Test]
        public void AddProfiledDataAssetAsync_ShouldHandleExceptionAndReturnFailedResult()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            testItems._ckanConnectionMock
                .Setup(c => c.AddCatalogEntryAsync(It.IsAny<CkanCatalogEntryWrite>()))
                .ThrowsAsync(new Exception("Some error"));

            var failedResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IAddDataAssetResult>(It.IsAny<string>(), null))
                .Returns(failedResult.Object);

            // Act
            var result = ((IDataAssetService)testItems.DataAssetService).AddProfiledDataAssetAsync(
                initiatingUserDetails,
                profileId,
                dataAssetType,
                payload,
                managementMetadata,
                actionSource
            );

            // Assert
            Assert.That(result, Is.Not.Null);
        }
        [Test]
        public async Task PatchProfiledDataAssetAsync_ShouldReturnSuccess_WhenValidInputs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var existingCkanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var existingCkanCatalogEntryCddo = new Mock<ICddoDataAsset>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());
            existingCkanCatalogEntryCddo.Setup(x => x.OrganisationId).Returns(initiatingUserDetails.UserIdSet.OrganisationId);
            existingCkanCatalogEntryCddo.Setup(x => x.DataAssetType).Returns(DataAssetType.DataSet);

            var createdDataAssetId = Guid.NewGuid();

            testItems._profiledDataAssetConverterPresenterMock
                 .Setup(p => p.GetProfiledDataAssetConverterForProfileId(profileId))
                 .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(existingCkanCatalogEntryCddo.Object);

            profileDataAssetConverterMock
                .Setup(p => p.GetDataAssetValidation())
                .Returns(dataAssetValidationMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertProfiledPartialDataAssetPayloadToCkanCatalogEntryWrite(It.IsAny<ProfiledPartialDataAsset>()))
                .Returns(ckanCatalogEntryWrite);

            testItems._ckanConnectionMock
                .Setup(c => c.GetCatalogEntryWithProfileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(existingCkanCatalogEntry);

            testItems._ckanConnectionMock
                .Setup(c => c.PatchCatalogEntryAsync(It.IsAny<CkanCatalogEntryWrite>(), It.IsAny<CkanCatalogEntryRead>()))
                .ReturnsAsync(createdDataAssetId);

            dataAssetValidationMock
                .Setup(p => p.ValidateCkanCatalogEntryWrite(It.IsAny<CkanCatalogEntryWrite>(), It.IsAny<DataAssetType>(), It.IsAny<bool>()))
                .Returns(iDataAssetValidationResultMock.Object);

            var successfulDataResult = new Mock<IServiceOperationDataResult<PatchDataAssetResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<PatchDataAssetResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PatchProfiledDataAssetAsync(
                initiatingUserDetails,
                profileId,
                dataAssetType,
                payload,
                managementMetadata,
                actionSource
            );

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }
        [Test]
        public void PatchProfiledDataAssetAsync_ShouldThrowArgumentNullException_WhenProfileIdIsNullOrEmpty()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            string? profileId = null; // Null or empty string will trigger ArgumentNullException
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).PatchProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetType,
                    payload,
                    managementMetadata,
                    actionSource
                ));
        }
        [Test]
        public void PatchProfiledDataAssetAsync_ShouldThrowInvalidEnumArgumentException_WhenInvalidDataAssetType()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = (DataAssetType)999; // Invalid enum value
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            // Act & Assert
            Assert.ThrowsAsync<InvalidEnumArgumentException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).PatchProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetType,
                    payload,
                    managementMetadata,
                    actionSource
                ));
        }
        [Test]
        public void PatchProfiledDataAssetAsync_ShouldThrowUnAuthorizedAccessToDataAssetException_WhenUserIsUnauthorized()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var existingCkanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var existingCkanCatalogEntryCddo = new Mock<ICddoDataAsset>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());
            existingCkanCatalogEntryCddo.Setup(x => x.OrganisationId).Returns(999999999);
            existingCkanCatalogEntryCddo.Setup(x => x.DataAssetType).Returns(DataAssetType.DataSet);

            testItems._profiledDataAssetConverterPresenterMock
                 .Setup(p => p.GetProfiledDataAssetConverterForProfileId(profileId))
                 .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(existingCkanCatalogEntryCddo.Object);

            profileDataAssetConverterMock
                .Setup(p => p.GetDataAssetValidation())
                .Returns(dataAssetValidationMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertProfiledPartialDataAssetPayloadToCkanCatalogEntryWrite(It.IsAny<ProfiledPartialDataAsset>()))
                .Returns(ckanCatalogEntryWrite);

            testItems._ckanConnectionMock
                .Setup(c => c.GetCatalogEntryWithProfileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(existingCkanCatalogEntry);

            // Act & Assert
            Assert.ThrowsAsync<UnAuthorizedAccessToDataAssetException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).PatchProfiledDataAssetAsync(
                    initiatingUserDetails,
                    profileId,
                    dataAssetType,
                    payload,
                    managementMetadata,
                    actionSource
                ));
        }
        [Test]
        public async Task PatchProfiledDataAssetAsync_ShouldReturnFailedResult_WhenAnExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var existingCkanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var existingCkanCatalogEntryCddo = new Mock<ICddoDataAsset>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());
            existingCkanCatalogEntryCddo.Setup(x => x.OrganisationId).Returns(initiatingUserDetails.UserIdSet.OrganisationId);
            existingCkanCatalogEntryCddo.Setup(x => x.DataAssetType).Returns(DataAssetType.DataSet);

            var createdDataAssetId = Guid.NewGuid();

            testItems._profiledDataAssetConverterPresenterMock
                 .Setup(p => p.GetProfiledDataAssetConverterForProfileId(profileId))
                 .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(existingCkanCatalogEntryCddo.Object);

            profileDataAssetConverterMock
                .Setup(p => p.GetDataAssetValidation())
                .Returns(dataAssetValidationMock.Object);

            profileDataAssetConverterMock
                .Setup(p => p.ConvertProfiledPartialDataAssetPayloadToCkanCatalogEntryWrite(It.IsAny<ProfiledPartialDataAsset>()))
                .Returns(ckanCatalogEntryWrite);

            testItems._ckanConnectionMock
                .Setup(c => c.GetCatalogEntryWithProfileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(existingCkanCatalogEntry);

            testItems._ckanConnectionMock
                .Setup(c => c.PatchCatalogEntryAsync(It.IsAny<CkanCatalogEntryWrite>(), It.IsAny<CkanCatalogEntryRead>()))
                .ThrowsAsync(new Exception("Some unexpected error"));

            var failedResult = new Mock<IServiceOperationDataResult<PatchDataAssetResult>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IPatchDataAssetResult>(It.IsAny<string>(), null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PatchProfiledDataAssetAsync(
                initiatingUserDetails,
                profileId,
                dataAssetType,
                payload,
                managementMetadata,
                actionSource
            );

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task PatchProfiledDataAssetAsync_ShouldReturnFailedResult_WhenDataAssetNotFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = (IUserDetails)_userProfile;
            var profileId = "ID";
            var dataAssetType = DataAssetType.DataService;
            var payload = "payload";
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();
            var actionSource = DataAssetActionSourceEnum.UserInterface;

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var existingCkanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var existingCkanCatalogEntryCddo = new Mock<ICddoDataAsset>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());
            existingCkanCatalogEntryCddo.Setup(x => x.OrganisationId).Returns(initiatingUserDetails.UserIdSet.OrganisationId);
            existingCkanCatalogEntryCddo.Setup(x => x.DataAssetType).Returns(DataAssetType.DataSet);

            var createdDataAssetId = Guid.NewGuid();

            testItems._profiledDataAssetConverterPresenterMock
                 .Setup(p => p.GetProfiledDataAssetConverterForProfileId(profileId))
                 .Throws(new ProfiledDataAssetNotFoundException() { DataAssetId = new Guid(), ProfileId ="1" });

            testItems._ckanConnectionMock
                .Setup(c => c.PatchCatalogEntryAsync(It.IsAny<CkanCatalogEntryWrite>(), It.IsAny<CkanCatalogEntryRead>()))
                .ThrowsAsync(new Exception("Some unexpected error"));

            var failedResult = new Mock<IServiceOperationDataResult<PatchDataAssetResult>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IPatchDataAssetResult>(It.IsAny<string>(), HttpStatusCode.NotFound))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PatchProfiledDataAssetAsync(
                initiatingUserDetails,
                profileId,
                dataAssetType,
                payload,
                managementMetadata,
                actionSource
            );

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task DeleteProfiledDataAssetAsync_ValidRequest_ReturnsSuccessfulDeletion()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var profileId = "ID";
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var initiatingUserDetails = (IUserDetails)_userProfile;
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var existingCkanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var existingCkanCatalogEntryCddo = new Mock<ICddoDataAsset>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());
            existingCkanCatalogEntryCddo.Setup(x => x.OrganisationId).Returns(initiatingUserDetails.UserIdSet.OrganisationId);
            existingCkanCatalogEntryCddo.Setup(x => x.DataAssetType).Returns(DataAssetType.DataSet);

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(existingCkanCatalogEntry);

            var mockIProfiledDataAssetConverter = new Mock<IProfiledDataAssetConverter>();

            var mockDataAsset = new Mock<ICddoDataAsset>();

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Returns(mockIProfiledDataAssetConverter.Object);

            mockIProfiledDataAssetConverter
                .Setup(x => x.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(existingCkanCatalogEntryCddo.Object);

            var successfulDataResult = new Mock<IServiceOperationDataResult<DeleteDataAssetResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<DeleteDataAssetResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).DeleteProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }
        [Test]
        public async Task DeleteProfiledDataAssetAsync_FailureWhenCatalogEntryNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = (IUserDetails)_userProfile;

            var dataAssetId = Guid.NewGuid();
            var profileId = "profile1";

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new DataAssetNotFoundException() { DataAssetId = dataAssetId });

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<DeleteDataAssetResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IDeleteDataAssetResult>(It.IsAny<string>(), It.IsAny<HttpStatusCode>()))
                .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).DeleteProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task DeleteProfiledDataAssetAsync_InsufficientPermissions_ReturnsForbiddenResult()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var profileId = "ID";
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var initiatingUserDetails = (IUserDetails)_userProfile;
            var managementMetadata = testItems.Fixture.Create<ManagementMetadataBase>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();
            var existingCkanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var existingCkanCatalogEntryCddo = new Mock<ICddoDataAsset>();
            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var iDataAssetValidationResultMock = new Mock<IDataAssetValidationResult>();

            iDataAssetValidationResultMock.Setup(x => x.ValidationPropertyResults).Returns(testItems.Fixture.Create<List<DataAssetValidationPropertyResult>>());
            existingCkanCatalogEntryCddo.Setup(x => x.OrganisationId).Returns(9999999);
            existingCkanCatalogEntryCddo.Setup(x => x.DataAssetType).Returns(DataAssetType.DataSet);

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(existingCkanCatalogEntry);

            var mockIProfiledDataAssetConverter = new Mock<IProfiledDataAssetConverter>();

            var mockDataAsset = new Mock<ICddoDataAsset>();

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Returns(mockIProfiledDataAssetConverter.Object);

            mockIProfiledDataAssetConverter
                .Setup(x => x.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(existingCkanCatalogEntryCddo.Object);


            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<DeleteDataAssetResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);
            unSuccessfulDataResult.Setup(x => x.StatusCode).Returns(HttpStatusCode.Forbidden);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IDeleteDataAssetResult>(It.IsAny<string>(), It.IsAny<HttpStatusCode>()))
                .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).DeleteProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(HttpStatusCode.Forbidden, Is.EqualTo(result.StatusCode));
        }
        [Test]
        public async Task DeleteProfiledDataAssetAsync_CatalogEntryDeleteFails_ReturnsInternalServerError()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = (IUserDetails)_userProfile;

            var dataAssetId = Guid.NewGuid();
            var profileId = "profile1";

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new Exception());  // Catalog entry not found

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<DeleteDataAssetResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IDeleteDataAssetResult>(It.IsAny<string>(), null))
                .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).DeleteProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task GetProfiledDataAssetsAsync_ShouldReturnSuccessfulResult_WhenNoErrorsOccur()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var dataAssetTypes = testItems.Fixture.Create<IEnumerable<DataAssetType>>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser = true;
            var onlyIncludeRecordsOwnedByOrganisationOfCallingUser = true;
            var searchText = testItems.Fixture.Create<string>();

            var ckanCatalogEntrySet = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();
            var profiledDataAssets = testItems.Fixture.Create<IEnumerable<ProfiledDataAsset>>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntriesWithProfileAsync(It.IsAny<string>(), It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<string>()))
                .ReturnsAsync(ckanCatalogEntrySet);

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(x => x.ConvertCkanCatalogEntryReadsToProfiledDataAssets(It.IsAny<IEnumerable<CkanCatalogEntryRead>>()))
                .Returns(profiledDataAssets);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetProfiledDataAssetsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<GetProfiledDataAssetsResult>(), null))
                .Returns(successfulDataResult.Object);
            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetsAsync(
                initiatingUserDetails, profileId, dataAssetTypes, onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
                onlyIncludeRecordsOwnedByOrganisationOfCallingUser, searchText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }
        [Test]
        public async Task GetProfiledDataAssetsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var dataAssetTypes = testItems.Fixture.Create<IEnumerable<DataAssetType>>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser = true;
            var onlyIncludeRecordsOwnedByOrganisationOfCallingUser = true;
            var searchText = testItems.Fixture.Create<string>();

            var ckanCatalogEntrySet = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();
            var profiledDataAssets = testItems.Fixture.Create<IEnumerable<ProfiledDataAsset>>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntriesWithProfileAsync(It.IsAny<string>(), It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Some error"));


            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetProfiledDataAssetsResult>(It.IsAny<string>(), null))
                .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetsAsync(
                initiatingUserDetails, profileId, dataAssetTypes, onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
                onlyIncludeRecordsOwnedByOrganisationOfCallingUser, searchText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void GetProfiledDataAssetsAsync_ShouldThrowArgumentException_WhenProfileIdIsNullOrWhiteSpace()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = "";
            var dataAssetTypes = testItems.Fixture.Create<IEnumerable<DataAssetType>>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser = true;
            var onlyIncludeRecordsOwnedByOrganisationOfCallingUser = true;
            var searchText = testItems.Fixture.Create<string>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                 ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetsAsync(
                    initiatingUserDetails, profileId, dataAssetTypes, onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser,
                    onlyIncludeRecordsOwnedByOrganisationOfCallingUser, searchText));
        }
        [Test]
        public async Task GetProfiledDataAssetIdsAsync_ShouldReturnSuccessfulResult_WhenNoErrorsOccur()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var ckanCatalogEntrySet = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();
            var profiledDataAssets = testItems.Fixture.Create<IEnumerable<ProfiledDataAsset>>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntriesWithProfileAsync(It.IsAny<string>(), It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), null))
                .ReturnsAsync(ckanCatalogEntrySet);

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(x => x.ConvertCkanCatalogEntryReadsToProfiledDataAssetIds(It.IsAny<IEnumerable<CkanCatalogEntryRead>>()))
                .Returns(testItems.Fixture.Create<List<string>>());

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetProfiledDataAssetIdsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<GetProfiledDataAssetIdsResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetIdsAsync(
                initiatingUserDetails, profileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }
        [Test]
        public async Task GetProfiledDataAssetIdsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var ckanCatalogEntrySet = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();
            var profiledDataAssets = testItems.Fixture.Create<IEnumerable<ProfiledDataAsset>>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntriesWithProfileAsync(It.IsAny<string>(), It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), null))
                .ThrowsAsync(new Exception("Some error"));

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetIdsResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetProfiledDataAssetIdsResult>(It.IsAny<string>(), null))
                .Returns(unSuccessfulDataResult.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetIdsAsync(
                initiatingUserDetails, profileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task GetProfiledDataAssetIdsAsync_ShouldThrowArgumentException_WhenProfileIdIsNullOrWhiteSpace()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = "";
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                  await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetIdsAsync(
                    initiatingUserDetails, profileId));
        }
        [Test]
        public async Task GetProfiledDataAssetAsync_ShouldReturnSuccessfulResult_WhenNoErrorsOccur()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var profiledDataAsset = testItems.Fixture.Create<ProfiledDataAsset>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryWithProfileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(ckanCatalogEntry);

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock
                .Setup(x => x.ConvertCkanCatalogEntryReadsToProfiledDataAssetIds(It.IsAny<IEnumerable<CkanCatalogEntryRead>>()))
                .Returns(testItems.Fixture.Create<List<string>>());

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetProfiledDataAssetResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<GetProfiledDataAssetResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            testItems._ckanConnectionMock.Verify(x => x.GetCatalogEntryWithProfileAsync(profileId, dataAssetId, It.IsAny<ICatalogEntriesOrganisationFilter>()), Times.Once);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetProfiledDataAssetResult>(), null), Times.Once);
        }
        [Test]
        public void GetProfiledDataAssetAsync_ShouldThrowArgumentException_WhenDataAssetIdIsEmpty()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var dataAssetId = Guid.Empty;  // Invalid dataAssetId
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                 ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId));
        }
        [Test]
        public async Task GetProfiledDataAssetAsync_ShouldReturnFailedResult_WhenProfiledDataAssetNotFoundExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var profiledDataAsset = testItems.Fixture.Create<ProfiledDataAsset>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryWithProfileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new ProfiledDataAssetNotFoundException() { DataAssetId = It.IsAny<Guid>(), ProfileId = It.IsAny<string>() });

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetProfiledDataAssetResult>(It.IsAny<string>(), It.IsAny<HttpStatusCode>()))
                .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            testItems._ckanConnectionMock.Verify(x => x.GetCatalogEntryWithProfileAsync(profileId, dataAssetId, It.IsAny<ICatalogEntriesOrganisationFilter>()), Times.Once);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IGetProfiledDataAssetResult>(It.IsAny<string>(), It.IsAny<HttpStatusCode>()), Times.Once);
        }
        [Test]
        public async Task GetProfiledDataAssetAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var profileId = testItems.Fixture.Create<string>();
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var profiledDataAsset = testItems.Fixture.Create<ProfiledDataAsset>();

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryWithProfileAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new Exception());

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetProfiledDataAssetResult>(It.IsAny<string>(), null))
                .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetProfiledDataAssetAsync(initiatingUserDetails, profileId, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            testItems._ckanConnectionMock.Verify(x => x.GetCatalogEntryWithProfileAsync(profileId, dataAssetId, It.IsAny<ICatalogEntriesOrganisationFilter>()), Times.Once);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IGetProfiledDataAssetResult>(It.IsAny<string>(), null), Times.Once);
        }
        [Test]
        public async Task GetCddoDataAssetsAsync_ShouldReturnSuccessfulResult_WhenNoErrorsOccur()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var startIndex = testItems.Fixture.Create<int>();
            var numberOfAssets = testItems.Fixture.Create<int>();
            var sortField = testItems.Fixture.Create<DataAssetsSortField>();
            var sortDirection = testItems.Fixture.Create<DataAssetsSortDirection>();
            var searchText = testItems.Fixture.Create<string>();
            var publishers = testItems.Fixture.Create<List<string>>();
            var themes = testItems.Fixture.Create<List<string>>();
            var entryTypes = testItems.Fixture.Create<List<string>>();

            var ckanCatalogEntrySet = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();
            var cddoDataAssets = testItems.Fixture.Create<List<ICddoDataAsset>>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntriesAsync(It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<IEnumerable<DataAssetStatus>>(),
                    It.IsAny<CatalogEntriesResultPagination>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<CatalogEntryLookupTokens>()))
                .ReturnsAsync(ckanCatalogEntrySet);

            testItems._cddoDataAssetConverterMock
                .Setup(x => x.ConvertCkanCatalogEntryReadsToCddoDataAssets(It.IsAny<IEnumerable<CkanCatalogEntryRead>>()))
                .Returns(cddoDataAssets);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetCddoDataAssetsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<GetCddoDataAssetsResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetsAsync(
                initiatingUserDetails,
                onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser: true,
                onlyIncludeRecordsOwnedByOrganisationOfCallingUser: true,
                dataAssetTypes: null,
                dataAssetStatuses: null,
                startIndex,
                numberOfAssets,
                sortField,
                sortDirection,
                searchText,
                publishers,
                themes,
                entryTypes
            );

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            testItems._ckanConnectionMock.Verify(x => x.GetCatalogEntriesAsync(It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<IEnumerable<DataAssetStatus>>(),
                It.IsAny<CatalogEntriesResultPagination>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<CatalogEntryLookupTokens>()), Times.Once);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetCddoDataAssetsResult>(), null), Times.Once);
        }
        [Test]
        public void GetCddoDataAssetsAsync_ShouldThrowArgumentOutOfRangeException_WhenStartIndexIsNegative()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var startIndex = -1; // Invalid start index
            var numberOfAssets = testItems.Fixture.Create<int>();
            var sortField = testItems.Fixture.Create<DataAssetsSortField>();
            var sortDirection = testItems.Fixture.Create<DataAssetsSortDirection>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                 ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetsAsync(initiatingUserDetails,
                    onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser: true,
                    onlyIncludeRecordsOwnedByOrganisationOfCallingUser: true,
                    dataAssetTypes: null,
                    dataAssetStatuses: null,
                    startIndex,
                    numberOfAssets,
                    sortField,
                    sortDirection,
                    searchText: null,
                    publishers: null,
                    themes: null,
                    entryTypes: null));
        }
        [Test]
        public void GetCddoDataAssetsAsync_ShouldThrowArgumentOutOfRangeException_WhenNumberOfAssetsIsZeroOrNegative()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var startIndex = testItems.Fixture.Create<int>();
            var numberOfAssets = 0; // Invalid number of assets
            var sortField = testItems.Fixture.Create<DataAssetsSortField>();
            var sortDirection = testItems.Fixture.Create<DataAssetsSortDirection>();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetsAsync(initiatingUserDetails,
                    onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser: true,
                    onlyIncludeRecordsOwnedByOrganisationOfCallingUser: true,
                    dataAssetTypes: null,
                    dataAssetStatuses: null,
                    startIndex,
                    numberOfAssets,
                    sortField,
                    sortDirection,
                    searchText: null,
                    publishers: null,
                    themes: null,
                    entryTypes: null));
        }
        [Test]
        public void GetCddoDataAssetsAsync_ShouldThrowInvalidEnumArgumentException_WhenSortDirectionIsInvalid()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var startIndex = testItems.Fixture.Create<int>();
            var numberOfAssets = testItems.Fixture.Create<int>();
            var sortField = testItems.Fixture.Create<DataAssetsSortField>();
            var sortDirection = (DataAssetsSortDirection)999; // Invalid sort direction

            // Act & Assert
            Assert.ThrowsAsync<InvalidEnumArgumentException>(() =>
                ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetsAsync(initiatingUserDetails,
                    onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser: true,
                    onlyIncludeRecordsOwnedByOrganisationOfCallingUser: true,
                    dataAssetTypes: null,
                    dataAssetStatuses: null,
                    startIndex,
                    numberOfAssets,
                    sortField,
                    sortDirection,
                    searchText: null,
                    publishers: null,
                    themes: null,
                    entryTypes: null));
        }
        [Test]
        public async Task GetCddoDataAssetsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var startIndex = testItems.Fixture.Create<int>();
            var numberOfAssets = testItems.Fixture.Create<int>();
            var sortField = testItems.Fixture.Create<DataAssetsSortField>();
            var sortDirection = testItems.Fixture.Create<DataAssetsSortDirection>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntriesAsync(It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<IEnumerable<DataAssetStatus>>(),
                    It.IsAny<CatalogEntriesResultPagination>(), It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<CatalogEntryLookupTokens>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var failedDataResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetsResult>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetCddoDataAssetsResult>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetsAsync(initiatingUserDetails,
                onlyIncludeRecordsDiscoverableByOrganisationOfCallingUser: true,
                onlyIncludeRecordsOwnedByOrganisationOfCallingUser: true,
                dataAssetTypes: null,
                dataAssetStatuses: null,
                startIndex,
                numberOfAssets,
                sortField,
                sortDirection,
                searchText: null,
                publishers: null,
                themes: null,
                entryTypes: null);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public void GetCddoDataAssetAsync_ShouldThrowArgumentException_WhenDataAssetIdIsEmpty()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = Guid.Empty;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetAsync(initiatingUserDetails, dataAssetId));
        }
        [Test]
        public async Task GetCddoDataAssetAsync_ShouldReturnSuccessfulResult_WhenNoErrorsOccur()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();

            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var cddoDataAsset = new Mock<CddoDataAsset>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(ckanCatalogEntry);

            testItems._cddoDataAssetConverterMock
                .Setup(x => x.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(cddoDataAsset.Object);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetCddoDataAssetResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<GetCddoDataAssetResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            testItems._ckanConnectionMock.Verify(x => x.GetCatalogEntryAsync(dataAssetId, It.IsAny<ICatalogEntriesOrganisationFilter>()), Times.Once);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetCddoDataAssetResult>(), null), Times.Once);
        }
        [Test]
        public async Task GetCddoDataAssetAsync_ShouldReturnFailedResult_WhenDataAssetNotFoundExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new DataAssetNotFoundException() { DataAssetId = It.IsAny<Guid>() });

            var failedDataResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetResult>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetCddoDataAssetResult>(It.IsAny<string>(), It.IsAny<HttpStatusCode?>()))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IGetCddoDataAssetResult>(It.IsAny<string>(), It.IsAny<HttpStatusCode?>()), Times.Once);
        }
        [Test]
        public async Task GetCddoDataAssetAsync_ShouldReturnFailedResult_WhenGenericExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var failedDataResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetResult>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IGetCddoDataAssetResult>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IGetCddoDataAssetResult>(It.IsAny<string>(), null), Times.Once);
        }
        [Test]
        public void GetCddoDataAssetValidationPropertyErrorsAsync_ShouldThrowArgumentException_WhenDataAssetIdIsEmpty()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = Guid.Empty;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() =>
                ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetValidationPropertyErrorsAsync(initiatingUserDetails, dataAssetId));
        }

        [Test]
        public async Task GetCddoDataAssetValidationPropertyErrorsAsync_ShouldReturnFailedResult_WhenCkanCatalogEntryNotFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync((CkanCatalogEntryRead?)null);

            var failedDataResult = new Mock<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IEnumerable<IDataAssetValidationPropertyResult>>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetValidationPropertyErrorsAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task GetCddoDataAssetValidationPropertyErrorsAsync_ShouldReturnFailedResult_WhenProfileIdNotFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(ckanCatalogEntry);

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Throws(new InvalidOperationException("error"));

            var failedDataResult = new Mock<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IEnumerable<IDataAssetValidationPropertyResult>>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetValidationPropertyErrorsAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IEnumerable<IDataAssetValidationPropertyResult>>(It.IsAny<string>(), null), Times.Once);
        }

        [Test]
        public async Task GetCddoDataAssetValidationPropertyErrorsAsync_ShouldReturnSuccessfulResult_WhenNoErrorsOccur()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var ckanExtraField = testItems.Fixture.Create<CkanCatalogEntryExtraRead>();
            ckanExtraField.Key = "profileId";
            ckanExtraField.Value = "test";
            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();

            ckanCatalogEntry.Extras = new List<CkanCatalogEntryExtraRead>() { ckanExtraField };
            var profileId = testItems.Fixture.Create<string>();

            var validationPropertyResults = testItems.Fixture.CreateMany<IDataAssetValidationPropertyResult>(3);

            var dataAssetValidationMock = new Mock<IDataAssetValidationResult>();
            dataAssetValidationMock.Setup(x => x.ValidationPropertyResults).Returns(validationPropertyResults);

            var dataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();


            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(ckanCatalogEntry);

            testItems._profiledDataAssetConverterPresenterMock
                .Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>()))
                .Returns(dataAssetConverterMock.Object);

            var validationMock = new Mock<IDataAssetValidation>();
            validationMock.Setup(x => x.ValidateCkanCatalogEntryRead(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(dataAssetValidationMock.Object);

            dataAssetConverterMock.Setup(x => x.GetDataAssetValidation()).Returns(validationMock.Object);

            var successfulDataResult = new Mock<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetValidationPropertyErrorsAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), null), Times.Once);
        }

        [Test]
        public async Task GetCddoDataAssetValidationPropertyErrorsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();

            testItems._ckanConnectionMock
                .Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new Exception("error"));

            var failedDataResult = new Mock<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateFailedDataResult<IEnumerable<IDataAssetValidationPropertyResult>>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoDataAssetValidationPropertyErrorsAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IEnumerable<IDataAssetValidationPropertyResult>>(It.IsAny<string>(), null), Times.Once);
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetAsync_ShouldReturnExpectedResult_WhenValidDataIsProvided()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var userDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = Guid.NewGuid();

            var subjectDataAsset = testItems.Fixture.Create<ICddoDataAsset>();
            var objectDataAssets = testItems.Fixture.Create<List<ICddoDataAsset>>();
            var potentialDuplicateDataAssetInformation = testItems.Fixture.Create<List<PotentialDuplicateDataAssetInformation>>();

            var ckanEntryRead = testItems.Fixture.Create<CkanCatalogEntryRead>();
            var ckanPackageData = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();
            // Mock behavior for dependencies
            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(It.IsAny<Guid>(), It.IsAny<CatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(ckanEntryRead);

            testItems._cddoDataAssetConverterMock.Setup(x => x.ConvertCkanCatalogEntryReadToCddoDataAsset(It.IsAny<CkanCatalogEntryRead>()))
                .Returns(subjectDataAsset);

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntriesAsync(It.IsAny<List<DataAssetType>>(), It.IsAny<List<DataAssetStatus>>(), It.IsAny<CatalogEntriesResultPagination>(), It.IsAny<CatalogEntriesOrganisationFilter>(), It.IsAny<CatalogEntryLookupTokens>()))
                .ReturnsAsync(ckanPackageData);

            testItems._cddoDataAssetConverterMock.Setup(x => x.ConvertCkanCatalogEntryReadsToCddoDataAssets(It.IsAny<List<CkanCatalogEntryRead>>()))
                .Returns(objectDataAssets);

            testItems._dataAssetDuplicationDeterminationMock.Setup(x => x.DeterminePotentialDuplicatesToDataAsset(It.IsAny<ICddoDataAsset>(), It.IsAny<IList<ICddoDataAsset>>()))
                .Returns(potentialDuplicateDataAssetInformation);

            var successfulDataResult = new Mock<IServiceOperationDataResult<CheckForPotentialDuplicatesToDataAssetResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock
                .Setup(s => s.CreateSuccessfulDataResult(It.IsAny<CheckForPotentialDuplicatesToDataAssetResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetAsync(userDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);

        }
        [Test]
        public async Task GetCddoOrganisationsAsync_ShouldReturnSuccessfulResult_WhenOrganisationsAreRetrieved()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetStatuses = testItems.Fixture.CreateMany<DataAssetStatus>(2);
            var allOrganisations = testItems.Fixture.CreateMany<string>(5); 
            var distinctOrganisations = new List<string>(new HashSet<string>(allOrganisations));

            // Setup mocks for successful path
            testItems._ckanConnectionMock.Setup(x => x.GetCatalogOrganisationsAsync(It.IsAny<IEnumerable<DataAssetStatus>>()))
                .ReturnsAsync(allOrganisations);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetCddoOrganisationsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);
            successfulDataResult.Setup(x => x.Data).Returns(new GetCddoOrganisationsResult
            {
                Organisations = distinctOrganisations
            });

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetCddoOrganisationsResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoOrganisationsAsync(initiatingUserDetails, dataAssetStatuses);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.Organisations, Is.EquivalentTo(distinctOrganisations));
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetCddoOrganisationsResult>(), null), Times.Once);
        }
        [Test]
        public async Task GetCddoOrganisationsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetStatuses = testItems.Fixture.CreateMany<DataAssetStatus>(2);
            var exceptionMessage = "error";

            // Setup mock to throw an exception
            testItems._ckanConnectionMock.Setup(x => x.GetCatalogOrganisationsAsync(It.IsAny<IEnumerable<DataAssetStatus>>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            var failedDataResult = new Mock<IServiceOperationDataResult<IGetCddoOrganisationsResult>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateFailedDataResult<IGetCddoOrganisationsResult>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoOrganisationsAsync(initiatingUserDetails, dataAssetStatuses);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IGetCddoOrganisationsResult>(exceptionMessage, null), Times.Once);
        }
        [Test]
        public async Task GetCddoTopicsAsync_ShouldReturnSuccessfulResult_WhenTopicsAreRetrieved()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetStatuses = testItems.Fixture.CreateMany<DataAssetStatus>(2);
            var allTopics = testItems.Fixture.CreateMany<string>(5); 
            var distinctTopics = new List<string>(new HashSet<string>(allTopics));

            // Setup mocks for successful path
            testItems._ckanConnectionMock.Setup(x => x.GetCatalogTopicsAsync(It.IsAny<IEnumerable<DataAssetStatus>>()))
                .ReturnsAsync(allTopics);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetCddoTopicsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);
            successfulDataResult.Setup(x => x.Data).Returns(new GetCddoTopicsResult
            {
                Topics = distinctTopics
            });

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetCddoTopicsResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoTopicsAsync(initiatingUserDetails, dataAssetStatuses);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.Topics, Is.EquivalentTo(distinctTopics));
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetCddoTopicsResult>(), null), Times.Once);
        }
        [Test]
        public async Task GetCddoTopicsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetStatuses = testItems.Fixture.CreateMany<DataAssetStatus>(2);
            var exceptionMessage = "Failed to retrieve topics.";

            // Setup mock to throw an exception
            testItems._ckanConnectionMock.Setup(x => x.GetCatalogTopicsAsync(It.IsAny<IEnumerable<DataAssetStatus>>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            var failedDataResult = new Mock<IServiceOperationDataResult<IGetCddoTopicsResult>>();
            failedDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateFailedDataResult<IGetCddoTopicsResult>(It.IsAny<string>(), null))
                .Returns(failedDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetCddoTopicsAsync(initiatingUserDetails, dataAssetStatuses);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IGetCddoTopicsResult>(exceptionMessage, null), Times.Once);
        }
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssetsAsync_ShouldReturnSuccessfulResult_WhenSearchSuggestionsAreRetrieved()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var searchText = testItems.Fixture.Create<string>();
            var searchSuggestions = testItems.Fixture.CreateMany<string>(5);
            var mockSearchResults = testItems.Fixture.Create<CkanSearchSuggestionsResponse>();
          

            testItems._ckanConnectionMock.Setup(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<List<DataAssetStatus>>()))
                .ReturnsAsync(mockSearchResults);

            testItems._ckanConfigurationPresenterMock.Setup(x => x.GetSolrMaximumNumberOfSearchSuggestions())
                .Returns(10);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetSearchSuggestionsForPublishedDataAssetsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);
            successfulDataResult.Setup(x => x.Data).Returns(new GetSearchSuggestionsForPublishedDataAssetsResult
            {
                SearchSuggestionsForPublishedDataAssets = searchSuggestions
            });

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetSearchSuggestionsForPublishedDataAssetsResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetSearchSuggestionsForPublishedDataAssetsAsync(initiatingUserDetails, searchText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.SearchSuggestionsForPublishedDataAssets, Is.EquivalentTo(searchSuggestions));
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetSearchSuggestionsForPublishedDataAssetsResult>(), null), Times.Once);
        }
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssetsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var searchText = testItems.Fixture.Create<string>();
            var searchSuggestions = testItems.Fixture.CreateMany<string>(5);
            var mockSearchResults = testItems.Fixture.Create<CkanSearchSuggestionsResponse>();


            testItems._ckanConnectionMock.Setup(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<List<DataAssetStatus>>()))
                .ReturnsAsync(mockSearchResults);

            testItems._ckanConfigurationPresenterMock.Setup(x => x.GetSolrMaximumNumberOfSearchSuggestions())
                .Returns(10);

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetSearchSuggestionsForPublishedDataAssetsResult>(), null))
                .Throws(new Exception());

            testItems._serviceOperationResultFactoryMock
                 .Setup(x => x.CreateFailedDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>(It.IsAny<string>(), null))
                 .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetSearchSuggestionsForPublishedDataAssetsAsync(initiatingUserDetails, searchText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssetsAsync_ShouldReturnSuccessfulResult_WhenSearchSuggestionsAreRetrieved()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var searchText = testItems.Fixture.Create<string>();
            var searchSuggestions = testItems.Fixture.CreateMany<string>(5);
            var mockSearchResults = testItems.Fixture.Create<CkanSearchSuggestionsResponse>();
          

            testItems._ckanConnectionMock.Setup(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<List<DataAssetStatus>>()))
                .ReturnsAsync(mockSearchResults);

            testItems._ckanConfigurationPresenterMock.Setup(x => x.GetSolrMaximumNumberOfSearchSuggestions())
                .Returns(10);

            var successfulDataResult = new Mock<IServiceOperationDataResult<GetSearchSuggestionsForOrganisationDataAssetsResult>>();
            successfulDataResult.Setup(x => x.Success).Returns(true);
            successfulDataResult.Setup(x => x.Data).Returns(new GetSearchSuggestionsForOrganisationDataAssetsResult
            {
                SearchSuggestionsForOrganisationDataAssets = searchSuggestions,
            });

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetSearchSuggestionsForOrganisationDataAssetsResult>(), null))
                .Returns(successfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetSearchSuggestionsForOrganisationDataAssetsAsync(initiatingUserDetails, searchText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<GetSearchSuggestionsForOrganisationDataAssetsResult>(), null), Times.Once);
        }
        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssetsAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var searchText = testItems.Fixture.Create<string>();
            var searchSuggestions = testItems.Fixture.CreateMany<string>(5);
            var mockSearchResults = testItems.Fixture.Create<CkanSearchSuggestionsResponse>();


            testItems._ckanConnectionMock.Setup(x => x.GetSearchSuggestionsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<List<DataAssetStatus>>()))
                .ReturnsAsync(mockSearchResults);

            testItems._ckanConfigurationPresenterMock.Setup(x => x.GetSolrMaximumNumberOfSearchSuggestions())
                .Returns(10);

            var unSuccessfulDataResult = new Mock<IServiceOperationDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>>();
            unSuccessfulDataResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock
                .Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetSearchSuggestionsForOrganisationDataAssetsResult>(), null))
                .Throws(new Exception());

            testItems._serviceOperationResultFactoryMock
                 .Setup(x => x.CreateFailedDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>(It.IsAny<string>(), null))
                 .Returns(unSuccessfulDataResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetSearchSuggestionsForOrganisationDataAssetsAsync(initiatingUserDetails, searchText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public void ValidateProfiledDataAssetsSpreadsheetContentAsync_ShouldThrowArgumentNullException_WhenDataAssetSpreadsheetIsNull()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).ValidateProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, null, "profileId"));
        }

        [Test]
        public void ValidateProfiledDataAssetsSpreadsheetContentAsync_ShouldThrowArgumentException_WhenDataAssetProfileIdIsNullOrEmpty()
        {
            // Act & Assert
            var testItems = TestsSetUp.CreateTestItems();

            var _dataAssetSpreadsheetMock = testItems.Fixture.Create<IFormFile>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).ValidateProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, _dataAssetSpreadsheetMock, string.Empty));
        }
        [Test]
        public async Task ValidateProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnSuccessfulResult_WhenValidInput()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var dataAssetSpreadsheet = testItems.Fixture.Create<IFormFile>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetProfileId = "validProfileId";
            var mockSpreadsheetItems = testItems.Fixture.Create<List<DataAssetSpreadsheetItem>>();

            var mockParsedResult = new ParseDataAssetSpreadsheetResult
            {
                Success = true,
                Errors = new List<string>(),
                DataAssetSpreadsheetItems = mockSpreadsheetItems,
                SpreadsheetFileName = "TestSpreadsheet.xlsx"
            };

            var validatedProfiledDataAsset = testItems.Fixture.Create<ValidatedProfiledDataAsset>();
            var validatedProfiledDataAssetSet = new ValidatedProfiledDataAssetSet
            {
                SpreadsheetName = "TestSpreadsheet.xlsx",
                ValidatedProfiledDataAssets = new List<ValidatedProfiledDataAsset> { validatedProfiledDataAsset }
            };

            var profiledDataAssetConverter = new Mock<IProfiledDataAssetConverter>();

            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(It.IsAny<IUserDetails>())).Returns(testItems.Fixture.Create<AgmUserDetails>());
            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(dataAssetProfileId))
                .Returns(profiledDataAssetConverter.Object);
            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(dataAssetProfileId).GetDataAssetSpreadsheetParser())
                .Returns(Mock.Of<IDataAssetSpreadsheetParser>());
            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(dataAssetProfileId).GetDataAssetSpreadsheetParser().ParseDataAssetSpreadsheetAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(mockParsedResult);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.StoreValidatedProfiledDataAssetSetForUserAsync(It.IsAny<AgmUserDetails>(), It.IsAny<ValidatedProfiledDataAssetSet>()))
                .Returns(Task.CompletedTask);

            profiledDataAssetConverter
                .Setup(x => x.ConvertDataAssetSpreadsheetItemToValidatedProfiledDataAsset(It.IsAny<DataAssetSpreadsheetItem>(), It.IsAny<IAgmUserDetails>()))
                .Returns(testItems.Fixture.Create<ValidatedProfiledDataAsset>());


            var successfulResult = new Mock<IServiceOperationDataResult<ValidateProfiledDataAssetsSpreadsheetContentResult>>();
            successfulResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<ValidateProfiledDataAssetsSpreadsheetContentResult>(), null))
                .Returns(successfulResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).ValidateProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, dataAssetSpreadsheet, dataAssetProfileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<ValidateProfiledDataAssetsSpreadsheetContentResult>(), null), Times.Once);
        }
        [Test]
        public async Task ValidateProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();

            var dataAssetSpreadsheet = testItems.Fixture.Create<IFormFile>();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetProfileId = "validProfileId";
            var mockSpreadsheetItems = testItems.Fixture.Create<List<DataAssetSpreadsheetItem>>();

            var exceptionMessage = "An error occurred while validating the spreadsheet content.";
            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(dataAssetProfileId))
                .Throws(new Exception(exceptionMessage));

            var failedResult = new Mock<IServiceOperationDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>(exceptionMessage, null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).ValidateProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, dataAssetSpreadsheet, dataAssetProfileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>(exceptionMessage, null), Times.Once);
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnSuccessfulResult_WhenDataFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();


            var agmUserDetails = testItems.Fixture.Create<AgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();

            // Mock the behavior of the external dependencies
            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(It.IsAny<IUserDetails>())).Returns(agmUserDetails);
            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            var successfulResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAssetSet?>>();
            successfulResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(validatedProfiledDataAssetSet, null))
                .Returns(successfulResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);

            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(validatedProfiledDataAssetSet, null), Times.Once);
        }

        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(It.IsAny<IUserDetails>()))
                .Throws(new Exception());

            var failedResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAssetSet?>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ValidatedProfiledDataAssetSet?>(It.IsAny<string>(), null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);

            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<ValidatedProfiledDataAssetSet?>(It.IsAny<string>(), null), Times.Once);
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync_ShouldReturnSuccessfulResult_WhenDataFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var agmUserDetails = testItems.Fixture.Create<AgmUserDetails>();
            var recordId = testItems.Fixture.Create<string>();
            var validatedProfiledDataAsset = testItems.Fixture.Create<IValidatedProfiledDataAsset>();

            // Mock the behavior of the external dependencies
            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(It.IsAny<IUserDetails>())).Returns(agmUserDetails);
            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetForUserAsync(agmUserDetails, recordId))
                .ReturnsAsync(validatedProfiledDataAsset);

            var successfulResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAsset>>();
            successfulResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(validatedProfiledDataAsset, null))
                .Returns(successfulResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(initiatingUserDetails, recordId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);

            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(validatedProfiledDataAsset, null), Times.Once);
        }

        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync_ShouldReturnFailedResult_WhenExceptionOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var exceptionMessage = "An error occurred while retrieving the validated profiled data asset.";
            var recordId = testItems.Fixture.Create<string>();

            // Simulate exception in dependency
            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(It.IsAny<IUserDetails>()))
                .Throws(new Exception(exceptionMessage));

            var failedResult = new Mock<IServiceOperationDataResult<IValidatedProfiledDataAsset>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IValidatedProfiledDataAsset>(exceptionMessage, null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(initiatingUserDetails, recordId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);

            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateFailedDataResult<IValidatedProfiledDataAsset>(exceptionMessage, null), Times.Once);
        }

        [Test]
        public void GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync_ShouldThrowArgumentException_WhenRecordIdIsNullOrWhitespace()
        {
            // Act & Assert
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(initiatingUserDetails, "")
            );
        }
        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnSuccessfulResult_WhenDataIsValid()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = new UserDetails()
            {
                OrganisationInformation = new OrganisationInformation { Domains = [], OrganisationId = 1, OrganisationName = "test" },
                UserContactDetails = new UserContactDetails() { EmailAddress = "test", UserName = "test" },
                UserIdSet = new UserIdSet() { DomainId =1, OrganisationId =1, UserId =1}
            };

            var profileDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();


            var request = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();
            var ckanCatalogEntryWrite = testItems.Fixture.Create<CkanCatalogEntryWrite>();

            var validatedProfiledDataAsset = new ValidatedProfiledDataAsset() 
                { AssetTitle = "tests", ProfiledDataAsset = testItems.Fixture.Create<ProfiledDataAsset>(), 
                RecordId = "test"
                , ValidationErrors = []
                };

            var validatedProfiledDataAssetSet = new ValidatedProfiledDataAssetSet() 
            { SpreadsheetName = "test", ValidatedProfiledDataAssets = new List<ValidatedProfiledDataAsset>() { validatedProfiledDataAsset } };

            var publishedItem = testItems.Fixture.Create<PublishedValidatedProfiledDataAssetsSpreadsheetContentItem>();


            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(It.IsAny<AgmUserDetails>()))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            testItems._ckanConnectionMock.Setup(x => x.AddCatalogEntryAsync(It.IsAny<CkanCatalogEntryWrite>()))
                .ReturnsAsync(Guid.NewGuid());

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(It.IsAny<string>())).Returns(profileDataAssetConverterMock.Object);

            profileDataAssetConverterMock.Setup(p => p.ConvertProfiledDataAssetPayloadToCkanCatalogEntryWrite(It.IsAny<ProfiledDataAsset>(), It.IsAny<AgmUserDetails>(), It.IsAny<DataShareRequestNotificationRecipient>()))
                .Returns(ckanCatalogEntryWrite);

            var successfulResult = new Mock<IServiceOperationDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>>();
            successfulResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>(), null))
                .Returns(successfulResult.Object);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.ClearContentForUserAsync(It.IsAny<IAgmUserDetails>()));

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            testItems._serviceOperationResultFactoryMock.Verify(x => x.CreateSuccessfulDataResult(It.IsAny<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>(), null), Times.Once);
        }

        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnFailedResult_WhenNoValidatedDataAssets()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<UserDetails>();
            var request = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();
            var agmUserDetails = testItems.Fixture.Create<AgmUserDetails>();
            var validatedProfiledDataAsset = new ValidatedProfiledDataAsset()
            {
                AssetTitle = "tests",
                ProfiledDataAsset = testItems.Fixture.Create<ProfiledDataAsset>(),
                RecordId = "test"
        ,
                ValidationErrors = []
            };

            var validatedProfiledDataAssetSet = new ValidatedProfiledDataAssetSet()
            { SpreadsheetName = "test", ValidatedProfiledDataAssets = new List<ValidatedProfiledDataAsset>() {  } };

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(It.IsAny<AgmUserDetails>()))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            var failedResult = new Mock<IServiceOperationDataResult<PublishValidatedProfiledDataAssetsSpreadsheetContentResult>>();

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>(It.IsAny<PublishValidatedProfiledDataAssetsSpreadsheetContentResult>(), null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnFailedResult_WhenValidationErrorsExist()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var request = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();
            var agmUserDetails = testItems.Fixture.Create<AgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var validatedProfiledDataAsset = testItems.Fixture.Create<ValidatedProfiledDataAsset>();
            var validationErrors = testItems.Fixture.Create<List<DataAssetValidationPropertyError>>();
            validatedProfiledDataAsset.ValidationErrors.Add("Error", validationErrors);

            //validatedProfiledDataAssetSet.Add(validatedProfiledDataAsset);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(It.IsAny<AgmUserDetails>()))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            var failedResult = new Mock<IServiceOperationDataResult<PublishValidatedProfiledDataAssetsSpreadsheetContentResult>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>(It.IsAny<PublishValidatedProfiledDataAssetsSpreadsheetContentResult>(), null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnFailedResult_WhenExceptionthrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

            var request = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();
            var agmUserDetails = testItems.Fixture.Create<AgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var validatedProfiledDataAsset = testItems.Fixture.Create<ValidatedProfiledDataAsset>();
            var validationErrors = testItems.Fixture.Create<List<DataAssetValidationPropertyError>>();
            validatedProfiledDataAsset.ValidationErrors.Add("Error", validationErrors);

            //validatedProfiledDataAssetSet.Add(validatedProfiledDataAsset);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(It.IsAny<AgmUserDetails>()))
                .Throws(new Exception());

            var failedResult = new Mock<IServiceOperationDataResult<PublishValidatedProfiledDataAssetsSpreadsheetContentResult>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>(It.IsAny<string>(), null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void PublishValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            //Arrange 
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails, null)
            );
        }

        [Test]
        public async Task ClearValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnSuccessfulResult_WhenNoExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var successfulResultMock = new Mock<IServiceOperationResult>();

            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.ClearContentForUserAsync(agmUserDetails))
                .Returns(Task.CompletedTask);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Verify(x => x.ClearContentForUserAsync(agmUserDetails), Times.Once);
        }

        [Test]
        public async Task ClearValidatedProfiledDataAssetsSpreadsheetContentAsync_ShouldReturnFailedResult_WhenExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var failedResultMock = new Mock<IServiceOperationResult>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.ClearContentForUserAsync(agmUserDetails))
                .ThrowsAsync(new Exception("error"));

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedResult(It.IsAny<string>(), null)).Returns(failedResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(failedResultMock.Object));

        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync_ShouldReturnSuccessfulResult_WhenNoExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var profiledDataAssetConverter = testItems.Fixture.Create<IProfiledDataAssetConverter>();
            var storedDataAssets = testItems.Fixture.CreateMany<SpreadsheetDataAsset>(5).ToList();
            var potentialDuplicates = testItems.Fixture.Create<List<PotentialDuplicatesToSpreadsheetItemInformation>>();
            var successfulDataResultMock = new Mock<IServiceOperationDataResult<CheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>>();
            var ckanResult = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntriesAsync(
                It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<IEnumerable<DataAssetStatus>>(), It.IsAny<ICatalogEntriesResultPagination>(),
                It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<ICatalogEntryLookupTokens>()))
                .ReturnsAsync(ckanResult);

            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.TryGetProfiledDataAssetConverterForProfileId(It.IsAny<string>(), out profiledDataAssetConverter))
                .Returns(true);

            testItems._dataAssetDuplicationDeterminationMock.Setup(x => x.DeterminePotentialDuplicatesToDataAssetsInSpreadsheet(It.IsAny<IEnumerable<SpreadsheetDataAsset>>(), It.IsAny<IEnumerable<ICddoDataAsset>>()))
                .Returns(potentialDuplicates);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<CheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>(), null)).Returns(successfulDataResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.EqualTo(successfulDataResultMock.Object));
            testItems._dataAssetDuplicationDeterminationMock.Verify(x => x.DeterminePotentialDuplicatesToDataAssetsInSpreadsheet(It.IsAny<IEnumerable<SpreadsheetDataAsset>>(), It.IsAny<IEnumerable<ICddoDataAsset>>()), Times.Once);
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync_ShouldReturnFailedResult_WhenValidatedProfiledDataAssetSetIsNull()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var failedDataResultMock = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync((ValidatedProfiledDataAssetSet?)null);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.EqualTo(failedDataResultMock.Object));
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync_ShouldReturnFailedResult_WhenProfiledDataAssetConverterIsNotFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var failedDataResultMock = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.TryGetProfiledDataAssetConverterForProfileId(It.IsAny<string>(), out It.Ref<IProfiledDataAssetConverter>.IsAny))
                .Returns(false);
            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.EqualTo(failedDataResultMock.Object));
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync_ShouldReturnFailedResult_WhenExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var exceptionMessage = "Some unexpected error occurred";
            var failedDataResultMock = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>>();

            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ThrowsAsync(new Exception(exceptionMessage));
            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(initiatingUserDetails);

            // Assert
            Assert.That(result, Is.EqualTo(failedDataResultMock.Object));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync_ShouldReturnSuccessfulResult_WhenNoExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var recordId = testItems.Fixture.Create<string>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var profiledDataAssetConverter = testItems.Fixture.Create<IProfiledDataAssetConverter>();
            var storedDataAssets = testItems.Fixture.CreateMany<SpreadsheetDataAsset>(5).ToList();
            var potentialDuplicates = testItems.Fixture.Create<PotentialDuplicatesToSpreadsheetItemInformation>();
            var successfulDataResultMock = new Mock<IServiceOperationDataResult<CheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>>();
            var ckanResult = testItems.Fixture.Create<CkanPackageSearchResponseResultSet>();


            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntriesAsync(
                It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<IEnumerable<DataAssetStatus>>(), It.IsAny<ICatalogEntriesResultPagination>(),
                It.IsAny<ICatalogEntriesOrganisationFilter>(), It.IsAny<ICatalogEntryLookupTokens>()))
                .ReturnsAsync(ckanResult);

            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.TryGetProfiledDataAssetConverterForProfileId(It.IsAny<string>(), out profiledDataAssetConverter))
                .Returns(true);

            testItems._dataAssetDuplicationDeterminationMock.Setup(x => x.DeterminePotentialDuplicatesToDataAssetInSpreadsheet(
                    recordId,
                    It.IsAny<IEnumerable<SpreadsheetDataAsset>>(),
                    It.IsAny<IEnumerable<ICddoDataAsset>>()))
                .Returns(potentialDuplicates);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<CheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>(), null)).Returns(successfulDataResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(initiatingUserDetails, recordId);

            // Assert
            Assert.That(result, Is.EqualTo(successfulDataResultMock.Object));
            testItems._dataAssetDuplicationDeterminationMock.Verify(x => x.DeterminePotentialDuplicatesToDataAssetInSpreadsheet(recordId, It.IsAny<IEnumerable<SpreadsheetDataAsset>>(), It.IsAny<IEnumerable<ICddoDataAsset>>()), Times.Once);
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync_ShouldReturnFailedResult_WhenValidatedProfiledDataAssetSetIsNull()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var recordId = testItems.Fixture.Create<string>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var failedDataResultMock = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync((ValidatedProfiledDataAssetSet?)null);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);


            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(initiatingUserDetails, recordId);

            // Assert
            Assert.That(result, Is.EqualTo(failedDataResultMock.Object));
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync_ShouldReturnFailedResult_WhenProfiledDataAssetConverterIsNotFound()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var recordId = testItems.Fixture.Create<string>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var validatedProfiledDataAssetSet = testItems.Fixture.Create<ValidatedProfiledDataAssetSet>();
            var failedDataResultMock = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ReturnsAsync(validatedProfiledDataAssetSet);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.TryGetProfiledDataAssetConverterForProfileId(It.IsAny<string>(), out It.Ref<IProfiledDataAssetConverter>.IsAny))
                .Returns(false);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(initiatingUserDetails, recordId);

            // Assert
            Assert.That(result, Is.EqualTo(failedDataResultMock.Object));
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync_ShouldReturnFailedResult_WhenExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var recordId = testItems.Fixture.Create<string>();
            var agmUserDetails = testItems.Fixture.Create<IAgmUserDetails>();
            var exceptionMessage = "Some unexpected error occurred";
            var failedDataResultMock = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>>();


            testItems._agmUserInformationBuilderMock.Setup(x => x.BuildAgmUserDetails(initiatingUserDetails))
                .Returns(agmUserDetails);

            testItems._validatedProfiledDataAssetSpreadsheetContentStoreMock.Setup(x => x.GetValidatedProfiledDataAssetSetForUserAsync(agmUserDetails))
                .ThrowsAsync(new Exception(exceptionMessage));

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(initiatingUserDetails, recordId);

            // Assert
            Assert.That(result, Is.EqualTo(failedDataResultMock.Object));
        }
        [Test]
        public async Task GetDataAssetTemplateSpreadsheetAsync_ShouldReturnSuccessfulResult_WhenNoExceptionIsThrown()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var profileId = testItems.Fixture.Create<string>();
            var mockConverter = new Mock<IProfiledDataAssetConverter>();
            var mockSpreadsheetParser = new Mock<IDataAssetSpreadsheetParser>();
            var esdaUploadTemplateSpreadsheetData = testItems.Fixture.Create<byte[]>();
            var successfulReturn = new Mock<IServiceOperationDataResult<EmbeddedResourceData>>();
            successfulReturn.Setup(x => x.Success).Returns(true);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(profileId))
                .Returns(mockConverter.Object);

            mockConverter.Setup(x => x.GetDataAssetSpreadsheetParser())
                .Returns(mockSpreadsheetParser.Object);

            mockSpreadsheetParser.Setup(x => x.GetDataAssetTemplateSpreadsheetFileName())
                .Returns("TemplateFile.xlsx");

            testItems._embeddedResourcesProviderMock.Setup(x => x.GetEmbeddedResourceDataFromAssembly("TemplateFile.xlsx", It.IsAny<System.Reflection.Assembly>()))
                .Returns(esdaUploadTemplateSpreadsheetData);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<IEmbeddedResourceData>(), null))
                .Returns(successfulReturn.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetDataAssetTemplateSpreadsheetAsync(initiatingUserDetails, profileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void GetDataAssetTemplateSpreadsheetAsync_ShouldThrowArgumentException_WhenProfileIdIsNullOrWhitespace()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var invalidProfileId = string.Empty; // Can also test with null value

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).GetDataAssetTemplateSpreadsheetAsync(initiatingUserDetails, invalidProfileId)
            );
        }

        [Test]
        public async Task GetDataAssetTemplateSpreadsheetAsync_ShouldReturnFailedResult_WhenProfiledDataAssetConverterForProfileIdFails()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var profileId = testItems.Fixture.Create<string>();
            var exceptionMessage = "Profiled data asset converter not found";
            var failedDataResultMock = new Mock<IServiceOperationDataResult<IEmbeddedResourceData>>();

            failedDataResultMock.Setup(x => x.Success).Returns(false);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(profileId))
                .Throws(new InvalidOperationException(exceptionMessage));


            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IEmbeddedResourceData>(It.IsAny<string>(), null)).Returns(failedDataResultMock.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetDataAssetTemplateSpreadsheetAsync(initiatingUserDetails, profileId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task MigrateProfiledDataAssetsFrom1p0To3p1Async_ShouldReturnSuccessfulResult_WhenMigrationIsSuccessful()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetIds = testItems.Fixture.Create<IEnumerable<Guid>>();
            var migrationList = testItems.Fixture.Create<List<IProfiledDataAssetMigrationResultV1p0ToV3p1>>();
            var successfulReturn = new Mock<IServiceOperationDataResult<MigrateProfiledDataAssetsFrom1P0To3P1Result>>();
            successfulReturn.Setup(x => x.Success).Returns(true);

            var migrationResult = new Mock<IProfiledDataAssetsMigrationV1p0ToV3p1Result>();
            migrationResult.Setup(x => x.Results).Returns(migrationList);

            testItems._profiledDataAssetsMigrationV1P0ToV3P1Mock.Setup(x => x.MigrateV1P0DataAssetsAsync(dataAssetIds))
                .ReturnsAsync(migrationResult.Object);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<MigrateProfiledDataAssetsFrom1P0To3P1Result>(), null))
                .Returns(successfulReturn.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).MigrateProfiledDataAssetsFrom1p0To3p1Async(initiatingUserDetails, dataAssetIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task MigrateProfiledDataAssetsFrom1p0To3p1Async_ShouldReturnFailedResult_WhenMigrationFails()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetIds = testItems.Fixture.Create<IEnumerable<Guid>>();
            var exceptionMessage = "Migration failed";
            var failedReturn = new Mock<IServiceOperationDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>>();
            failedReturn.Setup(x => x.Success).Returns(false);

            testItems._profiledDataAssetsMigrationV1P0ToV3P1Mock.Setup(x => x.MigrateV1P0DataAssetsAsync(dataAssetIds))
                .ThrowsAsync(new Exception(exceptionMessage));

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>(exceptionMessage, null))
                .Returns(failedReturn.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).MigrateProfiledDataAssetsFrom1p0To3p1Async(initiatingUserDetails, dataAssetIds);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.Not.Null);
        }

        [Test]
        public async Task GetEsdaOwnershipDetailsAsync_ShouldReturnSuccessfulResult_WhenDataIsValid()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();

            var ckanCatalogEntry = testItems.Fixture.Create<CkanCatalogEntryRead>();

            var cddoDataAsset = new CddoDataAsset
            {
                Id = dataAssetId,
                Title = "Sample Data Asset",
                OrganisationId = 1,
                DomainId = 1,
                DataAssetContacts = testItems.Fixture.Create<List<CddoDataAssetContact>>(),
                DataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.DomainDsrNotificationAddress,
                CustomDsrNotificationAddress = "customaddress@example.com"
            };

            var contactPoint = cddoDataAsset.DataAssetContacts.First();

            var successfulReturn = new Mock<IServiceOperationDataResult<GetEsdaOwnershipDetailsResult>>();
            successfulReturn.Setup(x => x.Success).Returns(true);

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(dataAssetId, It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ReturnsAsync(ckanCatalogEntry);

            testItems._cddoDataAssetConverterMock.Setup(x => x.ConvertCkanCatalogEntryReadToCddoDataAsset(ckanCatalogEntry))
                .Returns(cddoDataAsset);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<GetEsdaOwnershipDetailsResult>(), null))
                .Returns(successfulReturn.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetEsdaOwnershipDetailsAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task GetEsdaOwnershipDetailsAsync_ShouldReturnFailedResult_WhenAnErrorOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataAssetId = testItems.Fixture.Create<Guid>();
            var exceptionMessage = "Failed to retrieve catalog entry";
            var failedResult = new Mock<IServiceOperationDataResult<GetEsdaOwnershipDetailsResult>>();
            failedResult.Setup(x => x.Success).Returns(false);

            testItems._ckanConnectionMock.Setup(x => x.GetCatalogEntryAsync(dataAssetId, It.IsAny<ICatalogEntriesOrganisationFilter>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IGetEsdaOwnershipDetailsResult>(exceptionMessage, null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).GetEsdaOwnershipDetailsAsync(initiatingUserDetails, dataAssetId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task ValidateCataloguedResourceAsync_ShouldReturnSuccessfulResult_WhenValidationIsSuccessful()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var profileId = testItems.Fixture.Create<string>();
            var cataloguedResource = testItems.Fixture.Create<CataloguedResource>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var includeRequiredPropertiesInValidation = true;

            var dataAssetValidationMock = new Mock<IDataAssetValidation>();
            var validationResults = new Mock<IDataAssetValidationResult>();

            dataAssetValidationMock.Setup(x => x.ValidateCataloguedResource(
                    cataloguedResource, dataAssetType, includeRequiredPropertiesInValidation))
                .Returns(validationResults.Object);

            var profiledDataAssetConverterMock = new Mock<IProfiledDataAssetConverter>();
            profiledDataAssetConverterMock.Setup(x => x.GetDataAssetValidation())
                .Returns(dataAssetValidationMock.Object);

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(profileId))
                .Returns(profiledDataAssetConverterMock.Object);

            var successfulResult = new Mock<IServiceOperationDataResult<ValidateCataloguedResourceResult>>();
            successfulResult.Setup(x => x.Success).Returns(true);

            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateSuccessfulDataResult(It.IsAny<ValidateCataloguedResourceResult>(), null))
                .Returns(successfulResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).ValidateCataloguedResourceAsync(profileId, cataloguedResource, dataAssetType, includeRequiredPropertiesInValidation);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task ValidateCataloguedResourceAsync_ShouldReturnFailedResult_WhenAnErrorOccurs()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var profileId = testItems.Fixture.Create<string>();
            var cataloguedResource = testItems.Fixture.Create<CataloguedResource>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var includeRequiredPropertiesInValidation = true;
            var exceptionMessage = "Failed to validate catalogued resource";

            testItems._profiledDataAssetConverterPresenterMock.Setup(x => x.GetProfiledDataAssetConverterForProfileId(profileId))
                .Throws(new Exception(exceptionMessage));

            var failedResult = new Mock<IServiceOperationDataResult<ValidateCataloguedResourceResult>>();
            failedResult.Setup(x => x.Success).Returns(false);
            testItems._serviceOperationResultFactoryMock.Setup(x => x.CreateFailedDataResult<IValidateCataloguedResourceResult>(exceptionMessage, null))
                .Returns(failedResult.Object);

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).ValidateCataloguedResourceAsync(profileId, cataloguedResource, dataAssetType, includeRequiredPropertiesInValidation);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }
        [Test]
        public async Task SetMetadataManagement_ShouldReturnExpectedMetadata_WhenValidDatasetIsProvided()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = new Mock<IUserDetails>();
            var dataset = testItems.Fixture.Build<DataSet>()
                .With(d => d.Status, ResourceStatusEnum.Published)
                .Create();
            var userIdSet = testItems.Fixture.Create<UserIdSet>();

            initiatingUserDetails.Setup(x => x.UserIdSet).Returns(userIdSet);

            var expectedMetadata = new ManagementMetadataDcatUkApV3_1
            {
                DataAssetStatus = DataAssetStatus.Published,
                OrganisationId = userIdSet.OrganisationId.ToString(),
                DomainId = userIdSet.DomainId.ToString(),
                DataOwnerId = userIdSet.UserId.ToString(),
                Permissions = new Permissions
                {
                    ManageabilityPermissions = new ActionPermissions
                    {
                        OrganisationPermissions = new Dictionary<string, bool>
                        {
                            { userIdSet.OrganisationId.ToString(), true }
                        },
                        DomainPermissions = new Dictionary<string, bool>
                        {
                            { userIdSet.DomainId.ToString(), true }
                        }
                    }
                },
                DcatUK3_1Properties = new DcatUK3_1SpecificProperties
                {
                    AllowDSRRequest = true,
                    RequiresDSR = true
                }
            };

            // Act
            var result = await ((IDataAssetService)testItems.DataAssetService).SetMetadataManagement(dataset, initiatingUserDetails.Object, "profileId");

            // Assert
            Assert.That(result.OrganisationId, Is.EqualTo(expectedMetadata.OrganisationId));
            Assert.That(result.DataAssetStatus, Is.EqualTo(expectedMetadata.DataAssetStatus));
            Assert.That(result.DomainId, Is.EqualTo(expectedMetadata.DomainId));
        }

        [Test]
        public void SetMetadataManagement_ShouldThrowArgumentNullException_WhenDatasetStatusIsNull()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataset = testItems.Fixture.Build<DataSet>()
                .With(d => d.Status, (ResourceStatusEnum?)null) 
                .Create();


            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).SetMetadataManagement(dataset, initiatingUserDetails, "profileId"));

            Assert.That(ex.ParamName, Is.EqualTo("dataset"));
        }

        [Test]
        public void SetMetadataManagement_ShouldThrowInvalidEnumArgumentException_WhenDatasetStatusIsInvalid()
        {
            // Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();
            var dataset = testItems.Fixture.Build<DataSet>()
                .With(d => d.Status, (ResourceStatusEnum)999) // Invalid status
                .Create();


            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidEnumArgumentException>(async () =>
                await ((IDataAssetService)testItems.DataAssetService).SetMetadataManagement(dataset, initiatingUserDetails, "profileId"));

            Assert.That(ex.ParamName, Is.EqualTo("Status"));
        }

    }
}

