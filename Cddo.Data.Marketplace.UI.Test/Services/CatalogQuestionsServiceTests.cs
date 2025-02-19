using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.UI.Services;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using System.Net;
using Flurl.Http.Testing;
using Org.BouncyCastle.Asn1.Cmp;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class CatalogQuestionsServiceTests
    {
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfCatalogQuestionsService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            ILogger<CatalogQuestionsService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            Assert.That(() => new CatalogQuestionsService(logger, configuration, httpContextAccessor),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }
        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<CatalogQuestionsService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null);
        }

        [Test]
        public async Task CreateProfiledDataAssetTitleAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionFirstCreationRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            

            //Act
            var result = await testItems.CatalogQuestionService.CreateProfiledDataAssetTitleAsync(request, dataAssetType);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task CreateProfiledDataAssetTitleAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionFirstCreationRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/DataAsset/add-profiled-data-asset")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            Func<Task> result = async () => await testItems.CatalogQuestionService.CreateProfiledDataAssetTitleAsync(request, dataAssetType);
            //Assert
            await result.Should().ThrowAsync<InvalidOperationException>();

        }

        [Test]
        public async Task CreateProfiledDataAssetTitleAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionFirstCreationRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogQuestionService.CreateProfiledDataAssetTitleAsync(request, dataAssetType);
            //Assert
            result.Should().Be(null);

        }
        [Test]
        public async Task CreateProfiledDataAssetTitleAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionFirstCreationRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<AddProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/add-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.CreateProfiledDataAssetTitleAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }


        [Test]
        public async Task UpdateTitleAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionTitleRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            //ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");

            //Act
            var result = await testItems.CatalogQuestionService.UpdateTitleAsync(request, dataAssetType);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task UpdateTitleAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionTitleRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            Func<Task> result = async () => await testItems.CatalogQuestionService.UpdateTitleAsync(request, dataAssetType);
            //Assert
            await result.Should().ThrowAsync<InvalidOperationException>();

        }

        [Test]
        public async Task UpdateTitleAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionTitleRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogQuestionService.UpdateTitleAsync(request, dataAssetType);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task UpdateTitleAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionTitleRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateTitleAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateDescriptionAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionDescriptionRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateDescriptionAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateIdentifierAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionSupplierIdentifierRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateIdentifierAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateThemesAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionThemeRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateThemesAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateKeywordsAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionKeywordRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateKeywordsAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateContactPointAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionContactPointRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateContactPointAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateIssuedAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionIssuedRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateIssuedAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateSecurityClassificationAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionSecurityClassificationRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateSecurityClassificationAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateDistributionAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionDistributionRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateDistributionAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateUpdateFrequencyAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QuestionUpdateFrequencyRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateUpdateFrequencyAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateDataShareRequestNotificationsSelectionAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<DataShareRequestNotificationsRequest>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateDataShareRequestNotificationsSelectionAsync(request, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task UpdateDataAssetStatusAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<string>();
            var dataAssetStatus = testItems.Fixture.Create<DataAssetStatus>();
            var dataAssetType = testItems.Fixture.Create<DataAssetType>();
            var testResponse = testItems.Fixture.Create<PatchProfiledDataAssetResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/patch-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogQuestionService.UpdateDataAssetStatusAsync(request, dataAssetStatus, dataAssetType);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
    }

}
