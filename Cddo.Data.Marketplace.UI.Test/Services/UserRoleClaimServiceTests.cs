using AutoFixture.AutoMoq;
using AutoFixture;
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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class UserRoleClaimServiceTests
    {
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfUserRoleClaimService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserRoleClaimService> logger,
            IConfiguration configuration)
        {
            Assert.That(() => new UserRoleClaimService(httpContextAccessor, logger, configuration),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }
        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<UserRoleClaimService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();

            yield return new TestCaseData("httpContextAccessor", null, logger, configuration);
            yield return new TestCaseData("logger", httpContextAccessor, null, configuration);
            yield return new TestCaseData("configuration", httpContextAccessor, logger, null);
        }

        #region GetUserRoleDetailsAsync
        [Test]
        public async Task SubmitOrganisationRequestAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleDetailsAsync();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task SubmitOrganisationRequestAsync_WhenEmailOrUsernameNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("", ""));
            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleDetailsAsync();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetUserRoleDetailsAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/userinfo")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleDetailsAsync();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetUserRoleDetailsAsync_WhenApiThrowsException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            using var httpTest = new HttpTest();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            httpTest.ForCallsTo($"http://xyz/User/userinfo")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleDetailsAsync();

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetUserRoleDetailsAsync_WhenApiCallSuccess_UserRoleDetails()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<UserProfileResponse>();
            var token = GenerateJwt("test@email.com", "myusername");
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, token);
            testResponse.Token = token;

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/userinfo")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleDetailsAsync();

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }
        #endregion

        #region SetUserEmailNotificationAsync

        [Test]
        public async Task SetUserEmailNotificationAsync_WhenTokenIsNull_Returns()
        {

            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var notificationDecision = false;
            var userId = 1;

            using var httpTest = new HttpTest();

            //Act
            await testItems.UserRoleClaimService.SetUserEmailNotificationAsync(notificationDecision, userId);

            //Assert
            httpTest.ShouldNotHaveCalled($"http://xyz/User/notifications");

        }

        [Test]
        public async Task SetUserEmailNotificationAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            var notificationDecision = false;
            var userId = 1;
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/notifications")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            await testItems.UserRoleClaimService.SetUserEmailNotificationAsync(notificationDecision, userId);

            //Assert
            httpTest.ShouldHaveCalled($"http://xyz/User/notifications");
        }

        [Test]
        public async Task SetUserEmailNotificationAsync_WhenApiThrowsException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            var notificationDecision = false;
            var userId = 1;
            using var httpTest = new HttpTest();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            await testItems.UserRoleClaimService.SetUserEmailNotificationAsync(notificationDecision, userId);

            //Assert
            httpTest.ShouldNotHaveCalled($"http://xyz/User/notifications");
        }

        [Test]
        public async Task SetUserEmailNotificationAsync_WhenApiCallSuccess_EmailNotificationSet()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<UserProfileResponse>();
            var token = GenerateJwt("test@email.com", "myusername");
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, token);
            testResponse.Token = token;
            var notificationDecision = false;
            var userId = 1;

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/notifications")
           .RespondWithJson(testResponse);

            //Act
            await testItems.UserRoleClaimService.SetUserEmailNotificationAsync(notificationDecision, userId);

            //Assert
            httpTest.ShouldHaveCalled($"http://xyz/User/notifications");
        }
        #endregion

        #region GetUserRoleApprovalListAsync
        [Test]
        public async Task GetUserRoleApprovalListAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var userId = testItems.Fixture.Create<int>();

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleApprovalListAsync(userId);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetUserRoleApprovalListAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var userId = testItems.Fixture.Create<int>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/myapprovals/{userId}")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleApprovalListAsync(userId);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetUserRoleApprovalListAsync_WhenApiThrowsException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var userId = testItems.Fixture.Create<int>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            using var httpTest = new HttpTest();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            httpTest.ForCallsTo($"http://xyz/User/myapprovals")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleApprovalListAsync(userId);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetUserRoleApprovalListAsync_WhenApiCallSuccess_UserRoleDetails()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<List<UserRoleApprovalDetailResponse>>();
            var token = GenerateJwt("test@email.com", "myusername");
            var userId = testItems.Fixture.Create<int>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, token);

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/myapprovals/{userId}")
           .RespondWithJson(testResponse);

            //Act
            var result = await testItems.UserRoleClaimService.GetUserRoleApprovalListAsync(userId);

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }
        #endregion

        #region SetUserRoleApprovalAsync

        [Test]
        public async Task SetUserRoleApprovalAsync_WhenTokenIsNull_Returns()
        {

            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<List<SetUserApprovalRequest>>();

            using var httpTest = new HttpTest();

            //Act
            await testItems.UserRoleClaimService.SetUserRoleApprovalAsync(request);

            //Assert
            httpTest.ShouldNotHaveCalled($"http://xyz/User/ApprovalRequest-multiple");

        }

        [Test]
        public async Task SetUserRoleApprovalAsync_WhenApiThrowsFlurException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            var request = testItems.Fixture.Create<List<SetUserApprovalRequest>>();
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/ApprovalRequest-multiple")
           .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            await testItems.UserRoleClaimService.SetUserRoleApprovalAsync(request);

            //Assert
            httpTest.ShouldHaveCalled($"http://xyz/User/ApprovalRequest-multiple");
        }

        [Test]
        public async Task SetUserRoleApprovalAsync_WhenApiThrowsException_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "myusername"));
            var request = testItems.Fixture.Create<List<SetUserApprovalRequest>>();
            using var httpTest = new HttpTest();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            await testItems.UserRoleClaimService.SetUserRoleApprovalAsync(request);

            //Assert
            httpTest.ShouldNotHaveCalled($"http://xyz/User/ApprovalRequest-multiple");
        }

        [Test]
        public async Task SetUserRoleApprovalAsync_WhenApiCallSuccess_EmailNotificationSet()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<UserProfileResponse>();
            var token = GenerateJwt("test@email.com", "myusername");
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, token);
            testResponse.Token = token;
            var request = testItems.Fixture.Create<List<SetUserApprovalRequest>>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/User/ApprovalRequest-multiple")
           .RespondWithJson(testResponse);

            //Act
            await testItems.UserRoleClaimService.SetUserRoleApprovalAsync(request);

            //Assert
            httpTest.ShouldHaveCalled($"http://xyz/User/ApprovalRequest-multiple");
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
