using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.Lookup;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Requests.DataShareRequests;
using Cddo.Data.Marketplace.Logic.Exceptions;
using FluentAssertions;
using Flurl.Http.Testing;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class CatalogDataServiceTests
    {

        #region Topics
        [Test]
        public async Task GetCddoTopicsAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            //Act
            var result = await testItems.CatalogService.GetCddoTopicsAsync();
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetCddoTopicsAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetCddoTopicsAsync();
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetCddoTopicsAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetCddoTopicsAsync();
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetCddoTopicsAsync_WhenTopicsArePresent_ListOfTopics()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetCddoTopicsResponse>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/Topics")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetCddoTopicsAsync();
            //Assert
            result.Should().BeEquivalentTo(testResponse.Topics);

        }
        #endregion

        #region Organisation
        [Test]
        public async Task GetCddoOrganisationsAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            //Act
            var result = await testItems.CatalogService.GetCddoOrganisationsAsync();
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetCddoOrganisationsAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetCddoOrganisationsAsync();
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetCddoOrganisationsAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetCddoOrganisationsAsync();
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetCddoOrganisationsAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetCddoOrganisationsResponse>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/Organisations")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetCddoOrganisationsAsync();
            //Assert
            result.Should().BeEquivalentTo(testResponse.Organisations);

        }
        #endregion

        #region GetSearchSuggestionsForPublishedDataAssets
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssetsAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForPublishedDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssetsAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForPublishedDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssetsAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForPublishedDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssetsAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetSearchSuggestionsForPublishedDataAssetsResponse>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/DataAsset/get-search-suggestions-for-published-data-assets")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForPublishedDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(testResponse.SearchSuggestionsForPublishedDataAssets);

        }
        #endregion

        #region GetSearchSuggestionsForOrganisationDataAssets
        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssetsAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForOrganisationDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssetsAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForOrganisationDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssetsAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForOrganisationDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(Enumerable.Empty<string>());

        }

        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssetsAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetSearchSuggestionsForOrganisationDataAssetsResponse>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/get-search-suggestions-for-organisation-data-assets")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetSearchSuggestionsForOrganisationDataAssetsAsync("test");
            //Assert
            result.Should().BeEquivalentTo(testResponse.SearchSuggestionsForOrganisationDataAssets);

        }
        #endregion

        #region CheckForPotentialDuplicatesToDataAssetAsync
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            //Act
            var result = await testItems.CatalogService.CheckForPotentialDuplicatesToDataAssetAsync(Guid.NewGuid());
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.CheckForPotentialDuplicatesToDataAssetAsync(Guid.NewGuid());
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);


            //Act
            var result = await testItems.CatalogService.CheckForPotentialDuplicatesToDataAssetAsync(Guid.NewGuid());

            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<CheckForPotentialDuplicatesToDataAssetResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/check-for-potential-duplicates-to-data-asset")
               .RespondWithJson(testResponse);
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            //Act
            var result = await testItems.CatalogService.CheckForPotentialDuplicatesToDataAssetAsync(Guid.NewGuid());
            //Assert
            result.PotentialDuplicatesToDataAsset.Should().BeEquivalentTo(testResponse.PotentialDuplicatesToDataAsset);

        }
        #endregion

        #region GetDataAssetsAsync

        [Test]
        public async Task GetDataAssetsAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetsAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetsAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetsAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetsAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            var testResponse = new GetCddoDataAssetsResponse()
            {
                CddoDataAssets = new List<CddoDataAsset>(),
                TotalNumberOfMatchingCddoDataAssets = 1
            };

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/get-cddo-data-assets")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetDataAssetsAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion

        #region GetDataAssetsByUserAsync

        [Test]
        public async Task GetDataAssetsByUserAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();
            //Act
            var result = await testItems.CatalogService.GetDataAssetsByUserAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetsByUserAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetsByUserAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetsByUserAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetsByUserAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetsByUserAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<GetCddoDataAssetsRequest>();
            var testResponse = new GetCddoDataAssetsResponse()
            {
                CddoDataAssets = new List<CddoDataAsset>(),
                TotalNumberOfMatchingCddoDataAssets = 1
            };

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/DataAsset/get-cddo-data-assets")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetDataAssetsByUserAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion

        #region GetDataAssetAsync

        [Test]
        public async Task GetDataAssetAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<Guid>();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<Guid>();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<Guid>();
            var testResponse = new GetCddoDataAssetResponse()
            {
                CddoDataAsset = new CddoDataAsset(),
            };

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/DataAsset/get-cddo-data-asset").WithQueryParam("dataAssetId", request)
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.GetDataAssetAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion

        #region GetDataAssetValidationErrorsAsync

        [Test]
        public async Task GetDataAssetValidationErrorsAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<Guid>();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetValidationErrorsAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetValidationErrorsAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<Guid>();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.GetDataAssetValidationErrorsAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDataAssetValidationErrorsAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<GetCddoDataAssetValidationErrorsResponse>();
            var request = testItems.Fixture.Create<Guid>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/validate-cddo-data-asset")
               .RespondWithJson(testResponse);
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            //Act
            var result = await testItems.CatalogService.GetDataAssetValidationErrorsAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion

        #region DeleteDataAssetAsync

        [Test]
        public async Task DeleteDataAssetAsync_WhenTokenIsNull_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "_");
            var request = testItems.Fixture.Create<DeleteProfiledDataAssetRequest>();
            //Act
            var result = await testItems.CatalogService.DeleteDataAssetAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task DeleteDataAssetAsync_WhenApiCallThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<DeleteProfiledDataAssetRequest>();

            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.DeleteDataAssetAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task DeleteDataAssetAsync_WhenApiCallThrowsFlurlHttpException_Forbidden_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<DeleteProfiledDataAssetRequest>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();
            httpTest.RespondWith("", (int)HttpStatusCode.Forbidden);


            //Act
            Func<Task> act = async () => await testItems.CatalogService.DeleteDataAssetAsync(request, CancellationToken.None);

            //Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Test]
        public async Task DeleteDataAssetAsync_WhenHttpContextThrowsFlurlHttpException_EmpTopicsList()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<DeleteProfiledDataAssetRequest>();

            using var httpTest = new HttpTest();
            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));
            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogService.DeleteDataAssetAsync(request, CancellationToken.None);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task DeleteDataAssetAsync_WhenTopicsArePresent_ListOfOrganisations()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<DeleteProfiledDataAssetResponse>();
            var request = testItems.Fixture.Create<DeleteProfiledDataAssetRequest>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/DataAsset/delete-profiled-data-asset")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogService.DeleteDataAssetAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion
    }
}
