using AutoFixture.AutoMoq;
using AutoFixture;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Cddo.Data.Marketplace.Audit;
using Flurl.Http.Testing;
using Moq;
using Cddo.Data.Marketplace.Logic.Exceptions;
using FluentAssertions;
using System.Net;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using Moq.Protected;
using System.Text.Json;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Cddo.Data.Marketplace.Logic.Test;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using System.Net.Http.Json;

namespace Cddo.Data.Marketplace.Logic.Test.Services.Users
{
    [TestFixture]
    public class UserRoleServiceTests
    {
        protected readonly IFixture fixture;
        private HttpClient _httpClient;
        private string _usersApi = "https://fakeapi.com/";
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public UserRoleServiceTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(_usersApi),
                Timeout = TimeSpan.FromSeconds(50)
            };

        }

        #region Construction Tests
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfUserRoleService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAppInsightsLogger logger,
            IHttpContextAccessor httpContextAccessor)
        {
            Assert.That(() => new UserRoleService(httpClientFactory, configuration, logger, httpContextAccessor),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }

        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<IAppInsightsLogger>();
            var configuration = fixture.Create<IConfiguration>();
            var httpClientFactory = fixture.Create<IHttpClientFactory>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();

            yield return new TestCaseData("clientFactory", null, configuration, logger, httpContextAccessor);
            yield return new TestCaseData("configuration", httpClientFactory, null, logger, httpContextAccessor);
            yield return new TestCaseData("logger", httpClientFactory, configuration, null, httpContextAccessor);
            yield return new TestCaseData("httpContextAccessor", httpClientFactory, configuration, logger, null);
        }
        #endregion

        [Test]
        public async Task GivenGetUserProfileAsync_ISCalledWithNoTokenInTheCookie_ReturnsEmptyProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems();
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.UserRoleService.GetUserProfileAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }
        [Test]
        public async Task GivenGetUserProfileAsync_ISCalledWithNoEmailClaimInTheToken_ReturnsEmptyProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt(null, null));

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.UserRoleService.GetUserProfileAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }
        [Test]
        public async Task GivenGetUserProfileAsync_ISCalledWithNoNameClaimInTheToken_ReturnsEmptyProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", null));

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.UserRoleService.GetUserProfileAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }

        [Test]
        public async Task GetUserProfileAsync_ShouldReturnUserProfile_WhenApiReturnsValidResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.GetUserProfileAsync();

            result.Should().NotBeNull();
            result.User.UserName.Should().Be("tester");
        }

        [Test]
        public async Task GetUserProfileAsync_ShouldThrowHttpRequestException_WhenApiFails()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            // Act
            var result = await testItems.UserRoleService.GetUserProfileAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }

        [Test]
        public async Task GetUserProfileAsync_WhenTimeoutExceptionShouldThroTimeoutException_EmptyUserProfile()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            // Act
            var result = await testItems.UserRoleService.GetUserProfileAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }

        [Test]
        public async Task GivenGetUserByIdAsync_ISCalledWithNoTokenInTheCookie_ReturnsEmptyProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.UserRoleService.GetUserByIdAsync(userId.ToString());

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }

        [Test]
        public async Task GetUserByIdAsync_ShouldReturnUserProfile_WhenApiReturnsValidResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userId = Guid.NewGuid();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.GetUserByIdAsync(userId.ToString());

            result.Should().NotBeNull();
            result.User.UserName.Should().Be("tester");
        }

        [Test]
        public async Task GetUserByIdAsync_ShouldThrowHttpRequestException_WhenApiFails()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userId = fixture.Create<Guid>();

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            // Act
            var result = await testItems.UserRoleService.GetUserByIdAsync(userId.ToString());

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new UserProfile());
        }

        [Test]
        public async Task GivenGetAllRolesAsync_ISCalledWithNoTokenInTheCookie_ReturnsEmptyProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userId = Guid.NewGuid();

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            var result = await testItems.UserRoleService.GetAllRolesAsync();

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new List<Role>());
        }

        [Test]
        public async Task GetAllRolesAsync_ShouldThrowHttpRequestException_WhenApiFails()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userId = fixture.Create<Guid>();

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            // Act
            var result = await testItems.UserRoleService.GetAllRolesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(new List<Role>());
        }

        [Test]
        public async Task GetAllRolesAsync_ShouldReturnUserProfile_WhenApiReturnsValidResponse()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userId = Guid.NewGuid();
            var expectedProfile = fixture.Create<List<Role>>();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.GetAllRolesAsync();

            result.Should().NotBeNull();
            result.Count.Should().Be(expectedProfile.Count());
        }

        [Test]
        public async Task GivenIsUserDomainEnabledAsync_ISCalledWhenProfileWithNoDomain_ResultsInFalse()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var dataShareRequestId = Guid.NewGuid();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            using var httpTest = new HttpTest();

            httpTest.RespondWith("", (int)HttpStatusCode.InternalServerError);

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.IsUserDomainEnabledAsync();

            result.Should().Be(false);
        }

        [Test]
        public async Task GivenIsUserDomainEnabledAsync_WhenUserIsInDomain_ResultsInTrue()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };
            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.IsUserDomainEnabledAsync();

            result.Should().Be(true);
        }

        [Test]
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, true)]
        public async Task GivenIsIsUserInRoleAsync_TestDiffereScenarion_ExpectedResult(bool hasUser, bool domainEnabled, bool organisationEnabled, bool expectedResult)
        {
            // Arrange
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userRoles = new List<Role> { new Role() { RoleName = "testRole" } };
            var userRolesExpected = new List<string> { "testRole" };

            var expectedProfile = new UserProfile
            {
                User = hasUser ? new UserInfo() { UserName = "tester" } : null,
                Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = domainEnabled },
                Organisation = new UserOrganisation() { IsEnabled = organisationEnabled },
                Roles = userRoles
            };

            var jwtToken = GenerateJwt("test@email.com", "tester");

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, jwtToken);

            // Mock the HttpClientFactory to return a client using our mock handler
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };

            testItems.MockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient); // Use the properly mocked client

            // Mock SendAsync to return the fake response
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await testItems.UserRoleService.IsUserInRoleAsync(userRolesExpected);

            // Assert
            result.Should().Be(expectedResult);
        }


        [Test]
        public async Task GivenAddUserToRoleAsync_WhenIdTokenIsNull_ThrowException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            string roleId = fixture.Create<string>();
            string userId = fixture.Create<string>();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            Func<Task> act = async () => await testItems.UserRoleService.AddUserToRoleAsync(roleId, userId);

            await act.Should()
           .ThrowAsync<ArgumentException>()
           .WithMessage("GetUserProfileAsync: ID token is not available.");
        }

        [Test]
        public async Task GivenAddUserToRoleAsync_WhenApiCallFails_ThrowException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            string roleId = fixture.Create<string>();
            string userId = fixture.Create<string>();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            Func<Task> act = async () => await testItems.UserRoleService.AddUserToRoleAsync(roleId, userId);

            await act.Should()
           .ThrowAsync<HttpRequestException>()
           .WithMessage("Failed to add user to role, HTTP BadRequest");
        }

        [Test]
        public async Task GivenAddUserToRoleAsync_WhenAllWorks_ReturnsUserProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            string roleId = fixture.Create<string>();
            string userId = fixture.Create<string>();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.AddUserToRoleAsync(roleId, userId);

            result.Should().BeEquivalentTo(expectedProfile);
        }


        [Test]
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, true)]
        public async Task GivenIsIsUserRoleAdmin_TestDiffereScenarion_ExpectedResult(bool hasUser, bool domainEnabled, bool organisationEnabled, bool expectedResult)
        {
            // Arrange
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userRoles = new List<Role> { new Role() { RoleName = "Organisation Administrator" } };

            var expectedProfile = new UserProfile
            {
                User = hasUser ? new UserInfo() { UserName = "tester" } : null,
                Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = domainEnabled },
                Organisation = new UserOrganisation() { IsEnabled = organisationEnabled },
                Roles = userRoles
            };

            var jwtToken = GenerateJwt("test@email.com", "tester");

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, jwtToken);

            // Mock the HttpClientFactory to return a client using our mock handler
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };

            testItems.MockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient); // Use the properly mocked client

            // Mock SendAsync to return the fake response
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await testItems.UserRoleService.IsUserRoleAdmin();

            // Assert
            result.Should().Be(expectedResult);
        }

        [Test]
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, true)]
        public async Task GivenIsUserRoleSystemAdmin_TestDiffereScenarion_ExpectedResult(bool hasUser, bool domainEnabled, bool organisationEnabled, bool expectedResult)
        {
            // Arrange
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userRoles = new List<Role> { new Role() { RoleName = "System Administrator" } };

            var expectedProfile = new UserProfile
            {
                User = hasUser ? new UserInfo() { UserName = "tester" } : null,
                Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = domainEnabled },
                Organisation = new UserOrganisation() { IsEnabled = organisationEnabled },
                Roles = userRoles
            };

            var jwtToken = GenerateJwt("test@email.com", "tester");

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, jwtToken);

            // Mock the HttpClientFactory to return a client using our mock handler
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };

            testItems.MockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient); // Use the properly mocked client

            // Mock SendAsync to return the fake response
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await testItems.UserRoleService.IsUserRoleSystemAdmin();

            // Assert
            result.Should().Be(expectedResult);
        }

        [Test]
        [TestCase(false, false, false, false, "Data Request Approver")]
        [TestCase(true, false, false, false, "Data Request Approver")]
        [TestCase(true, true, false, false, "Data Request Approver")]
        [TestCase(true, true, true, true, "Data Request Approver")]
        [TestCase(true, true, true, true, "Metadata Publisher")]
        public async Task GivenIsUserRoleSupplier_TestDiffereScenarion_ExpectedResult(bool hasUser, bool domainEnabled, bool organisationEnabled, bool expectedResult, string roleName)
        {
            // Arrange
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userRoles = new List<Role> { new Role() { RoleName = roleName } };

            var expectedProfile = new UserProfile
            {
                User = hasUser ? new UserInfo() { UserName = "tester" } : null,
                Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = domainEnabled },
                Organisation = new UserOrganisation() { IsEnabled = organisationEnabled },
                Roles = userRoles
            };

            var jwtToken = GenerateJwt("test@email.com", "tester");

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, jwtToken);

            // Mock the HttpClientFactory to return a client using our mock handler
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };

            testItems.MockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient); // Use the properly mocked client

            // Mock SendAsync to return the fake response
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await testItems.UserRoleService.IsUserRoleSupplier();

            // Assert
            result.Should().Be(expectedResult);
        }

        [Test]
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, false, false)]
        [TestCase(true, true, true, true)]
        public async Task GivenIsUserRolePublisher_TestDiffereScenarion_ExpectedResult(bool hasUser, bool domainEnabled, bool organisationEnabled, bool expectedResult)
        {
            // Arrange
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            var userRoles = new List<Role> { new Role() { RoleName = "Metadata Publisher" } };

            var expectedProfile = new UserProfile
            {
                User = hasUser ? new UserInfo() { UserName = "tester" } : null,
                Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = domainEnabled },
                Organisation = new UserOrganisation() { IsEnabled = organisationEnabled },
                Roles = userRoles
            };

            var jwtToken = GenerateJwt("test@email.com", "tester");

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };
            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, jwtToken);

            // Mock the HttpClientFactory to return a client using our mock handler
            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://my-base-url")
            };

            testItems.MockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(httpClient); // Use the properly mocked client

            // Mock SendAsync to return the fake response
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await testItems.UserRoleService.IsUserRolePublisher();

            // Assert
            result.Should().Be(expectedResult);
        }

        [Test]
        public async Task GivenRemoveUserFromRoleAsync_WhenIdTokenIsNull_ThrowException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            string roleId = fixture.Create<string>();
            string userId = fixture.Create<string>();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, "");

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            Func<Task> act = async () => await testItems.UserRoleService.RemoveUserFromRoleAsync(roleId, userId);

            await act.Should()
           .ThrowAsync<ArgumentException>()
           .WithMessage("RemoveUserFromRole: ID token is not available.");
        }

        [Test]
        public async Task GivenRemoveUserFromRoleAsync_WhenApiCallFails_ThrowException()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            string roleId = fixture.Create<string>();
            string userId = fixture.Create<string>();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            Func<Task> act = async () => await testItems.UserRoleService.RemoveUserFromRoleAsync(roleId, userId);

            await act.Should()
           .ThrowAsync<HttpRequestException>()
           .WithMessage("RemoveUserFromRoleAsync: Failed to remove user from role, HTTP BadRequest");
        }

        [Test]
        public async Task GivenRemoveUserFromRoleAsync_WhenAllWorks_ReturnsUserProfile()
        {
            var testItems = HttpTestsSetup.CreateTestItems("http://my-base-url");
            string roleId = fixture.Create<string>();
            string userId = fixture.Create<string>();
            var expectedProfile = new UserProfile { User = new UserInfo() { UserName = "tester" }, Domain = new UserDomain() { DomainName = "testOrgDomain", IsEnabled = true } };
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedProfile))
            };

            responseMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            HttpTestsSetup.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockHttpClientFactory
              .Setup(_ => _.CreateClient(It.IsAny<string>()))
              .Returns(_httpClient);

            _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

            var result = await testItems.UserRoleService.RemoveUserFromRoleAsync(roleId, userId);

            result.Should().BeEquivalentTo(expectedProfile);
        }
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
