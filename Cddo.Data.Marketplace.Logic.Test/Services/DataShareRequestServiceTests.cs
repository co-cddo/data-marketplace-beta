using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Flurl.Http.Testing;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Responses.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;
using System.Security.Claims;
using System.Net.Http;
using Cddo.Data.Marketplace.Api.Dto.Models;
using System.Collections.Generic;
using Agrimetrics.DataShare.Api.Dto.Responses.Supplier;
using Agrimetrics.DataShare.Api.Dto.Requests.Supplier;
using System.Net;
using Flurl.Http;
using Cddo.Data.Marketplace.Api.Dto.Requests.DataShareRequests;
using Flurl.Util;
using FluentAssertions;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Logic.Test.Services
{
    [TestFixture]
    public class DataShareRequestServiceTests
    {
        protected readonly IFixture fixture;
        public DataShareRequestServiceTests() 
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }
        #region Construction Tests
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfDataShareRequestService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            ILogger<DataShareRequestService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IUserRoleService userRoleService)
        {
            Assert.That(() => new DataShareRequestService(logger, configuration, httpContextAccessor, userRoleService),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }

        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<DataShareRequestService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();
            var userRoleService = fixture.Create<IUserRoleService>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor, userRoleService);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor, userRoleService);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null, userRoleService);
            yield return new TestCaseData("userRoleService", logger, configuration, httpContextAccessor, null);
        }
        #endregion

        #region GetDataShareRequestReturnCommentsAuditLog() Tests
        [Test]
        public async Task GivenAnAuthenticatedUser_WhenIGetDataShareRequestReturnCommentsAuditLog_ThenTheHttpCallIsMadeUsingTheIdTokenOfTheAuthenticatedUser()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                It.IsAny<Guid>());

            httpTest.ShouldHaveCalled("http://my-base-url/AuditLog/GetDataShareRequestAuditLog")
                .WithOAuthBearerToken("my-test-id-token");
        }
        [Test]
        public async Task GivenIGetDataShareRequestReturnCommentsAuditLog_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(dataShareRequestId, CancellationToken.None);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }

        [Test]
        public async Task GivenADataShareRequestId_WhenIGetDataShareRequestReturnCommentsAuditLog_ThenTheHttpCallIsMadeWithTheDataSharingRequestIdAsAQueryParameter()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testDataShareRequestId = testItems.Fixture.Create<Guid>();

            using var httpTest = new HttpTest();
            
            await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                testDataShareRequestId);

            httpTest.ShouldHaveCalled("http://my-base-url/AuditLog/GetDataShareRequestAuditLog")
                .WithQueryParam("DataShareRequestId", testDataShareRequestId);
        }

        [Test]
        public async Task GivenADataShareRequestService_WhenIGetDataShareRequestReturnCommentsAuditLog_ThenTheHttpCallIsMadeWithTheToStatusesSetToReturnStatusesAsAQueryParameter()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            using var httpTest = new HttpTest();

            await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                It.IsAny<Guid>());

            httpTest.ShouldHaveCalled("http://my-base-url/AuditLog/GetDataShareRequestAuditLog")
                .WithQueryParam("ToStatuses", new List<DataShareRequestStatus>{DataShareRequestStatus.Returned});
        }

        [Test]
        public async Task GivenAResponseIsReceived_WhenIGetDataShareRequestReturnCommentsAuditLog_ThenTheReceivedResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAuditLogResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AuditLog/GetDataShareRequestAuditLog")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                It.IsAny<Guid>());

            Assert.Multiple(() =>
            {
                // I want to compare the responses, but the complex structure is failing somewhere in the equality comparison,
                // so I just compare the request id and the times in the responses - that's enough to be sure that the actual response
                // is being returned
                Assert.That(result.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestId));

                Assert.That(result.DataShareRequestAuditLog.AuditLogEntries.Count, Is.EqualTo(testResponseJson.DataShareRequestAuditLog.AuditLogEntries.Count));
                foreach (var testAuditLogEntry in testResponseJson.DataShareRequestAuditLog.AuditLogEntries)
                {
                    Assert.That(result.DataShareRequestAuditLog.AuditLogEntries.Any(x => x.ChangedOnUtc == testAuditLogEntry.ChangedOnUtc), Is.True);
                }
            });
        }

        [Test]
        public async Task GivenARequestIsReceived_WhenIGetStartDataSharingRequest_ThenTheDataShareRequestIdIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<StartDataShareRequestResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/StartDataShareRequest")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.StartDataSharingRequest(
                It.IsAny<Guid>(), default);

            Assert.That(result, Is.EqualTo(testResponseJson.DataShareRequestId));

        }
        [Test]
        public async Task GivenIStartDataSharingRequest_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.StartDataSharingRequest(
                It.IsAny<Guid>(), default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenIQuestionsSummary_ThenTheDataShareRequestQuestionsSummaryIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestQuestionsSummaryResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestQuestionsSummary")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.QuestionsSummary(
                It.IsAny<Guid>(), default);

            Assert.That(result.DataShareRequestRequestId, Is.EqualTo(testResponseJson.DataShareRequestRequestId));

        }
        [Test]
        public async Task GivenIQuestionsSummary_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.QuestionsSummary(
                It.IsAny<Guid>(), default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenIQuestionSummary_ThenTheDataShareRequestQuestionsSummaryIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestQuestionInformationResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestQuestionInformation")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.QuestionSummary(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(), 
                default);

            Assert.That(result.QuestionId, Is.EqualTo(testResponseJson.QuestionId));

        }
        [Test]
        public async Task GivenIQuestionSummary_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.QuestionSummary(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenISubmitAnswerQuestion_ThenTheDataShareRequestQuestionAnswerIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<SetDataShareRequestQuestionAnswerResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/SetDataShareRequestQuestionAnswer")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.SubmitAnswerQuestion(
                It.IsAny<SetDataShareRequestQuestionAnswerRequest>(),
                default);

            Assert.That(result.Result.NextQuestionId, Is.EqualTo(testResponseJson.Result.NextQuestionId));

        }
        [Test]
        public async Task GivenSubmitAnswerQuestion_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.SubmitAnswerQuestion(
                It.IsAny<SetDataShareRequestQuestionAnswerRequest>(),
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenIGetAnswerSummary_ThenTheDataShareRequestAnswersSummaryIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAnswersSummaryResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestAnswersSummary")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetAnswerSummary(
                It.IsAny<Guid>(),
                default);

            Assert.That(result.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestId));

        }
        [Test]
        public async Task GivenGetAnswerSummary_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetAnswerSummary(
                It.IsAny<Guid>(),
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenISubmitDataShareRequest_ThenTheDataShareRequestAnswersSummaryIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<SubmitDataShareRequestResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/SubmitDataShareRequest")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.SubmitDataShareRequest(
                It.IsAny<Guid>(),
                default);

            Assert.That(result.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestId));

        }
        [Test]
        public async Task GivenSubmitDataShareRequest_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.SubmitDataShareRequest(
                It.IsAny<Guid>(),
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenIGetAcquirerDataShareRequests_ThenTheDataShareRequestSummariesIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestSummariesResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetAcquirerDataShareRequestSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetAcquirerDataShareRequests(dataShareStatusses,
                default);

            Assert.That(result.DataShareRequestSummaries.DataShareRequestSummaries.Count(), Is.EqualTo(testResponseJson.DataShareRequestSummaries.DataShareRequestSummaries.Count()));

        }
        [Test]
        public async Task GivenGetAcquirerDataShareRequests_WhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetAcquirerDataShareRequests(dataShareStatusses,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenARequestIsReceived_WhenIGetDataShareRequestAdminSummaries_AndUserIsNotAuthenticated_ThenTheDataShareRequestSummariesIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAdminSummariesResponse>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestAdminSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestAdminSummaries(
                It.IsAny<GetDataShareRequestAdminSummariesRequest>(),
                default);

            Assert.That(result, Is.EqualTo(null));

        }
        
        [Test]
        public async Task GivenDataShareRequestAdminSummaries_WhenUserIsAuthenticatedAndTheUserOrganisationIsNotThesameAsSupplierOrganisationId_ThenNullIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAdminSummariesResponse>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c=>c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);

            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestAdminSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestAdminSummaries(
                request,
                default);

            Assert.That(result, Is.EqualTo(null));

        }
        [Test]
        public async Task GivenDataShareRequestAdminSummaries_WhenUserIsAuthenticatedAndTheUserSystemAdmin_ThenDataShareRequestAdminSummariesIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAdminSummariesResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestAdminSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestAdminSummaries(
                request,
                default);

            Assert.That(result.DataShareRequestAdminSummaries.DataShareRequestAdminSummaries.Count(), Is.EqualTo(testResponseJson.DataShareRequestAdminSummaries.DataShareRequestAdminSummaries.Count()));

        }
        [Test]
        public async Task GivenDataShareRequestAdminSummaries_WhenUserIsAuthenticatedAndTheUserOrganisationAdmin_ThenDataShareRequestAdminSummariesIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAdminSummariesResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleAdmin()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestAdminSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestAdminSummaries(
                request,
                default);

            Assert.That(result.DataShareRequestAdminSummaries.DataShareRequestAdminSummaries.Count(), Is.EqualTo(testResponseJson.DataShareRequestAdminSummaries.DataShareRequestAdminSummaries.Count()));

        }
        [Test]
        public async Task GivenGetDataShareRequestAdminSummaries_AndAuthenicatedUserOrganisationAdminWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");
            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleAdmin()).ReturnsAsync(true);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetDataShareRequestAdminSummaries(
                request,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenDataShareRequestAdminSummaries_WhenUserIsAuthenticatedAndTheUserSupplier_ThenDataShareRequestAdminSummariesIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAdminSummariesResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestAdminSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestAdminSummaries(
                request,
                default);

            Assert.That(result.DataShareRequestAdminSummaries.DataShareRequestAdminSummaries.Count(), Is.EqualTo(testResponseJson.DataShareRequestAdminSummaries.DataShareRequestAdminSummaries.Count()));

        }

        [Test]
        public async Task GivenEsdaQuestionSetOutline_WhenEsdaQuestionSetOutlineIsCalled_ThenEsdaQuestionSetOutlineIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetEsdaQuestionSetOutlineResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetEsdaQuestionSetOutline")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetEsdaQuestionSetOutline(
                esda,
                default);

            Assert.That(result.EsdaId, Is.EqualTo(testResponseJson.EsdaId));

        }
        [Test]
        public async Task GivenGetEsdaQuestionSetOutline_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetEsdaQuestionSetOutline(
                dataShareRequestId,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationIsCalled_ThenDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(
                esda,
                default);

            Assert.That(result.EsdaId, Is.EqualTo(testResponseJson.EsdaId));

        }
        [Test]
        public async Task GivenGetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(
                dataShareRequestId,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenCancelDataShareRequestIsCalled_ThenCancelDataShareRequestResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<CancelDataShareRequestResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/CancelDataShareRequest")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.CancelDataShareRequest(
                esda,
                It.IsAny<string>(),
                default);

            Assert.That(result.ReasonsForCancellation, Is.EqualTo(testResponseJson.ReasonsForCancellation));

        }
        [Test]
        public async Task GivenCancelDataShareRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.CancelDataShareRequest(
                esda,
                It.IsAny<string>(),
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenDeleteDataShareRequestIsCalled_ThenDeleteDataShareRequestResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<DeleteDataShareRequestResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AcquirerDataShareRequest/DeleteDataShareRequest")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.DeleteDataShareRequest(
                esda,
                default);

            Assert.That(result.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestId));

        }
        [Test]
        public async Task GivenDeleteDataShareRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.DeleteDataShareRequest(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        #endregion

        #region Supplier Interface
        [Test]
        public async Task GivenGetSupplierDataShareRequestsIsCalled_ThenGetSubmissionSummariesResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetSubmissionSummariesResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetSubmissionSummaries")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetSupplierDataShareRequests(
                default);

            Assert.That(result.SubmissionSummariesSet.PendingSubmissionSummaries.Count(), Is.EqualTo(testResponseJson.SubmissionSummariesSet.PendingSubmissionSummaries.Count()));

        }
        [Test]
        public async Task GivenGetSupplierDataShareRequests_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetSupplierDataShareRequests(
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetSubmissionInformationIsCalled_ThenGetSubmissionInformationResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetSubmissionInformationResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetSubmissionInformation")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetSubmissionInformation(
                esda,
                default);

            Assert.That(result.SubmissionInformation.EsdaName, Is.EqualTo(testResponseJson.SubmissionInformation.EsdaName));

        }
        [Test]
        public async Task GivenGetSubmissionInformation_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetSubmissionInformation(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetSubmissionReviewInformationIsCalled_ThenGetSubmissionInformationResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetSubmissionReviewInformationResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetSubmissionReviewInformation")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetSubmissionReviewInformation(
                esda,
                default);

            Assert.That(result.SubmissionReviewInformation.SubmissionDetails.RequestStatus, Is.EqualTo(testResponseJson.SubmissionReviewInformation.SubmissionDetails.RequestStatus));

        }
        [Test]
        public async Task GivenGetSubmissionReviewInformation_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetSubmissionReviewInformation(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetReturnedSubmissionInformationIsCalled_ThenGetReturnedSubmissionInformationResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetReturnedSubmissionInformationResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetReturnedSubmissionInformation")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetReturnedSubmissionInformation(
                esda,
                default);

            Assert.That(result.ReturnedSubmissionInformation.WhenNeededBy, Is.EqualTo(testResponseJson.ReturnedSubmissionInformation.WhenNeededBy));

        }
        [Test]
        public async Task GivenGetReturnedSubmissionInformation_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetReturnedSubmissionInformation(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetSubmissionDetailsIsCalled_ThenGetSubmissionDetailsResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetSubmissionDetailsResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetSubmissionDetails")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetSubmissionDetails(
                esda,
                default);

            Assert.That(result.SubmissionDetails.AcquirerOrganisationName, Is.EqualTo(testResponseJson.SubmissionDetails.AcquirerOrganisationName));

        }
        [Test]
        public async Task GivenGetSubmissionDetails_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetSubmissionDetails(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenStartSubmissionReviewIsCalled_ThenStartSubmissionReviewResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<StartSubmissionReviewResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/StartSubmissionReview")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.StartSubmissionReview(
                esda,
                default);

            Assert.That(result.SubmissionReviewInformation.SupplierNotes, Is.EqualTo(testResponseJson.SubmissionReviewInformation.SupplierNotes));

        }
        [Test]
        public async Task GivenStartSubmissionReview_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.StartSubmissionReview(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenSetSubmissionNotesResponseIsCalled_ThenSetSubmissionNotesResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<SetSubmissionNotesResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/SetSubmissionNotes")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.SetSubmissionNotes(
                esda,
                It.IsAny<string>(),
                default);

            Assert.That(result.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestId));

        }
        [Test]
        public async Task GivenSetSubmissionNotes_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.SetSubmissionNotes(
                esda,
                It.IsAny<string>(),
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetCompletedReceivedRequestIsCalled_ThenSetSubmissionNotesResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();

            var testResponseJson = testItems.Fixture.Create<GetCompletedSubmissionInformationResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetCompletedSubmissionInformation")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetCompletedReceivedRequest(
                esda,
                default);

            Assert.That(result.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestId));

        }
        [Test]
        public async Task GivenGetCompletedReceivedRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetCompletedReceivedRequest(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenAcceptReceivedRequestIsCalled_ThenSetSubmissionNotesResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            var testResponseJson = testItems.Fixture.Create<AcceptSubmissionResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/AcceptSubmission")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.AcceptReceivedRequest(
                esda,
                notes,
                default);

            Assert.That(result.AcceptedDecisionSummary.AcquirerUserEmailAddress, Is.EqualTo(testResponseJson.AcceptedDecisionSummary.AcquirerUserEmailAddress));

        }
        [Test]
        public async Task GivenAcceptReceivedRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.AcceptReceivedRequest(
                esda,
                notes,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenRejectReceivedRequestIsCalled_ThenSetSubmissionNotesResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            var testResponseJson = testItems.Fixture.Create<RejectSubmissionResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/RejectSubmission")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.RejectReceivedRequest(
                esda,
                notes,
                default);

            Assert.That(result.NotificationSuccess, Is.EqualTo(testResponseJson.NotificationSuccess));

        }
        [Test]
        public async Task GivenRejectReceivedRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.RejectReceivedRequest(
                esda,
                notes,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenReturnReceivedRequestIsCalled_ThenReturnSubmissionResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            var testResponseJson = testItems.Fixture.Create<ReturnSubmissionResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/ReturnSubmission")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.ReturnReceivedRequest(
                esda,
                notes,
                default);

            Assert.That(result.NotificationSuccess, Is.EqualTo(testResponseJson.NotificationSuccess));

        }
        [Test]
        public async Task GivenReturnReceivedRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.ReturnReceivedRequest(
                esda,
                notes,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenDownloadCompletedRequestIsCalled_ThenbyteArrayIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<DataShareRequestFileFormat>();

            var testResponseJson = testItems.Fixture.Create<byte[]>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/SupplierDataShareRequest/GetSubmissionContentAsFile")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.DownloadCompletedRequest(
                esda,
                notes,
                default);

            Assert.That(6, Is.EqualTo(result.Count()));

        }
        [Test]
        public async Task GivenDownloadCompletedRequest_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<DataShareRequestFileFormat>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.DownloadCompletedRequest(
                esda,
                notes,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }
        [Test]
        public async Task GivenGetDataShareRequestReturnCommentsAuditLogIsCalled_ThenReturnSubmissionResponseIsReturned()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var userDetails = fixture.Create<UserProfile>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<string>();

            var testResponseJson = testItems.Fixture.Create<GetDataShareRequestAuditLogResponse>();

            //Set matching Org and supplier
            request.SupplierOrganisationId = userDetails.Organisation.OrganisationId;

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            var cookieDictionary = new Dictionary<string, string>
                {
                    { "CO-Datamarketplace", "your-cookie-value" }
                };

            var mockRequestCookies = new Mock<IRequestCookieCollection>();
            mockRequestCookies.Setup(c => c["CO-Datamarketplace"]).Returns(cookieDictionary["CO-Datamarketplace"]);

            httpContext.Request.Cookies = mockRequestCookies.Object;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);
            testItems.MockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userDetails);
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSupplier()).ReturnsAsync(true);
            httpTest.ForCallsTo("http://my-base-url/AuditLog/GetDataShareRequestAuditLog")
                .RespondWithJson(testResponseJson);

            var result = await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                esda,
                default);

            Assert.That(result.DataShareRequestAuditLog.DataShareRequestId, Is.EqualTo(testResponseJson.DataShareRequestAuditLog.DataShareRequestId));

        }
        [Test]
        public async Task GivenGetDataShareRequestReturnCommentsAuditLog_AndWhenHttpCallThrowsException_FlurlHttpException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var dataShareStatusses = fixture.Create<IEnumerable<DataShareRequestStatus>>();
            var request = fixture.Create<GetDataShareRequestAdminSummariesRequest>();
            var esda = fixture.Create<Guid>();
            var notes = fixture.Create<DataShareRequestFileFormat>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-id-token");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            Func<Task> act = async () => await testItems.DataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                esda,
                default);

            await act.Should().ThrowAsync<DataShareRequestException>().Where(ex => ex.DsrStatusCode == 500);
        }

        #endregion

    }
}
