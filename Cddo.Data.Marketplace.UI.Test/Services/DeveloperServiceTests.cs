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
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using FluentAssertions;
using Cddo.Data.Marketplace.Api.Dto.Requests.ClientAuth;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Moq;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Org.BouncyCastle.Asn1.Cmp;
using Flurl.Http.Testing;
using Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth;
using System.Net;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class DeveloperServiceTests
    {
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfDeveloperService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeveloperService> logger,
            IUserRoleClaimService userRoleClaimService,
            IConfiguration configuration)
        {
            Assert.That(() => new DeveloperService(httpContextAccessor, logger, userRoleClaimService, configuration),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }
        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<DeveloperService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();
            var userRoleClaimService = fixture.Create<IUserRoleClaimService>();

            yield return new TestCaseData("httpContextAccessor", null, logger, userRoleClaimService, configuration);
            yield return new TestCaseData("logger", httpContextAccessor, null, userRoleClaimService, configuration);
            yield return new TestCaseData("userRoleClaimService", httpContextAccessor, logger, null, configuration);
            yield return new TestCaseData("configuration", httpContextAccessor, logger, userRoleClaimService, null);
        }

        [Test]
        public async Task CreateClientAuthCredentialAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<ClientAuthCredentialsRequest>();

            //Act
            var result = await testItems.DeveloperService.CreateClientAuthCredentialAsync(request);

            //Assert
            result.Should().Be(null);  
        }

        [Test]
        public async Task CreateClientAuthCredentialAsync_WhenTokenIsInvalidNull_ThrowSecurityTokenMalformedException()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<ClientAuthCredentialsRequest>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");

            //Act
            var result = await testItems.DeveloperService.CreateClientAuthCredentialAsync(request);


            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task CreateClientAuthCredentialAsync_WhenUserProfileIsNull_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<ClientAuthCredentialsRequest>();
            UserProfileResponse userProfile = null;
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockUserRoleClaimService.Setup(c => c.GetUserRoleDetailsAsync(default)).ReturnsAsync(userProfile);


            //Act
            var result = await testItems.DeveloperService.CreateClientAuthCredentialAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task CreateClientAuthCredentialAsync_WithProfileAndSuccessfullApiCall_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<ClientAuthCredentialsRequest>();
            var userProfile = testItems.Fixture.Create<UserProfileResponse>();
            var testResponse = testItems.Fixture.Create<ClientAuthCredentialsResponse>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            testItems.MockUserRoleClaimService.Setup(c => c.GetUserRoleDetailsAsync(default)).ReturnsAsync(userProfile);

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/generate-client")
               .RespondWithJson(testResponse);


            //Act
            var result = await testItems.DeveloperService.CreateClientAuthCredentialAsync(request);

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }


        [Test]
        public async Task GetClientAuthCredentialsAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();

            //Act
            var result = await testItems.DeveloperService.GetClientAuthCredentialsAsync();

            //Assert
            result.Should().BeNull();
        }


        [Test]
        public async Task GetClientAuthCredentialsAsync_WithProfileAndSuccessfullApiCall_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<List<ClientAuthCredentialsResponse>>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/credentials")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.DeveloperService.GetClientAuthCredentialsAsync();

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task GetClientAuthCredentialByIdAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var id = testItems.Fixture.Create<string>();

            //Act
            var result = await testItems.DeveloperService.GetClientAuthCredentialByIdAsync(id);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetClientAuthCredentialByIdAsync_WithProfileAndSuccessfullApiCall_ClientAuthCredential()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<ClientAuthCredentialsResponse>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            var id = testItems.Fixture.Create<string>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/credential/{id}")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.DeveloperService.GetClientAuthCredentialByIdAsync(id);

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task DeleteClientAuthCredentialByIdAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var id = testItems.Fixture.Create<string>();

            //Act
            var result = await testItems.DeveloperService.DeleteClientAuthCredentialByIdAsync(id);

            //Assert
            result.Should().BeFalse();
        }

        [Test]
        public async Task DeleteClientAuthCredentialByIdAsync_WithInvalidToken_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<List<ClientAuthCredentialsResponse>>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "invalid-token");
            var id = testItems.Fixture.Create<string>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/credentials/{id}")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.DeveloperService.DeleteClientAuthCredentialByIdAsync(id);

            //Assert
            result.Should().BeFalse();
        }

        [Test]
        public async Task DeleteClientAuthCredentialByIdAsync_WithValidToken_CredentialsRemoved()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<List<ClientAuthCredentialsResponse>>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            var id = testItems.Fixture.Create<string>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/credentials/{id}")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.DeveloperService.DeleteClientAuthCredentialByIdAsync(id);

            //Assert
            result.Should().BeTrue();
        }

        [Test]
        public async Task UpdateClientAuthCredentialByIdAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var id = testItems.Fixture.Create<string>();
            var updateRequest = testItems.Fixture.Create<ClientAuthCredentialsRequest>();

            //Act
            var result = await testItems.DeveloperService.UpdateClientAuthCredentialByIdAsync(id, updateRequest);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task UpdateClientAuthCredentialByIdAsync_WithProfileAndSuccessfullApiCall_UpdatedClientAuthCredential()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<ClientAuthCredentialsResponse>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            var id = testItems.Fixture.Create<string>();
            var updateRequest = testItems.Fixture.Create<ClientAuthCredentialsRequest>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/credential/{id}")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.DeveloperService.UpdateClientAuthCredentialByIdAsync(id, updateRequest);

            //Assert
            result.Should().BeEquivalentTo(testResponse);
        }

        [Test]
        public async Task UpdateClientAuthCredentialByIdAsync_WhenApiCallThrows_InvalidOperationException()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testResponse = testItems.Fixture.Create<ClientAuthCredentialsResponse>();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, GenerateJwt("test@email.com", "tester"));
            var id = testItems.Fixture.Create<string>();
            var updateRequest = testItems.Fixture.Create<ClientAuthCredentialsRequest>();

            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/ClientAuth/credential/{id}")
               .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            Func<Task> act = async () => await testItems.DeveloperService.UpdateClientAuthCredentialByIdAsync(id, updateRequest);

            //Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
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
