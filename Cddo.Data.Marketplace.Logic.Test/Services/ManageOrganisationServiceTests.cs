using AutoFixture.AutoMoq;
using AutoFixture;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using System.Net;
using Flurl.Http.Testing;
using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;

namespace Cddo.Data.Marketplace.Logic.Test.Services
{
    public class ManageOrganisationServiceTests
    {

        protected readonly IFixture fixture;
        private HttpClient _httpClient;
        private string _usersApi = "https://fakeapi.com/";
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public ManageOrganisationServiceTests()
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
        public async Task GetManageOrganisationsAsync_WhenUserIsNotAuthenticated_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageOrganisationsRequest>(); 

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            var result = await testItems.ManageOrganisationService.GetManageOrganisationsAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetManageOrganisationsAsync_WhenUserIsNotAgmAdmin_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageOrganisationsRequest>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");

            var result = await testItems.ManageOrganisationService.GetManageOrganisationsAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetManageOrganisationsAsync_WhenUserIsAgmAdmin_ReturnsManageOrganisationsResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageOrganisationsRequest>();
            var testResponse = fixture.Create<ManageOrganisationsResponse>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.ForCallsTo("http://xyz/Organisations/organisationsByPage")
                .RespondWithJson(testResponse);

            var result = await testItems.ManageOrganisationService.GetManageOrganisationsAsync(manageOrganisationRequest, default);

            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task GetManageOrganisationsAsync_WhenApiCallThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageOrganisationsRequest>();
            var testResponse = fixture.Create<ManageOrganisationsResponse>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.ManageOrganisationService.GetManageOrganisationsAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }
    }
}
