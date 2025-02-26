using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Api.Dto.Requests.ClientAuth;
using Cddo.Data.Marketplace.Api.Dto.Responses.ClientAuth;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.UI.Test.Controllers
{

    [TestFixture]
    public class DeveloperControllerTests
    {
        private Mock<ILogger<DeveloperController>> _mockLogger;
        private Mock<IDeveloperService> _mockDeveloperService;
        private Mock<IUserRoleService> _mockUserRoleService;
        private Mock<IAppInsightsLogger> _mockAppInsightsLogger;
        private Mock<IUserProfilePresenter> _mockUserProfilePresenter;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private DeveloperController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private Fixture _fixture;
        private UserDetails _userProfile;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<DeveloperController>>();
            _mockDeveloperService = new Mock<IDeveloperService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockAppInsightsLogger = new Mock<IAppInsightsLogger>();
            _mockUserProfilePresenter = new Mock<IUserProfilePresenter>();
            _fixture = new Fixture();

            _controller = new DeveloperController(
                _mockLogger.Object,
                _mockDeveloperService.Object,
                _mockUserRoleService.Object,
                _mockAppInsightsLogger.Object,
                _mockUserProfilePresenter.Object);

            var userIdSet = new UserIdSet()
            {
                UserId = 1,
                DomainId = 1,
                OrganisationId = 1
            };

            var userContactDetails = new UserContactDetails()
            {
                UserName = "test user",
                EmailAddress = "test@email.com"
            };
            var organisationDetails = new OrganisationInformation()
            {
                OrganisationId = 1,
                OrganisationName = "Test Org",
                Domains = new List<IDomainInformation>()

            };

            _userProfile = new UserDetails
            {
                UserIdSet = userIdSet,
                UserContactDetails = userContactDetails,
                OrganisationInformation = organisationDetails
            };
        }

        private void SetAuthenticatedUser(bool isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "TestUser")
            };

            ClaimsIdentity identity;

            if (isAuthenticated)
            {
                identity = new ClaimsIdentity(claims, "TestAuthenticationType");
            }
            else
            {
                identity = new ClaimsIdentity();
            }

            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Test]
        public async Task ApiCredentials_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.ApiCredentials();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task ApiCredentials_UserIsAuthorized_ReturnsCredentialsViewWithData()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var credentials = _fixture.Create<List<ClientAuthCredentialsResponse>>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialsAsync(It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credentials);

            // Act
            var result = await _controller.ApiCredentials();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ApiCredentials.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credentials));
        }

        [Test]
        public async Task ApiCredentials_ExceptionThrown_ReturnsCredentialsViewWithNullModel()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialsAsync(It.IsAny<CancellationToken>()))
                                  .ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.ApiCredentials();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ApiCredentials.cshtml"));
            Assert.That(viewResult.Model, Is.Null);
        }
        [Test]
        public async Task GetApiCredentialById_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GetApiCredentialById("test-id");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task GetApiCredentialById_CredentialExists_ReturnsViewWithCredential()
        {
            // Arrange
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();

            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.GetApiCredentialById("test-id");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }

        [Test]
        public async Task GetApiCredentialById_CredentialNotFound_ReturnsViewWithNullModel()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((ClientAuthCredentialsResponse)null);

            // Act
            var result = await _controller.GetApiCredentialById("test-id");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.Null);
        }

        [Test]
        public async Task GetApiCredentialById_ExceptionThrown_ReturnsViewWithNullModel()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                  .ThrowsAsync(new Exception("Test Exception"));


            // Act
            var result = await _controller.GetApiCredentialById("test-id");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.Null);
        }
        [Test]
        public async Task GotoCreateApiCredential_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GotoCreateApiCredential();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task GotoCreateApiCredential_UserIsAuthorized_ReturnsCreateCredentialView()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(_userProfile);

            // Act
            var result = await _controller.GotoCreateApiCredential();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/CreateApiCredential.cshtml"));
        }
        [Test]
        public async Task CreateApiCredential_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var request = _fixture.Create<ClientAuthCredentialsRequest>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.CreateApiCredential(request);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task CreateApiCredential_ModelStateInvalid_ReturnsCreateCredentialView()
        {
            // Arrange
            var request = _fixture.Create<ClientAuthCredentialsRequest>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _controller.ModelState.AddModelError("Error", "Invalid Model");

            // Act
            var result = await _controller.CreateApiCredential(request);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/CreateApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(request));
        }

        [Test]
        public async Task CreateApiCredential_CreationFails_ReturnsCreateCredentialViewWithError()
        {
            // Arrange
            var request = _fixture.Create<ClientAuthCredentialsRequest>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.CreateClientAuthCredentialAsync(It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((ClientAuthCredentialsResponse)null);

            // Act
            var result = await _controller.CreateApiCredential(request);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/CreateApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(request));
            Assert.That(_controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public async Task CreateApiCredential_CreationSucceeds_ReturnsStoreApiCredentialsView()
        {
            // Arrange
            var request = _fixture.Create<ClientAuthCredentialsRequest>();
            var response = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.CreateClientAuthCredentialAsync(It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(response);
            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(_userProfile);


            // Act
            var result = await _controller.CreateApiCredential(request);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/StoreApiCredentials.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(response));
        }
        [Test]
        public async Task CreateApiCredential_ExceptionThrown_ReturnsCreateCredentialViewWithError()
        {
            // Arrange
            var request = _fixture.Create<ClientAuthCredentialsRequest>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.CreateClientAuthCredentialAsync(It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.CreateApiCredential(request);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/CreateApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(request));
            Assert.That(_controller.ModelState.ErrorCount, Is.GreaterThan(0));
        }
        [Test]
        public async Task ApiCredentialConfirmation_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.ApiCredentialConfirmation();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task ApiCredentialConfirmation_UserIsAuthorized_ReturnsStoreApiCredentialsView()
        {
            // Arrange
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);

            // Act
            var result = await _controller.ApiCredentialConfirmation();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/StoreApiCredentials.cshtml"));
        }
        [Test]
        public async Task GotoRevokeCredentials_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GotoRevokeCredentials(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task GotoRevokeCredentials_UserIsAuthorized_ReturnsRevokeCredentialsViewWithCredential()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.GotoRevokeCredentials(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/RevokeCredentials.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }
        [Test]
        public async Task RevokeCredentials_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.RevokeCredentials(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task RevokeCredentials_UserIsAuthorized_DeletesCredentialAndRedirects()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.DeleteClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeCredentials(id);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo(nameof(DeveloperController.ApiCredentials)));
        }

        [Test]
        public async Task GotoUpdateApiName_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GotoUpdateApiName(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task GotoUpdateApiName_UserIsAuthorized_ReturnsEditCredentialView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>(); 
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.GotoUpdateApiName(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialName.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }
        [Test]
        public async Task UpdateApiName_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var appName = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateApiName(id, appName);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task UpdateApiName_UserIsAuthorized_UpdatesCredentialAndReturnsView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var appName = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();

            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);
            _mockDeveloperService.Setup(x => x.UpdateClientAuthCredentialByIdAsync(id, It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.UpdateApiName(id, appName);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }
        [Test]
        public async Task GotoUpdateApiScope_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GotoUpdateApiScope(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task GotoUpdateApiScope_UserIsAuthorized_ReturnsEditCredentialScopeView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();

            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.GotoUpdateApiScope(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialScope.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }
        [Test]
        public async Task UpdateApiScope_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var scopes = new List<string> { "scope1", "scope2" };
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateApiScope(id, scopes);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task UpdateApiScope_ScopesAreNull_ReturnsEditCredentialScopeViewWithModelError()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.UpdateApiScope(id, null);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialScope.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }

        [Test]
        public async Task UpdateApiScope_ValidRequest_UpdatesCredentialAndReturnsView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var scopes = new List<string> { "scope1", "scope2" };
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);
            _mockDeveloperService.Setup(x => x.UpdateClientAuthCredentialByIdAsync(id, It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.UpdateApiScope(id, scopes);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
        }
        [Test]
        public async Task UpdateApiScope_InvalidModelState_ReturnsView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var scopes = new List<string> { "scope1", "scope2" };
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);
            _controller.ModelState.AddModelError("Scopes", "Invalid scope selection");

            // Act
            var result = await _controller.UpdateApiScope(id, scopes);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }

        [Test]
        public async Task GotoUpdateApiExpiry_UserIsNotAuthorized_ReturnsLandingPageView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(false);
            _mockUserRoleService.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GotoUpdateApiExpiry(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/APIPortal/APILandingPage.cshtml"));
        }

        [Test]
        public async Task GotoUpdateApiExpiry_CredentialNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((ClientAuthCredentialsResponse)null);

            // Act
            var result = await _controller.GotoUpdateApiExpiry(id);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult.Value, Is.EqualTo("Credential not found."));
        }

        [Test]
        public async Task GotoUpdateApiExpiry_CredentialExists_ReturnsEditCredentialExpiryView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRolePublisher()).ReturnsAsync(true);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.GotoUpdateApiExpiry(id);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialExpiry.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }
        [Test]
        public async Task UpdateApiExpiry_InvalidDate_ReturnsEditCredentialExpiryView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            _controller.ModelState.AddModelError("expiryDate", "Invalid date");
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.UpdateApiExpiry(id, "32", "13", "1899", CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialExpiry.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }

        [Test]
        public async Task UpdateApiExpiry_ValidDate_UpdatesCredentialAndReturnsView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            var updatedCredential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);
            _mockDeveloperService.Setup(x => x.UpdateClientAuthCredentialByIdAsync(id, It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(updatedCredential);
            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(_userProfile);

            // Act
            var result = await _controller.UpdateApiExpiry(id, "10", "12", "2025", CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/ViewApiCredential.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(updatedCredential));
        }
        [Test]
        public async Task UpdateApiExpiry_ModelStateContainsKeys_ClearsErrors()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _controller.ModelState.AddModelError("expiryDay", "Some error");
            _controller.ModelState.AddModelError("expiryMonth", "Some error");
            _controller.ModelState.AddModelError("expiryYear", "Some error");

            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.UpdateApiExpiry(id, "10", "12", "2025", CancellationToken.None);

            // Assert
            Assert.That(_controller.ModelState.ContainsKey("expiryDay"), Is.True);
            Assert.That(_controller.ModelState["expiryDay"].Errors, Is.Empty);
            Assert.That(_controller.ModelState.ContainsKey("expiryMonth"), Is.True);
            Assert.That(_controller.ModelState["expiryMonth"].Errors, Is.Empty);
            Assert.That(_controller.ModelState.ContainsKey("expiryYear"), Is.True);
            Assert.That(_controller.ModelState["expiryYear"].Errors, Is.Empty);
        }
        [Test]
        public async Task UpdateApiExpiry_ExpiryInPast_ReturnsEditCredentialExpiryView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            // Act
            var result = await _controller.UpdateApiExpiry(id, "10", "12", "2000", CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialExpiry.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }

        [Test]
        public async Task UpdateApiExpiry_ExpiryMoreThan12MonthsAhead_ReturnsEditCredentialExpiryView()
        {
            // Arrange
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);

            var futureYear = DateTime.UtcNow.AddYears(2).Year.ToString();

            // Act
            var result = await _controller.UpdateApiExpiry(id, "10", "12", futureYear, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialExpiry.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(credential));
        }
        [Test]
        public async Task UpdateApiExpiry_ExceptionThrown_ReturnsEditCredentialExpiryView()
        {
            var id = _fixture.Create<string>();
            var credential = _fixture.Create<ClientAuthCredentialsResponse>();
            _mockDeveloperService.Setup(x => x.GetClientAuthCredentialByIdAsync(id, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(credential);
            _mockDeveloperService.Setup(x => x.UpdateClientAuthCredentialByIdAsync(id, It.IsAny<ClientAuthCredentialsRequest>(), It.IsAny<CancellationToken>()))
                                  .Throws(new Exception("Database error"));

            var result = await _controller.UpdateApiExpiry(id, "10", "12", "2025", CancellationToken.None);

            var viewResult = result as ViewResult;
            Assert.That(viewResult?.ViewName, Is.EqualTo("~/Pages/Developer/EditCredentialExpiry.cshtml"));
            Assert.That(viewResult?.Model, Is.EqualTo(credential));
        }
    }
}
