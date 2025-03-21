using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Exceptions;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;
using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using FluentAssertions;
using Flurl.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Api.Test.Controllers
{
    [TestFixture]
    public class ApiControllerTests
    {
        protected readonly IFixture fixture;

        public ApiControllerTests() 
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        #region Query Dataset
        [Test]
        public async Task GivenANullQueryQueryCataloguedResources_WhenQueryingCatalogueResouses_ThenAnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();

            //Act
            var result = (BadRequestObjectResult)await testItems.DataMarketApiController.QueryCataloguedResources(string.Empty);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00012");
            errorMessage.Message.Should().Be("Validation failures");
        }

        [Test]
        public async Task GivenAQueryQueryCataloguedResources_WhenEnvironmentIsSandBox_ReturnStubbedResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");

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
                            new() { Name = "John Doe", Email = "contact@example.com", Role = ContactRoleEnum.Owner }
                        },
                        Description = "This is a mocked dataset description for sandbox.",
                        Keyword = new List<string> { "mock", "dataset", "Sandbox" },
                        Modified = DateTime.UtcNow.AddDays(-1),
                        Publisher = "Mock Publisher",
                        SecurityClassification = SecurityClassificationEnum.Official,
                        Status = ResourceStatusEnum.Published,
                        SupplierIdentifier = "supplier-12345",
                        Theme = new List<string> { nameof(ThemeEnum.Education), nameof(ThemeEnum.ScienceAndTechnology) }
                    },
                    new()
                    {
                        Type = ResourceEnum.DataService,
                        Identifier = Guid.NewGuid().ToString(),
                        Title = "Mocked Data Service Title",
                        AccessRights = AccessRightsEnum.Internal,
                        ContactPoint = new List<Contact>
                        {
                            new() { Name = "Jane Smith", Email = "servicecontact@example.com", Role = ContactRoleEnum.Contact }
                        },
                        Description = "This is a mocked data service description for sandbox.",
                        Keyword = new List<string> { "mock", "data-service", "Sandbox" },
                        Modified = DateTime.UtcNow.AddDays(-3),
                        Publisher = "Another Mock Publisher",
                        SecurityClassification = SecurityClassificationEnum.Secret,
                        Status = ResourceStatusEnum.Draft,
                        SupplierIdentifier = "supplier-67890",
                        Theme = new List<string> { nameof(ThemeEnum.HealthAndCare), nameof(ThemeEnum.TransportAndInfrastructure) }
                    }
                };

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockModelValidationService.Setup(v => v.GetMockedCataloguedResources()).Returns(mockResponse);

            var result = (OkObjectResult)await testItems.DataMarketApiController.QueryCataloguedResources("string.Empty");

            //Assert
            result.Value.Should().NotBeNull();
            var dataResources = result.Value as List<CataloguedResource>;
            dataResources.Should().NotBeNull();
            dataResources.Should().BeEquivalentTo(mockResponse);
        }

        [Test]
        public async Task GivenAQueryQueryCataloguedResources_WhenEnvironmentIsNotSandBoxAndGetDataFails_ReturnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<DataAssetType>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>())).ReturnsAsync(mockResult.Object);

            var result = (ObjectResult)await testItems.DataMarketApiController.QueryCataloguedResources("gov engagement");

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00015");
            errorMessage.Message.Should().Be("An internal server error occurred while querying catalogued resources.");
        }

        [Test]
        public async Task GivenAQueryQueryCataloguedResources_WhenEnvironmentIsNotSandBox_CataloguedResources()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new List<ProfiledDataAsset>() 
            { 
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };
            
            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d=>d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<DataAssetType>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>())).ReturnsAsync(mockResult.Object);

            var result = (OkObjectResult)await testItems.DataMarketApiController.QueryCataloguedResources("gov engagement");

            //Assert
            result.Value.Should().NotBeNull();
            var datasets = result.Value as List<CataloguedResource>;
            datasets.Count.Should().Be(1);
        }

        [Test]
        public async Task GivenAQueryQueryCataloguedResources_WhenGetProfiledDataAssetsAsyncExceptionIsThrown_500Response()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<DataAssetType>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>())).ThrowsAsync(new Exception("Something went kaput"));

            var result = (ObjectResult)await testItems.DataMarketApiController.QueryCataloguedResources("gov engagement");

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
        }
       
        [Test]
        public async Task GivenADatasetIdQueryCataloguedResources_WhenGetProfiledDataAssetAsyncExceptionIsThrown_500Response()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
            };

            var data = new Mock<IGetProfiledDataAssetResult>();
            data.Setup(d => d.ProfiledDataAsset).Returns(resultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<DataAssetType>>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()
                    )).ThrowsAsync(new Exception("Something went kaput"));

            var result = (ObjectResult)await testItems.DataMarketApiController.QueryCataloguedResources(datasetId);

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
        }

#endregion

        #region Retrieve Dataset
        [Test]
        public async Task GivenRetrieveDataset_WhenGetProfiledDataAssetAsyncThrows_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new ProfiledDataAsset()
            {
                DataAssetType = DataAssetType.DataSet,
                ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                Payload = payload,
                ProfileId = "dv-Uk1"
            };

            var data = new Mock<IGetProfiledDataAssetResult>();
            data.Setup(d => d.ProfiledDataAsset).Returns(resultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).Throws(new Exception("This is brokered"));


            var result = (ObjectResult)await testItems.DataMarketApiController.RetrieveDataset(datasetId);

            //Assert
            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00011");
            errorMessage.Message.Should().Be($"Internal server error occurred while retrieving dataset with ID {datasetId}.");
        }

        [Test]
        public async Task GivenANullDatasetIdRetrieveDataset_WhenQueryingCatalogueResources_ThenAnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();

            //Act
            var result = (BadRequestObjectResult)await testItems.DataMarketApiController.RetrieveDataset(string.Empty);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00012");
            errorMessage.Message.Should().Be("Validation failures");
        }

        [Test]
        public async Task GivenADataSetIdQueryCataloguedResources_WhenEnvironmentIsSandBox_ReturnStubbedResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var mockDataset = new DataSet
            {
                Type = ResourceEnum.DataSet,
                Identifier = datasetId,
                Title = "Mocked Dataset Title",
                AccessRights = AccessRightsEnum.Open,
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mock Dataset Owner", Email = "owner@example.com", Role = ContactRoleEnum.Owner }
                    },
                Description = "This is a mocked dataset description tailored for the sandbox environment.",
                Keyword = new List<string> { "Sandbox", "mocked", "dataset" },
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
                UpdateFrequency = "Yearly"
            };

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (OkObjectResult)await testItems.DataMarketApiController.RetrieveDataset(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as CataloguedResource;
            errorMessage.Should().NotBeNull();
        }

        [Test]
        public async Task GivenADatasetIdQueryCataloguedResources_WhenEnvironmentIsNotSandBoxAndGetDataFails_ReturnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            var result = (ObjectResult)await testItems.DataMarketApiController.RetrieveDataset(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00010");
            errorMessage.Message.Should().Be($"Dataset with identifier {datasetId} does not exist.");
        }

        [Test]
        public async Task GivenADatasetIdQueryCataloguedResources_WhenEnvironmentIsNotSandBox_CataloguedResource()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new ProfiledDataAsset()
            {
                DataAssetType = DataAssetType.DataSet,
                ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                Payload = payload,
                ProfileId = "dv-Uk1"
            };

            var data = new Mock<IGetProfiledDataAssetResult>();
            data.Setup(d => d.ProfiledDataAsset).Returns(resultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);


            var result = (OkObjectResult)await testItems.DataMarketApiController.RetrieveDataset(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var datasets = result.Value as CataloguedResource;
            datasets.Title.Should().Be(payloadObject.Title);
        }


        #endregion

        #region Create Dataset
        [Test]
        public async Task CreateDataset_WhenDatasetModelStateHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v=>v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            controller.ModelState.AddModelError("Title", "Missing title property");


            var result = (BadRequestObjectResult)await controller.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task CreateDataset_WhenDatasetIsNull_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);

            var result = (BadRequestObjectResult)await controller.CreateDataset(null);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task CreateDataset_WhenValidationResponseHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;
            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            var result = (BadRequestObjectResult)await controller.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task CreateDatasetIsSandboxEnvironment_WhenDatasetIsInvalid_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));
            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataset(new DataSet()); 

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDatasetIsSandboxEnvironment_WhenDatasetIsValid_SandBoxDatasetCreated()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);
            var result = (CreatedResult)await testItems.DataMarketApiController.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(201, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataset_WhenDataseSupplierIdentifierIsProvided_CheckforGetProfiledDataAssetsFails_500ErrorResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataset(new DataSet() { SupplierIdentifier = "SupplierTest"});

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataset_WhenDataseSupplierIdentifierIsProvided_WhenThereAreConflictsConflictResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataset(new DataSet() { SupplierIdentifier = "SupplierTest" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(409, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataset_WhenDatasetSupplierIdentifierIsNotProvidedAndAddProfiledDataAssetIsFalse_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataset_WhenDatasetSupplierIdentifierIsNotProvidedAndAddProfiledDataAssetIsTrue_CreatedDataset()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (CreatedResult)await testItems.DataMarketApiController.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(201, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataset_WhenDatasetSupplierIdentifierIsNotProvidedAndJasonExceptionIsThrown_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new System.Text.Json.JsonException("Cant park here mate"));

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("Invalid dataset format. JSON deserialization failed.", Is.EqualTo(resultErrorMessage.Message));
        }

        [Test]
        public async Task CreateDataset_WhenDatasetSupplierIdentifierIsNotProvidedAndExceptionIsThrown_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new JsonException("Cant park here mate"));

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataset(new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("An internal server error occurred while creating the dataset.", Is.EqualTo(resultErrorMessage.Message));
        }

#endregion

        #region Update Dataset

        [Test]
        public async Task UpdateDataset_WhenDatasetModelStateHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            controller.ModelState.AddModelError("Title", "Missing title property");


            var result = (BadRequestObjectResult)await controller.UpdateDataset(string.Empty, new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDataset_WhenDatasetIdIsNull_NotFoundErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);

            var result = (NotFoundObjectResult)await controller.UpdateDataset(string.Empty, new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDataset_WhenPatchDatasetIsNull_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);

            var result = (BadRequestObjectResult)await controller.UpdateDataset("datasetId", (DataSet)null);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDataset_WhenValidationResponseHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;
            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            var result = (BadRequestObjectResult)await controller.UpdateDataset("datasetId", new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDatasetIsSandboxEnvironment_WhenDatasetIsInvalid_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));
            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataset("datasetId", new DataSet());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDatasetIsSandboxEnvironment_WhenDatasetIsValid_SandBoxDatasetUpdated()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();
            var updatedDataset = fixture.Create<DataSet>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            testItems.MockModelValidationService.Setup(v => v.GetMockedUpdatedDataset(It.IsAny<string>(), It.IsAny<DataSet>())).Returns(updatedDataset);
            var result = (OkObjectResult)await testItems.DataMarketApiController.UpdateDataset("datasetId", new DataSet());

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(updatedDataset);
        }

        [Test]
        public async Task UpadateDataset_WhenDataseSupplierIdentifierIsProvided_WhenThereAreConflictsConflictResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataset("SupplierTest", new DataSet() { Identifier = "SupplierTestxx" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(409, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDataset_WhenPatchProfiledDataAssetIsFalse_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IPatchDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.UpdateDataset("SupplierTest", new DataSet() { Identifier = "SupplierTest" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDataset_WhenPatchProfiledDataAssetIsFalse_UpdatedDataset()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockUpdateResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockUpdateResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var updateData = new Mock<IPatchDataAssetResult>();
            updateData.Setup(d => d.DataAssetId).Returns(datasetId);
            mockUpdateResult.Setup(r => r.Data).Returns(updateData.Object);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockUpdateResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.UpdateDataset(datasetId.ToString(), new DataSet() { Identifier = datasetId.ToString(), Status = ResourceStatusEnum.Published });

            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDataset_WhenPatchProfiledDataAssetIsTrue_UpdatedDataset()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockUpdateResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockUpdateResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var updatedPayload = new ProfiledDataAsset()
            {
                DataAssetType = DataAssetType.DataSet,
                ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                Payload = payload,
                ProfileId = "dv-Uk1"
            };

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var updateData = new Mock<IPatchDataAssetResult>();
            updateData.Setup(d => d.DataAssetId).Returns(datasetId);
            mockUpdateResult.Setup(r => r.Data).Returns(updateData.Object);

            var data = new Mock<IGetProfiledDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            data.Setup(d => d.ProfiledDataAsset).Returns(updatedPayload);
            mockResult.Setup(r => r.Data).Returns(data.Object);
            

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockUpdateResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (OkObjectResult)await testItems.DataMarketApiController.UpdateDataset(datasetId.ToString(), new DataSet() { Identifier = datasetId.ToString() });

            //Assert
            result.Should().NotBeNull();
            Assert.That(200, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataset_WhenPatchProfiledDataAssetAsyncThrowsUnAuthorizedAccessToDataAssetException_ForbidenErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new UnAuthorizedAccessToDataAssetException());

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataset(datasetId.ToString(), new DataSet() { Identifier = datasetId.ToString() });

            //Assert
            result.Should().NotBeNull();
            Assert.That(403, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("TODO", Is.EqualTo(resultErrorMessage.Message));
        }
        [Test]
        public async Task CreateDataset_WhenPatchProfiledDataAssetAsyncThrowsException_ForbidenErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new Exception("Its broked"));

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataset(datasetId.ToString(), new DataSet() { Identifier = datasetId.ToString() });

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That($"Internal server error occurred while patching dataset with ID {datasetId}.", Is.EqualTo(resultErrorMessage.Message));
        }
#endregion

        #region Delete Dataset
        [Test]
        public async Task GivenANullDatasetId_WhenRemoveDataset_ThenNotFoundErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();

            //Act
            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.RemoveDataset(string.Empty);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00010");
            errorMessage.Message.Should().Be("Dataset identifier is missing or invalid.");
        }

        [Test]
        public async Task GivenAnIvalidDataSetIdToemoveDataset_WhenEnvironmentIsSandBox_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));

            var result = (ObjectResult)await testItems.DataMarketApiController.RemoveDataset(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var response = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAValidDataSetIdToemoveDataset_WhenEnvironmentIsSandBox_NoContent()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (NoContentResult)await testItems.DataMarketApiController.RemoveDataset(datasetId);

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAValidDataSetToemoveDataset_WhenDeleteResultIsUnsuccessful_NotFound()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();
           

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.RemoveDataset(datasetId);

            //Assert
            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataset_WhenDeleteResultIsSuccessful_NotContent()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (NoContentResult)await testItems.DataMarketApiController.RemoveDataset(datasetId);

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataset_WhenDeleteProfiledDataAssetAsyncThrowsUnAuthorizedAccessToDataAssetException_Forbidden()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).Throws(new UnAuthorizedAccessToDataAssetException());

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.RemoveDataset(datasetId);

            //Assert
            //Assert
            result.Should().NotBeNull();
            Assert.That(403, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("TODO", Is.EqualTo(resultErrorMessage.Message));
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataset_WhenDeleteProfiledDataAssetAsyncThrowsException_Forbidden()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).Throws(new Exception("Cant do it"));

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.RemoveDataset(datasetId);

            //Assert
            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That($"Internal server error occurred while deleting dataset with ID {datasetId}.", Is.EqualTo(resultErrorMessage.Message));
        }

        #endregion

        #region Retrieve Dataservice
        [Test]
        public async Task GivenRetrieveDataservice_WhenGetProfiledDataAssetAsyncThrows_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new ProfiledDataAsset()
            {
                DataAssetType = DataAssetType.DataSet,
                ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                Payload = payload,
                ProfileId = "dv-Uk1"
            };

            var data = new Mock<IGetProfiledDataAssetResult>();
            data.Setup(d => d.ProfiledDataAsset).Returns(resultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).Throws(new Exception("This is brokered"));


            var result = (ObjectResult)await testItems.DataMarketApiController.RetrieveDataService(datasetId);

            //Assert
            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00011");
            errorMessage.Message.Should().Be($"Internal server error occurred while retrieving data service with ID {datasetId}.");
        }

        [Test]
        public async Task GivenANullDatasetIdRetrieveDataService_WhenQueryingCatalogueResouses_ThenAnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();

            //Act
            var result = (BadRequestObjectResult)await testItems.DataMarketApiController.RetrieveDataService(string.Empty);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00012");
            errorMessage.Message.Should().Be("Validation failures");
        }

        [Test]
        public async Task GivenADataSetIdRetrieveDataService_WhenEnvironmentIsSandBox_ReturnStubbedResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var mockDataset = new DataSet
            {
                Type = ResourceEnum.DataSet,
                Identifier = datasetId,
                Title = "Mocked Dataset Title",
                AccessRights = AccessRightsEnum.Open,
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mock Dataset Owner", Email = "owner@example.com", Role = ContactRoleEnum.Owner }
                    },
                Description = "This is a mocked dataset description tailored for the sandbox environment.",
                Keyword = new List<string> { "Sandbox", "mocked", "dataset" },
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
                UpdateFrequency = "Yearly"
            };

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var result = (OkObjectResult)await testItems.DataMarketApiController.RetrieveDataService(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as CataloguedResource;
            errorMessage.Should().NotBeNull();
        }

        [Test]
        public async Task GivenADataSetIdRetrieveDataService_WhenEnvironmentIsSandBoxNoDataSetForTheID_ReturnStubbedResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));

            var result = (ObjectResult)await testItems.DataMarketApiController.RetrieveDataService(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
        }

        [Test]
        public async Task GivenADatasetIdRetrieveDataService_WhenEnvironmentIsNotSandBoxAndGetDataFails_ReturnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            var result = (ObjectResult)await testItems.DataMarketApiController.RetrieveDataService(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00010");
            errorMessage.Message.Should().Be($"Data service with identifier {datasetId} does not exist.");
        }

        [Test]
        public async Task GivenADatasetIdQueryRetrieveDataService_WhenEnvironmentIsNotSandBox_CataloguedResource()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var resultList = new ProfiledDataAsset()
            {
                DataAssetType = DataAssetType.DataSet,
                ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                Payload = payload,
                ProfileId = "dv-Uk1"
            };

            var data = new Mock<IGetProfiledDataAssetResult>();
            data.Setup(d => d.ProfiledDataAsset).Returns(resultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);


            var result = (OkObjectResult)await testItems.DataMarketApiController.RetrieveDataService(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var datasets = result.Value as CataloguedResource;
            datasets.Title.Should().Be(payloadObject.Title);
        }


        #endregion

        #region Create DataService
        [Test]
        public async Task CreateDataService_WhenDatasetModelStateHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            controller.ModelState.AddModelError("Title", "Missing title property");


            var result = (BadRequestObjectResult)await controller.CreateDataService(new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task CreateDataService_WhenDatasetIsNull_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);

            var result = (BadRequestObjectResult)await controller.CreateDataService(null);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task CreateDataService_WhenValidationResponseHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;
            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            var result = (BadRequestObjectResult)await controller.CreateDataService(new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task CreateDataServiceIsSandboxEnvironment_WhenDatasetIsInvalid_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));
            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataService(new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataServiceIsSandboxEnvironment_WhenDatasetIsValid_SandBoxDatasetCreated()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);
            var result = (CreatedResult)await testItems.DataMarketApiController.CreateDataService(new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(201, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataService_WhenDataseSupplierIdentifierIsProvided_CheckforGetProfiledDataAssetsFails_500ErrorResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataService(new DataService() { SupplierIdentifier = "SupplierTest" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataService_WhenDataseSupplierIdentifierIsProvided_WhenThereAreConflictsConflictResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataService(new DataService() { SupplierIdentifier = "SupplierTest" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(409, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataService_WhenDatasetSupplierIdentifierIsNotProvidedAndAddProfiledDataAssetIsFalse_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataService(new DataService() { Status = ResourceStatusEnum.Draft });

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataService_WhenDatasetSupplierIdentifierIsNotProvidedAndAddProfiledDataAssetIsTrue_CreatedDataset()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (CreatedResult)await testItems.DataMarketApiController.CreateDataService(new DataService() { Status = ResourceStatusEnum.Draft });

            //Assert
            result.Should().NotBeNull();
            Assert.That(201, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataService_WhenDatasetSupplierIdentifierIsNotProvidedAndJasonExceptionIsThrown_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new System.Text.Json.JsonException("Cant park here mate"));

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataService(new DataService() { Status = ResourceStatusEnum.Draft });

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("Invalid data service format. JSON deserialization failed.", Is.EqualTo(resultErrorMessage.Message));
        }

        [Test]
        public async Task CreateDataService_WhenDatasetSupplierIdentifierIsNotProvidedAndExceptionIsThrown_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IAddDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.AddProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new JsonException("Cant park here mate"));

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.CreateDataService(new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("An internal server error occurred while creating the data service.", Is.EqualTo(resultErrorMessage.Message));
        }

        #endregion

        #region Update DataService

        [Test]
        public async Task DataService_WhenDatasetModelStateHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            controller.ModelState.AddModelError("Title", "Missing title property");


            var result = (BadRequestObjectResult)await controller.UpdateDataService(string.Empty, new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task DataService_WhenDatasetIdIsNull_NotFoundErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);

            var result = (NotFoundObjectResult)await controller.UpdateDataService(string.Empty, new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDataService_WhenPatchDatasetIsNull_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;

            //Act
            testItems.MockModelValidationService.Setup(v => v.RecordModelStateErrorsAndBuildErrorResponse(It.IsAny<ActionContext>(), It.IsAny<IUserDetails>())).Returns(errorMessage);

            var result = (BadRequestObjectResult)await controller.UpdateDataService("datasetId", (DataService)null);

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDataService_WhenValidationResponseHasErrors_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var errorMessage = fixture.Create<ErrorMessage>();
            var controller = testItems.DataMarketApiController;
            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns(errorMessage);
            var result = (BadRequestObjectResult)await controller.UpdateDataService("datasetId", new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));

        }

        [Test]
        public async Task UpdateDataServiceIsSandboxEnvironment_WhenDatasetIsInvalid_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));
            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataService("datasetId", new DataService());

            //Assert
            result.Should().NotBeNull();
            Assert.That(400, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDataServiceIsSandboxEnvironment_WhenDatasetIsValid_SandBoxDatasetUpdated()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var errorMessage = fixture.Create<ErrorMessage>();
            var updatedDataset = fixture.Create<DataService>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            //Act
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var data = new Mock<IValidateCataloguedResourceResult>();
            data.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
            It.IsAny<bool>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);
            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            testItems.MockModelValidationService.Setup(v => v.GetMockedUpdatedDataService(It.IsAny<string>())).Returns(updatedDataset);
            var result = (OkObjectResult)await testItems.DataMarketApiController.UpdateDataService("datasetId", new DataService());

            //Assert
            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(updatedDataset);
        }

        [Test]
        public async Task UpadateDataService_WhenDataseSupplierIdentifierIsProvided_WhenThereAreConflictsConflictResponse()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var resultList = new List<ProfiledDataAsset>()
            {
                new ProfiledDataAsset()
                {
                    DataAssetType = DataAssetType.DataSet,
                    ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                    Payload = payload,
                    ProfileId = "dv-Uk1"
                }
            };

            var data = new Mock<IGetProfiledDataAssetsResult>();
            data.Setup(d => d.ProfiledDataAssets).Returns(resultList.ToArray());
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetsAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<IEnumerable<DataAssetType>>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataService("SupplierTest", new DataService() { Identifier = "SupplierTestxx" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(409, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDataService_WhenPatchProfiledDataAssetIsFalse_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var data = new Mock<IPatchDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(datasetId);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.UpdateDataService("SupplierTest", new DataService() { Identifier = "SupplierTest" });

            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task UpdateDataService_WhenPatchProfiledDataAssetIsTrue_UpdatedDataService()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockUpdateResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockUpdateResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var updatedPayload = new ProfiledDataAsset()
            {
                DataAssetType = DataAssetType.DataSet,
                ManagementMetadata = fixture.Create<ManagementMetadataBase>(),
                Payload = payload,
                ProfileId = "dv-Uk1"
            };

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            var updateData = new Mock<IPatchDataAssetResult>();
            updateData.Setup(d => d.DataAssetId).Returns(datasetId);
            mockUpdateResult.Setup(r => r.Data).Returns(updateData.Object);

            var data = new Mock<IGetProfiledDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            data.Setup(d => d.ProfiledDataAsset).Returns(updatedPayload);
            mockResult.Setup(r => r.Data).Returns(data.Object);


            testItems.MockDataAssetService.Setup(d => d.GetProfiledDataAssetAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).ReturnsAsync(mockUpdateResult.Object);
            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (OkObjectResult)await testItems.DataMarketApiController.UpdateDataService(datasetId.ToString(), new DataService() { Identifier = datasetId.ToString(), Status = ResourceStatusEnum.Draft });

            //Assert
            result.Should().NotBeNull();
            Assert.That(200, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task CreateDataService_WhenPatchProfiledDataAssetAsyncThrowsUnAuthorizedAccessToDataAssetException_ForbidenErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new UnAuthorizedAccessToDataAssetException());

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataService(datasetId.ToString(), new DataService() { Identifier = datasetId.ToString() });

            //Assert
            result.Should().NotBeNull();
            Assert.That(403, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("TODO", Is.EqualTo(resultErrorMessage.Message));
        }
        [Test]
        public async Task CreateDataService_WhenPatchProfiledDataAssetAsyncThrowsException_ForbidenErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var errorMessage = fixture.Create<ErrorMessage>();
            var datasetId = fixture.Create<Guid>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var validationMockResult = new Mock<IServiceOperationDataResult<IValidateCataloguedResourceResult>>();
            validationMockResult.Setup(r => r.Success).Returns(true);

            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var validationResultList = fixture.Create<List<IDataAssetValidationPropertyResult>>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new StringEnumConverter() }
            };

            var payloadObject = fixture.Create<CataloguedResource>();
            var payload = JsonConvert.SerializeObject(payloadObject, settings);

            var validationData = new Mock<IValidateCataloguedResourceResult>();
            validationData.Setup(d => d.DataAssetValidationPropertyResults).Returns(validationResultList);
            validationMockResult.Setup(r => r.Data).Returns(validationData.Object);

            testItems.MockDataAssetService.Setup(d => d.ValidateCataloguedResourceAsync(
                    It.IsAny<string>(),
                    It.IsAny<CataloguedResource>(),
                    It.IsAny<DataAssetType>(),
                    It.IsAny<bool>())).ReturnsAsync(validationMockResult.Object);

            testItems.MockDataAssetService.Setup(d => d.PatchProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<DataAssetType>(),
                   It.IsAny<string>(),
                   It.IsAny<ManagementMetadataBase>(),
                   It.IsAny<DataAssetActionSourceEnum>())).Throws(new Exception("Its broked"));

            testItems.MockModelValidationService.Setup(v => v.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>(), It.IsAny<IUserDetails>())).Returns((ErrorMessage)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.UpdateDataService(datasetId.ToString(), new DataService() { Identifier = datasetId.ToString() });

            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That($"Internal server error occurred while patching data service with ID {datasetId}.", Is.EqualTo(resultErrorMessage.Message));
        }
        #endregion

        #region Delete Dataset
        [Test]
        public async Task GivenANullDatasetId_WhenRemoveDataService_ThenNotFoundErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();

            //Act
            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.RemoveDataService(string.Empty);

            //Assert
            result.Value.Should().NotBeNull();
            var errorMessage = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
            errorMessage.Code.Should().Be("DM00010");
            errorMessage.Message.Should().Be("Data service identifier is missing or invalid.");
        }

        [Test]
        public async Task GivenAnIvalidDataSetIdToRemoveDataService_WhenEnvironmentIsSandBox_ErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((400, errorMessage));

            var result = (ObjectResult)await testItems.DataMarketApiController.RemoveDataService(datasetId);

            //Assert
            result.Value.Should().NotBeNull();
            var response = result.Value as ErrorMessage;
            errorMessage.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAValidDataSetIdToRemoveDataService_WhenEnvironmentIsSandBox_NoContent()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "test");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();

            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (NoContentResult)await testItems.DataMarketApiController.RemoveDataService(datasetId);

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataService_WhenDeleteResultIsUnsuccessful_NotFound()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (NotFoundObjectResult)await testItems.DataMarketApiController.RemoveDataService(datasetId);

            //Assert
            //Assert
            result.Should().NotBeNull();
            Assert.That(404, Is.EqualTo(result.StatusCode));
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataService_WhenDeleteResultIsSuccessful_NotContent()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).ReturnsAsync(mockResult.Object);

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (NoContentResult)await testItems.DataMarketApiController.RemoveDataService(datasetId);

            //Assert
            result.Should().NotBeNull();
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataService_WhenDeleteProfiledDataAssetAsyncThrowsUnAuthorizedAccessToDataAssetException_Forbidden()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).Throws(new UnAuthorizedAccessToDataAssetException());

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.RemoveDataService(datasetId);

            //Assert
            //Assert
            result.Should().NotBeNull();
            Assert.That(403, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That("TODO", Is.EqualTo(resultErrorMessage.Message));
        }

        [Test]
        public async Task GivenAValidDataSetToRemoveDataService_WhenDeleteProfiledDataAssetAsyncThrowsException_Forbidden()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var mockHttpContext = new Mock<HttpContext>();
            var mockUser = new Mock<ClaimsPrincipal>();
            var emailClaim = new Claim("environment", "dev");
            var datasetId = Guid.NewGuid().ToString();
            var errorMessage = fixture.Create<ErrorMessage>();


            //Act
            mockUser.Setup(u => u.Claims)
                .Returns(new List<Claim> { emailClaim });

            mockHttpContext.Setup(c => c.User).Returns(mockUser.Object);

            testItems.DataMarketApiController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IDeleteDataAssetResult>();
            data.Setup(d => d.DataAssetId).Returns(new Guid(datasetId));
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.DeleteProfiledDataAssetAsync(
                   It.IsAny<IUserDetails>(),
                   It.IsAny<string>(),
                   It.IsAny<Guid>())).Throws(new Exception("Cant do it"));

            testItems.MockModelValidationService.Setup(v => v.HandleSimulatedErrors(It.IsAny<CataloguedResource>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((ValueTuple<int, ErrorMessage>?)null);

            var result = (ObjectResult)await testItems.DataMarketApiController.RemoveDataService(datasetId);

            //Assert
            //Assert
            result.Should().NotBeNull();
            Assert.That(500, Is.EqualTo(result.StatusCode));
            var resultErrorMessage = result.Value as ErrorMessage;
            Assert.That($"Internal server error occurred while deleting data service with ID {datasetId}.", Is.EqualTo(resultErrorMessage.Message));
        }

        #endregion


    }
}
