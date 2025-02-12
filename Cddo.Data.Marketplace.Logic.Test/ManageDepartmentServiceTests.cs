using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using FluentAssertions;
using Flurl.Http.Testing;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.Extensions.Logging;
using Cddo.Data.Marketplace.Audit;
using Microsoft.Extensions.Configuration;
using Cddo.Data.Marketplace.Api.Dto.Requests;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;

namespace Cddo.Data.Marketplace.Logic.Test
{
    public class ManageDepartmentServiceTests
    {
        protected readonly IFixture fixture;
        private HttpClient _httpClient;
        private string _usersApi = "https://fakeapi.com/";
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public ManageDepartmentServiceTests()
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
        public void GivenANullParameter_WhenIConstructAnInstanceOfManageDepartmentsService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            IAppInsightsLogger logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IUserRoleService userRoleService)
        {
            Assert.That(() => new ManageDepartmentsService(logger, configuration, httpContextAccessor, userRoleService),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }

        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<IAppInsightsLogger>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();
            var userRoleService = fixture.Create<IUserRoleService>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor, userRoleService);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor, userRoleService);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null, userRoleService);
            yield return new TestCaseData("userRoleService", logger, configuration, httpContextAccessor, null);
        }

        [Test]
        public async Task GetManageDepartmentsAsync_WhenUserIsNotAuthenticated_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageDepartmentRequest>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            var result = await testItems.ManageDepartmentService.GetManageDepartmentsAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetManageDepartmentsAsync_WhenUserIsNotAgmAdmin_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageDepartmentRequest>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");

            var result = await testItems.ManageDepartmentService.GetManageDepartmentsAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetManageDepartmentsAsync_WhenUserIsAgmAdmin_ReturnsManageOrganisationsResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageDepartmentRequest>();
            var testResponse = fixture.Create<ManageDepartmentsResponse>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.ForCallsTo("http://xyz/Department/departments-paged")
                .RespondWithJson(testResponse);

            var result = await testItems.ManageDepartmentService.GetManageDepartmentsAsync(manageOrganisationRequest, default);

            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task GetManageDepartmentsAsync_WhenApiCallThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<ManageDepartmentRequest>();
            var testResponse = fixture.Create<ManageDepartmentsResponse>();

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

            var result = await testItems.ManageDepartmentService.GetManageDepartmentsAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetDepartmentByIdAsync_WhenUserIsNotAuthenticated_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<int>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            var result = await testItems.ManageDepartmentService.GetDepartmentByIdAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetDepartmentByIdAsync_WhenUserIsNotAgmAdmin_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<int>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");

            var result = await testItems.ManageDepartmentService.GetDepartmentByIdAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task GetDepartmentByIdAsync_WhenUserIsAgmAdmin_ReturnsManageOrganisationsResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var departmentId = 1;
            var testResponse = fixture.Create<Department>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.ForCallsTo("http://xyz/Department/department/1")
                .RespondWithJson(testResponse);

            var result = await testItems.ManageDepartmentService.GetDepartmentByIdAsync(departmentId, default);

            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task GetDepartmentByIdAsync_WhenApiCallThrowsException_ReturnsNull()
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

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.ManageDepartmentService.GetDepartmentByIdAsync(manageOrganisationRequest, default);

            result.Should().BeNull();
        }

        [Test]
        public async Task PostAddDepartmentAsync_WhenUserIsNotAuthenticated_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<string>();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            var result = await testItems.ManageDepartmentService.PostAddDepartmentAsync(manageOrganisationRequest, default);

            result.Should().Be(false);
        }

        [Test]
        public async Task PostAddDepartmentAsync_WhenUserIsNotAgmAdmin_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<string>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");

            var result = await testItems.ManageDepartmentService.PostAddDepartmentAsync(manageOrganisationRequest, default);

            result.Should().Be(false);
        }

        [Test]
        public async Task PostAddDepartmentAsync_WhenUserIsAgmAdmin_ReturnsManageOrganisationsResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<string>();
            var testResponse = fixture.Create<Department>();

            using var httpTest = new HttpTest();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            httpContext.User = principal;

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Returns(httpContext);

            //HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "my-test-token");
            testItems.MockUserRoleService.Setup(u => u.IsUserRoleSystemAdmin()).ReturnsAsync(true);

            httpTest.ForCallsTo("http://xyz/Department/create")
                .RespondWithJson(testResponse);

            var result = await testItems.ManageDepartmentService.PostAddDepartmentAsync(manageOrganisationRequest, default);

            result.Should().Be(true);
        }

        [Test]
        public async Task PostAddDepartmentAsync_WhenApiCallThrowsException_ReturnsNull()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var manageOrganisationRequest = fixture.Create<string>();
            var testResponse = fixture.Create<Department>();

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

            var result = await testItems.ManageDepartmentService.PostAddDepartmentAsync(manageOrganisationRequest, default);

            result.Should().Be(false);
        }
    }
}
