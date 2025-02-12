using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.Api.Dto.Requests;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using System.Security.Claims;
using Flurl.Http.Testing;
using FluentAssertions;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using System.Net;

namespace Cddo.Data.Marketplace.Logic.Test
{
    public class ManageOrganisationsServiceTests
    {
        protected readonly IFixture fixture;
        private HttpClient _httpClient;
        private string _usersApi = "https://fakeapi.com/";
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public ManageOrganisationsServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(_usersApi),
                Timeout = TimeSpan.FromSeconds(50)
            };
        }

        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfManageOrganisationsService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            IAppInsightsLogger logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            Assert.That(() => new ManageOrganisationsService(logger, configuration, httpContextAccessor),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }

        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<IAppInsightsLogger>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null);
        }

        [Test]
        public async Task GetOrganisationAsync_WhenUserIsAgmAdmin_ReturnsManageOrganisationsResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var organisationId = fixture.Create<int>();
            var testResponse = fixture.Create<OrganisationDetail>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.ForCallsTo($"http://xyz/Organisations/{organisationId}")
                .RespondWithJson(testResponse);

            var result = await testItems.ManageOrganisationsService.GetOrganisationAsync(organisationId, default);

            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task GetOrganisationAsync_WhenApiCallThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<int>();
            var testResponse = fixture.Create<Department>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.ManageOrganisationsService.GetOrganisationAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationAsync_WhenHttpContextThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<int>();
            var testResponse = fixture.Create<Department>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.ManageOrganisationsService.GetOrganisationAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task UpdateDataShareRequestMailboxAddress_WhenUserIsAgmAdmin_ReturnsManageOrganisationsResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var domainId = fixture.Create<int>();
            var dataShareRequestMailboxAddress = fixture.Create<string>();
            var testResponse = fixture.Create<OrganisationDetail>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            httpContext.Request.Headers["Cookie"] = $"CO-Datamarketplace=my-test-id-token; AnotherTestCookie=AnotherTestValue";

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            httpTest.ForCallsTo($"http://xyz/Organisations/domains/{domainId}/dataShareRequestMailboxAddress")
                .RespondWithJson(testResponse);

            await testItems.ManageOrganisationsService.UpdateDataShareRequestMailboxAddress(domainId, dataShareRequestMailboxAddress, default);

            httpTest.ShouldHaveCalled($"http://xyz/Organisations/domains/{domainId}/dataShareRequestMailboxAddress")
                .WithOAuthBearerToken("my-test-id-token");

        }

        [Test]
        public async Task UpdateDataShareRequestMailboxAddress_WhenApiCallThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var domainId = fixture.Create<int>();
            var dataShareRequestMailboxAddress = fixture.Create<string>();
            var testResponse = fixture.Create<Department>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            await testItems.ManageOrganisationsService.UpdateDataShareRequestMailboxAddress(domainId, dataShareRequestMailboxAddress, default);

            httpTest.ShouldHaveCalled($"http://xyz/Organisations/domains/{domainId}/dataShareRequestMailboxAddress");
        }

        [Test]
        public async Task UpdateDataShareRequestMailboxAddress_WhenHttpContextThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var domainId = fixture.Create<int>();
            var testResponse = fixture.Create<Department>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            await testItems.ManageOrganisationsService.UpdateDataShareRequestMailboxAddress(domainId, default);

            httpTest.ShouldNotHaveCalled($"http://xyz/Organisations/domains/{domainId}/dataShareRequestMailboxAddress");
        }
    }
}
