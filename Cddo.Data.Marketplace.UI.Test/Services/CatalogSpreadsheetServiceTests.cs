using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results.SpreadsheetIngestion.ValidatedDataAssetSpreadsheetItems;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using FluentAssertions;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Flurl.Http.Testing;
using System.Net;
using Newtonsoft.Json;
using Moq;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class CatalogSpreadsheetServiceTests
    {

        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfCatalogSpreadsheetService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            ILogger<CatalogSpreadsheetService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ICddoFlurlExceptionBuilder cddoFlurlExceptionBuilder,
            IValidatedDataAssetSpreadsheetItemSummaryBuilder validatedDataAssetSpreadsheetItemSummaryBuilder,
            IDataShareRequestMailboxAddressValidation dataShareRequestMailboxAddressValidation)
        {
            Assert.That(() => new CatalogSpreadsheetService(logger, configuration, httpContextAccessor, cddoFlurlExceptionBuilder, validatedDataAssetSpreadsheetItemSummaryBuilder, dataShareRequestMailboxAddressValidation),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }
        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<CatalogSpreadsheetService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();
            var cddoFlurlExceptionBuilder = fixture.Create<ICddoFlurlExceptionBuilder>();
            var validatedDataAssetSpreadsheetItemSummaryBuilder = fixture.Create<IValidatedDataAssetSpreadsheetItemSummaryBuilder>();
            var dataShareRequestMailboxAddressValidation = fixture.Create<IDataShareRequestMailboxAddressValidation>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor, cddoFlurlExceptionBuilder, validatedDataAssetSpreadsheetItemSummaryBuilder, dataShareRequestMailboxAddressValidation);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor, cddoFlurlExceptionBuilder, validatedDataAssetSpreadsheetItemSummaryBuilder, dataShareRequestMailboxAddressValidation);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null, cddoFlurlExceptionBuilder, validatedDataAssetSpreadsheetItemSummaryBuilder, dataShareRequestMailboxAddressValidation);
            yield return new TestCaseData("cddoFlurlExceptionBuilder", logger, configuration, httpContextAccessor, null , validatedDataAssetSpreadsheetItemSummaryBuilder, dataShareRequestMailboxAddressValidation);
            yield return new TestCaseData("validatedDataAssetSpreadsheetItemSummaryBuilder", logger, configuration, httpContextAccessor, cddoFlurlExceptionBuilder, null, dataShareRequestMailboxAddressValidation);
            yield return new TestCaseData("dataShareRequestMailboxAddressValidation", logger, configuration, httpContextAccessor, cddoFlurlExceptionBuilder, validatedDataAssetSpreadsheetItemSummaryBuilder, null);
        }

        [Test]
        public async Task DownloadSpreadsheetTemplateAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.CatalogSpreadsheetService.DownloadSpreadsheetTemplateAsync();

            //Assert
            result.Should().BeEquivalentTo(new byte[0]);
        }

        [Test]
        public async Task DownloadSpreadsheetTemplateAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.DownloadSpreadsheetTemplateAsync();
            //Assert
            result.Should().BeEquivalentTo(new byte[0]);

        }

        [Test]
        public async Task DownloadSpreadsheetTemplateAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<byte[]>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/get-data-asset-template-spreadsheet")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.DownloadSpreadsheetTemplateAsync();
            //Assert
            result.Should().NotBeNull();

        }

        [Test]
        public async Task UploadSpreadsheetAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var uploadFile = testItems.Fixture.Create<IFormFile>();

            //Act
            var result = await testItems.CatalogSpreadsheetService.UploadSpreadsheetAsync(uploadFile);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task UploadSpreadsheetAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<IFormFile>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.UploadSpreadsheetAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task UploadSpreadsheetAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<IFormFile>();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/validate-profiled-data-assets-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
           var result = await testItems.CatalogSpreadsheetService.UploadSpreadsheetAsync(mockFormFile.Object);
            //Assert
            result.Should().Be(null);

        }


        [Test]
        public async Task UploadSpreadsheetAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<IFormFile>();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

           //Act

            var testResponse = testItems.Fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/validate-profiled-data-assets-spreadsheet-content")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.UploadSpreadsheetAsync(mockFormFile.Object);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task GetValidatedDataAssetsSpreadsheetAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetsSpreadsheetAsync();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetValidatedDataAssetsSpreadsheetAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetsSpreadsheetAsync();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetValidatedDataAssetsSpreadsheetAsync_WhenApiCallThrowsJsonSerializationException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new JsonSerializationException("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetsSpreadsheetAsync();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetValidatedDataAssetsSpreadsheetAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/get-validated-profiled-data-assets-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetsSpreadsheetAsync();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetValidatedDataAssetsSpreadsheetAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/get-validated-profiled-data-assets-spreadsheet-content")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetsSpreadsheetAsync();
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task GetValidatedDataAssetSpreadsheetItemAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetSpreadsheetItemAsync(recordId);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetValidatedDataAssetSpreadsheetItemAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetValidatedDataAssetSpreadsheetItemAsync_WhenApiCallThrowsJsonSerializationException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new JsonSerializationException("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetValidatedDataAssetSpreadsheetItemAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/get-validated-profiled-data-assets-spreadsheet-item-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetValidatedDataAssetSpreadsheetItemAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetItemContentResponse>();
            var recordId = testItems.Fixture.Create<string>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/get-validated-profiled-data-assets-spreadsheet-item-content")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.GetValidatedDataAssetSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().NotBeNull();
            testItems.MockValidatedDataAssetSpreadsheetItemSummaryBuilder.Verify(v=>v.BuildFromResponse(It.IsAny<GetValidatedProfiledDataAssetsSpreadsheetItemContentResponse>()), Times.Once());

        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenRequestNotificationIsNotSupplied_NullResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a=>a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();

        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenCustomAddressIsNotSupplied_NullResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(false);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();

        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenUserTokenIsNotSupplied_NullResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();

        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenApiCallThrowsException_NullResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/publish-validated-profiled-data-assets-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();

        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenApiCallThrowsJasonException_NullResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new JsonSerializationException("not here lad"));
            
            httpTest.ForCallsTo($"http://xyz/DataAsset/publish-validated-profiled-data-assets-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();

        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenApiCallThrowsExceptionTypeException_NullResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/publish-validated-profiled-data-assets-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();

        }
        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenPublishIsSuccessful_PublishValidatedProfiledDataAssetsSpreadsheetContentResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaContactPointEmailAddress") },
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaContactPointEmailAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/publish-validated-profiled-data-assets-spreadsheet-content")
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().NotBeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();
            result.Response.Success.Should().BeTrue();
            result.DataShareRequestNotificationAddressValidationResult.RequestWasValid.Should().BeTrue();
            result.DataShareRequestNotificationAddressValidationResult.SelectedRecipientType.Should().Be(DataShareRequestNotificationRecipientType.EsdaContactPointEmailAddress);
        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenPublishIsSuccessfulForEsdaCustomDsrNotificationAddress_PublishValidatedProfiledDataAssetsSpreadsheetContentResponse()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>();

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
                { "custom-address", new StringValues("test@email.com") }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues("test@email.com");
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/publish-validated-profiled-data-assets-spreadsheet-content")
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().NotBeNull();
            result.DataShareRequestNotificationAddressValidationResult.Should().NotBeNull();
            result.Response.Success.Should().BeTrue();
            result.DataShareRequestNotificationAddressValidationResult.EnteredCustomAddress.Should().Be("test@email.com");
            result.DataShareRequestNotificationAddressValidationResult.RequestWasValid.Should().BeTrue();
            result.DataShareRequestNotificationAddressValidationResult.SelectedRecipientType.Should().Be(DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress);
        }

        [Test]
        public async Task PublishSpreadsheetDataAssetsAsync_WhenCustomeAddressGreaterThan256_Null()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            var ridiculouslyLongString = string.Join(string.Empty, testItems.Fixture.CreateMany<string>(10)) ;

            var mockFormFile = new Mock<IFormFile>();

            var fileName = "testfile.txt";
            var contentType = "text/plain";
            var content = "Hello, World!";
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(content);
            writer.Flush();
            fileStream.Position = 0;  // Rewind the stream for reading

            mockFormFile.Setup(x => x.OpenReadStream()).Returns(fileStream);
            mockFormFile.Setup(x => x.FileName).Returns(fileName);
            mockFormFile.Setup(x => x.ContentType).Returns(contentType);
            mockFormFile.Setup(x => x.Length).Returns(fileStream.Length);

            var initialFormData = new Dictionary<string, StringValues>
            {
                { "dsr-notification-option", new StringValues("EsdaCustomDsrNotificationAddress") },
                { "custom-address", new StringValues(ridiculouslyLongString.ToString()) }
            };


            var mockFormCollection = new Mock<IFormCollection>();
            mockFormCollection.Setup(x => x.Keys).Returns(initialFormData.Keys);
            foreach (var kv in initialFormData)
            {
                mockFormCollection.Setup(f => f[kv.Key]).Returns(kv.Value);
            }
            mockFormCollection.Setup(x => x.Files).Returns(new FormFileCollection { mockFormFile.Object });

            mockFormCollection.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<StringValues>.IsAny))
             .Returns((string key, out StringValues value) =>
             {
                 if (key == "dsr-notification-option")
                 {
                     value = new StringValues("EsdaCustomDsrNotificationAddress");
                     return true;
                 }
                 if (key == "custom-address")
                 {
                     value = new StringValues(ridiculouslyLongString.ToString());
                     return true;
                 }
                 value = StringValues.Empty;
                 return false;
             });

            testItems.MockDataShareRequestMailboxAddressValidation.Setup(a => a.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny)).Returns(true);
            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/publish-validated-profiled-data-assets-spreadsheet-content")
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.PublishSpreadsheetDataAssetsAsync(mockFormCollection.Object);

            result.Response.Should().BeNull();
        }

        [Test]
        public async Task ClearSpreadsheetDataAssets_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.CatalogSpreadsheetService.ClearSpreadsheetDataAssets();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task ClearSpreadsheetDataAssets_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.ClearSpreadsheetDataAssets();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task ClearSpreadsheetDataAssets_WhenApiCallThrowsJsonSerializationException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new JsonSerializationException("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.ClearSpreadsheetDataAssets();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task ClearSpreadsheetDataAssets_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/clear-validated-profiled-data-assets-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.ClearSpreadsheetDataAssets();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task ClearSpreadsheetDataAssets_WhenRequestIsSuccessful_SuccessString()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<string>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/clear-validated-profiled-data-assets-spreadsheet-content")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.ClearSpreadsheetDataAssets();
            //Assert
            result.Should().BeEquivalentTo($"\"{testResponse}\"");

        }

        #region Check for potential dupes
        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync_WhenApiCallThrowsJsonSerializationException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new JsonSerializationException("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync_WhenApiCallThrowsFlurlException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();
            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/check-for-potential-duplicates-in-validated-spreadsheet-content")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync();
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync_WhenRequestIsSuccessful_SuccessString()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/check-for-potential-duplicates-in-validated-spreadsheet-content")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync();
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();
            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(recordId);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync_WhenApiCallThrowsJsonSerializationException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new JsonSerializationException("not here lad"));

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync_WhenApiCallThrowsFlurlException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();
            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            httpTest.ForCallsTo($"http://xyz/DataAsset/check-for-potential-duplicates-in-validated-spreadsheet-item")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync_WhenRequestIsSuccessful_SuccessString()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var recordId = testItems.Fixture.Create<string>();
            var testResponse = testItems.Fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/check-for-potential-duplicates-in-validated-spreadsheet-item")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogSpreadsheetService.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(recordId);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion

        private static string GenerateJwt(string? email, string? userName)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c")); // Use a strong key
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email ?? ""),
            new Claim(JwtRegisteredClaimNames.Name, userName ?? ""),
            new Claim("role", "admin")
        };

            var token = new JwtSecurityToken(
                issuer: "test-issuer",
                audience: "test-audience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
