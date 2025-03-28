using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Responses.RequestAccess;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
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
    public class RequestAccessControllerTests
    {
        private Mock<IRequestAccessService> _mockRequestAccessService;
        private Mock<IUserRoleService> _mockUserRoleService;
        private Mock<ILogger<RequestAccessController>> _mockLogger;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private RequestAccessController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _mockRequestAccessService = new Mock<IRequestAccessService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockLogger = new Mock<ILogger<RequestAccessController>>();
            _fixture = new Fixture();

            _controller = new RequestAccessController(
                _mockRequestAccessService.Object,
                _mockUserRoleService.Object,
                _mockLogger.Object);
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
        public void ManageOrganisation_ShouldReturnView()
        {
            var result = _controller.ManageOrganisation();
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task GetOrganisationsRequest_WhenUserIsSystemAdmin_ShouldReturnView()
        {
            // Arrange 
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            var mockResult = _fixture.Create<List<OrganisationAccessResponse>>();
            _mockRequestAccessService.Setup(x => x.GetOrganisationAllRequestAsync(default)).ReturnsAsync(mockResult);

            // Act
            var result = await _controller.GetOrganisationsRequest();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(mockResult));
        }

        [Test]
        public async Task GetOrganisationRequest_WhenUserIsSystemAdmin_ShouldReturnView()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var mockResult = _fixture.Create<OrganisationAccessResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _mockRequestAccessService.Setup(x => x.GetOrganisationRequestByIdAsync(It.IsAny<int>(), default)).ReturnsAsync(mockResult);

            // Act
            var result = await _controller.GetOrganisationRequest((int)mockResult.OrganisationRequestID);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(mockResult));
        }
        [Test]
        public async Task GetOrganisationsRequest_UserNotAuthenticated_ModelErrors()
        {
            //Arrange
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("error", "test Error");


            // Act
            var result = await _controller.GetOrganisationsRequest();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }
        [Test]
        public async Task GetOrganisationRequest_UserNotAuthenticated_ModelErrors()
        {
            //Arrange
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("error", "test Error");


            // Act
            var result = await _controller.GetOrganisationRequest(1);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task UpdateOrganisationRequest_WhenUserIsSystemAdmin_ShouldUpdateAndRedirect()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var mockResult = _fixture.Create<OrganisationAccessResponse>();
            _mockUserRoleService.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _mockRequestAccessService.Setup(x => x.GetOrganisationRequestByIdAsync(It.IsAny<int>(), default)).ReturnsAsync(mockResult);

            // Act
            var result = await _controller.UpdateOrganisationRequest(mockResult);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        }
        [Test]
        public async Task UpdateOrganisationRequest_UserNotAuthenticated_ModelErrors()
        {
            //Arrange
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("error", "test Error");

            var mockResult = _fixture.Create<OrganisationAccessResponse>();

            // Act
            var result = await _controller.UpdateOrganisationRequest(mockResult);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        [Test]
        public async Task UpdateOrganisationRequest_UserNotAuthenticated()
        {
            //Arrange
            SetAuthenticatedUser(false);

            var mockResult = _fixture.Create<OrganisationAccessResponse>();

            // Act
            var result = await _controller.UpdateOrganisationRequest(mockResult);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public void RejectOrganisationAccess_ShouldReturnViewWithModel()
        {
            // Arrange
            int organisationRequestID = 1;

            // Act
            var result = _controller.RejectOrganisationAccess(organisationRequestID);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as OrganisationAccessResponse;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.OrganisationRequestID, Is.EqualTo(organisationRequestID));
        }
        [Test]
        public void RejectOrganisationAccess_ModelErrors_ShouldReturnViewWithModel()
        {
            // Arrange
            int organisationRequestID = 1;
            _controller.ModelState.AddModelError("error", "test error");

            // Act
            var result = _controller.RejectOrganisationAccess(organisationRequestID);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            var model = viewResult.Model as OrganisationAccessResponse;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.OrganisationRequestID, Is.EqualTo(organisationRequestID));
        }

        [Test]
        public async Task EditOrganisationAccess_ShouldReturnViewWithModel()
        {
            // Arrange
            var mockResult = _fixture.Create<OrganisationAccessResponse>();
            _mockRequestAccessService.Setup(x => x.GetOrganisationRequestByIdAsync(It.IsAny<int>(), default)).ReturnsAsync(mockResult);

            // Act
            var result = await _controller.EditOrganisationAccess((int)mockResult.OrganisationRequestID);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(mockResult));
        }
        [Test]
        public async Task EditOrganisationAccess_HasModelErrors()
        {
            // Arrange
            var mockResult = _fixture.Create<OrganisationAccessResponse>();
            _mockRequestAccessService.Setup(x => x.GetOrganisationRequestByIdAsync(It.IsAny<int>(), default)).ReturnsAsync(mockResult);
            _controller.ModelState.AddModelError("error", "test error");

            // Act
            var result = await _controller.EditOrganisationAccess((int)mockResult.OrganisationRequestID);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(mockResult));
        }

        [Test]
        public async Task UpdateAccessStatus_WhenUserIsSystemAdmin_ShouldUpdateAndRedirect()
        {
            // Arrange
            int organisationRequestID = 1;
            string status = "Approved";
            string reason = "Valid reason";
            var mockResult = _fixture.Create<OrganisationAccessResponse>();

            _mockUserRoleService.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _mockRequestAccessService.Setup(x => x.GetOrganisationRequestByIdAsync(It.IsAny<int>(), default)).ReturnsAsync(mockResult);
            _mockRequestAccessService.Setup(x => x.UpdateOrganisationRequestAsync(It.IsAny<OrganisationAccessResponse>(), default)).ReturnsAsync(1);

            SetAuthenticatedUser(true);


            // Act
            var result = await _controller.UpdateAccessStatus(organisationRequestID, status, reason);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Manage/ManageOrganisation"));
        }
        [Test]
        public async Task UpdateAccessStatus_UsernotAuthenticated_RedirectToAction()
        {
            // Arrange
            int organisationRequestID = 1;
            string status = "Approved";
            string reason = "Valid reason";
            SetAuthenticatedUser(false);

            _controller.ModelState.AddModelError("Error", "test error");

            // Act
            var result = await _controller.UpdateAccessStatus(organisationRequestID, status, reason);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("GetOrganisationsRequest"));
        }
    }
}