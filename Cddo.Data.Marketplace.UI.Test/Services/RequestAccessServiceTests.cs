using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Cddo.Data.Marketplace.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Cddo.Data.Marketplace.Api.Dto.Requests.RequestAccess;
using FluentAssertions;
using System.Net;
using Flurl.Http.Testing;
using Cddo.Data.Marketplace.Api.Dto.Responses.RequestAccess;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class RequestAccessServiceTests
    {
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfCatalogQuestionsService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            ILogger<RequestAccessService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            Assert.That(() => new RequestAccessService(logger, configuration, httpContextAccessor),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }
        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<RequestAccessService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null);
        }

        #region SubmitOrganisationRequestAsync
        [Test]
        public async Task SubmitOrganisationRequestAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<CreateOrganisationRequest>();

            //Act
            var result = await testItems.RequestAccessService.SubmitOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task SubmitOrganisationRequestAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<CreateOrganisationRequest>();
            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.RequestAccessService.SubmitOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task SubmitOrganisationRequestAsync_WhenHttpThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<CreateOrganisationRequest>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.RequestAccessService.SubmitOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task SubmitOrganisationRequestAsync_WhenApiCallIsSuccessful_OrganisationSubmitted()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<CreateOrganisationRequest>();
            var testResponse = testItems.Fixture.Create<int>();

            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.RequestAccessService.SubmitOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(testResponse);
        }
        #endregion

        #region UpdateOrganisationRequestAsync
        [Test]
        public async Task UpdateOrganisationRequestAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<OrganisationAccessResponse>();

            //Act
            var result = await testItems.RequestAccessService.UpdateOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task UpdateOrganisationRequestAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<OrganisationAccessResponse>();
            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/{request.OrganisationRequestID}")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.RequestAccessService.UpdateOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task UpdateOrganisationRequestAsync_WhenHttpThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<OrganisationAccessResponse>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.RequestAccessService.UpdateOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task UpdateOrganisationRequestAsync_WhenApiCallIsSuccessful_OrganisationCreated()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<OrganisationAccessResponse>();
            var testResponse = testItems.Fixture.Create<int>();

            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/{request.OrganisationRequestID}")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.RequestAccessService.UpdateOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(testResponse);
        }

        [Test]
        public async Task UpdateOrganisationRequestAsync_WhenStatusIsApprovedApiCallIsSuccessful_OrganisationRequestUpdated_OrganisationCreated()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<OrganisationAccessResponse>();
            var testResponse = testItems.Fixture.Create<int>();

            request.Status = "Approved";


            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/{request.OrganisationRequestID}")
           .RespondWithJson(testResponse);

            httpTest.ForCallsTo($"http://xyz/Organisations")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.RequestAccessService.UpdateOrganisationRequestAsync(request);

            //Assert
            result.Should().Be(testResponse);
        }

        [Test]
        public async Task UpdateOrganisationRequestAsync_WhenStatusIsApprovedApiCallIsFails_OrganisationNotRequestUpdated_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<OrganisationAccessResponse>();
            var testResponse = testItems.Fixture.Create<int>();

            request.Status = "Approved";


            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations")
           .RespondWithJson(null);

            //Act
            var result = await testItems.RequestAccessService.UpdateOrganisationRequestAsync(request);

            //Assert
            result.Should().BeNull();
        }
        #endregion

        #region GetOrganisationAllRequestAsync
        [Test]
        public async Task GetOrganisationAllRequestAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationAllRequestAsync();

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationAllRequestAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<CreateOrganisationRequest>();
            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/All")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationAllRequestAsync();

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationAllRequestAsync_WhenHttpThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<CreateOrganisationRequest>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationAllRequestAsync();

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationAllRequestAsync_WhenApiCallIsSuccessful_OrganisationSubmitted()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<List<OrganisationAccessResponse>>();
            var testResponse = testItems.Fixture.Create<List<OrganisationAccessResponse>>();

            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/All")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationAllRequestAsync();

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }
        #endregion

        #region GetOrganisationRequestByIdAsync
        [Test]
        public async Task GetOrganisationRequestByIdAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var organisationId = testItems.Fixture.Create<int>();
            //Act
            var result = await testItems.RequestAccessService.GetOrganisationRequestByIdAsync(organisationId);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationRequestByIdAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<int>();
            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/{request}")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationRequestByIdAsync(request);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationRequestByIdAsync_WhenHttpThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<int>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationRequestByIdAsync(request);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationRequestByIdAsync_WhenApiCallIsSuccessful_OrganisationSubmitted()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-owntest-token");
            var request = testItems.Fixture.Create<int>();
            var testResponse = testItems.Fixture.Create<OrganisationAccessResponse>();

            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Organisations/Request/{request}")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.RequestAccessService.GetOrganisationRequestByIdAsync(request);

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }
        #endregion
    }
}
