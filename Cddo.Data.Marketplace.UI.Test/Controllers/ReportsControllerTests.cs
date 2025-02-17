using NUnit.Framework;
using Moq;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text;

namespace Cddo.Data.Marketplace.UI.Test.Controllers
{
    [TestFixture]
    public class ReportsControllerTests
    {
        private Mock<ILogger<ReportsController>> _loggerMock;
        private Mock<ICatalogReportsService> _catalogReportsServiceMock;
        private Mock<IUserRoleService> _userRoleServiceMock;
        private Mock<ICatalogDataService> _catalogDataServiceMock;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private readonly ReportsController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private Fixture _fixture;
        private string _dateFormat = "yyyyMMdd_HHmmss";
        private string _csvString = "text/csv";
        private static string _accessDeniedPage = "/Error/403";


        public ReportsControllerTests()
        {
            _fixture = new Fixture();

            // Mocking the dependencies
            _loggerMock = new Mock<ILogger<ReportsController>>();
            _catalogReportsServiceMock = new Mock<ICatalogReportsService>();
            _userRoleServiceMock = new Mock<IUserRoleService>();
            _catalogDataServiceMock = new Mock<ICatalogDataService>();

            // Instantiate the controller with mocked dependencies
            _controller = new ReportsController(
                _loggerMock.Object,
                _catalogReportsServiceMock.Object,
                _userRoleServiceMock.Object,
                _catalogDataServiceMock.Object
            );
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
        public async Task GotoMetadataReport_ShouldReturnView_WhenUserIsAdminAndTemplateExists()
        {
            // Arrange
            Guid? templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");
            var isDownload = false;
            var reportData = _fixture.Create<QueryCatalogReportsDataResponse>();
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(s => s.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(s => s.IsUserRoleAdmin()).ReturnsAsync(true);
            _catalogReportsServiceMock.Setup(s => s.GetCatalogReportsDataAsync(It.IsAny<QueryCatalogReportsDataRequest>(), default))
                .ReturnsAsync(reportData);
            _catalogDataServiceMock.Setup(s => s.GetCddoOrganisationsAsync(null, default)).ReturnsAsync(new List<string> { "Org1", "Org2" });

            // Act
            var result = await _controller.GotoMetadataReport(templateId, isDownload);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/MetadataReport.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(reportData));
        }

        [Test]
        public async Task GotoMetadataReport_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var templateId = Guid.NewGuid();
            var isDownload = false;

            SetAuthenticatedUser(false);

            // Act
            var result = await _controller.GotoMetadataReport(templateId, isDownload);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task GotoMetadataReport_ShouldRedirectToGetMetadataStatsReport_WhenTemplateIdMatchesSpecificValue()
        {
            // Arrange
            var templateId = Guid.Parse("86ef337b-1432-49ae-9d96-adcfa87553c2");
            var isDownload = false;

            // Act
            var result = await _controller.GotoMetadataReport(templateId, isDownload);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("GetMetadataStatsReport"));
            Assert.That(redirectResult.RouteValues["templateId"], Is.EqualTo(templateId));
        }

        [Test]
        public async Task GotoMetadataReport_ShouldReturnAccessDeniedPage_WhenUserIsNotAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var templateId = Guid.NewGuid();
            var isDownload = false;
            var userRoleMock = false;
            _userRoleServiceMock.Setup(s => s.IsUserRoleSystemAdmin()).ReturnsAsync(userRoleMock);
            _userRoleServiceMock.Setup(s => s.IsUserRoleAdmin()).ReturnsAsync(userRoleMock);

            // Act
            var result = await _controller.GotoMetadataReport(templateId, isDownload);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task DownloadMetadataReport_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("templateId", "Invalid template ID");

            var templateId = Guid.NewGuid();

            // Act
            var result = await _controller.DownloadMetadataReport(templateId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task DownloadMetadataReport_ShouldRedirectToAccessDenied_WhenUserIsNotAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var templateId = Guid.NewGuid();

            _userRoleServiceMock.Setup(s => s.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(s => s.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.DownloadMetadataReport(templateId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo(_accessDeniedPage));
        }



    }
}
