using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results.SpreadsheetIngestion.ValidatedDataAssetSpreadsheetItems;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NPOI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.UI.Test.Controllers
{
    [TestFixture]
    public class CatalogSpreadsheetControllerTests
    {
        private Mock<ICatalogSpreadsheetService> _mockCatalogSpreadsheetService;
        private Mock<IUserRoleService> _mockUserRoleService;
        private Mock<IUserProfilePresenter> _mockUserProfilePresenter;
        private Mock<IAppInsightsLogger> _mockAppInsightLogger;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private CatalogSpreadsheetController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private Fixture _fixture;


        public CatalogSpreadsheetControllerTests()
        {
            _mockCatalogSpreadsheetService = new Mock<ICatalogSpreadsheetService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockUserProfilePresenter = new Mock<IUserProfilePresenter>();
            _mockAppInsightLogger = new Mock<IAppInsightsLogger>();
            _fixture = new Fixture();

            _controller = new CatalogSpreadsheetController(
                _mockCatalogSpreadsheetService.Object,
                _mockUserRoleService.Object,
                _mockUserProfilePresenter.Object,
                _mockAppInsightLogger.Object
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
        public async Task DownloadSpreadsheetTemplate_ReturnsFile_WhenUserIsAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var fileContent = _fixture.Create<byte[]>();
            _mockCatalogSpreadsheetService.Setup(s => s.DownloadSpreadsheetTemplateAsync(default)).ReturnsAsync(fileContent);
            var userProfile = _fixture.Create<UserProfile>();
            _mockUserRoleService.Setup(s => s.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Act
            var result = await _controller.DownloadSpreadsheetTemplate() as FileContentResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContentType, Is.EqualTo("application/octet-stream"));
            Assert.That(result.FileDownloadName, Is.EqualTo("Template For Data Descriptions.xlsx"));
        }
        [Test]
        public async Task DownloadSpreadsheetTemplate_ReturnsNull_WhenUserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);

            // Act
            var result = await _controller.DownloadSpreadsheetTemplate();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void AddNewUploadSpreadsheet_ReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);

            // Act
            var result = _controller.AddNewUploadSpreadsheet() as ViewResult;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/UploadSpreadsheet.cshtml"));
        }

        [Test]
        public async Task AddNewUploadSubmit_ReturnsView_WhenFileIsNull()
        {
            // Arrange
            SetAuthenticatedUser(true);

            // Act
            var result = await _controller.AddNewUploadSubmit(null) as ViewResult;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/UploadSpreadsheet.cshtml"));
        }
        [Test]
        public async Task AddNewUploadSubmit_ReturnsView_WhenModelErrors()
        {
            // Arrange
            _controller.ModelState.AddModelError("test", "error");
            
            // Act
            var result = await _controller.AddNewUploadSubmit(null) as ViewResult;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/UploadSpreadsheet.cshtml"));
        }

        [Test]
        public async Task AddNewUploadSubmit_RedirectsOnSuccess()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var mockFile = new Mock<IFormFile>();
            var uploadReturn = _fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            mockFile.Setup(f => f.Length).Returns(1);

            _mockCatalogSpreadsheetService.Setup(s => s.UploadSpreadsheetAsync(It.IsAny<IFormFile>(), default)).ReturnsAsync(uploadReturn);

            // Act
            var result = await _controller.AddNewUploadSubmit(mockFile.Object) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("GetValidatedDataAssetsSpreadsheet"));
        }
        [Test]
        public async Task AddNewUploadSubmit_ReturnsView_WHenUserNotAuthenticated()
        {
            // Arrnange
            SetAuthenticatedUser(false);

            var mockFile = new Mock<IFormFile>();
            var uploadReturn = _fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            mockFile.Setup(f => f.Length).Returns(1);

            _mockCatalogSpreadsheetService.Setup(s => s.UploadSpreadsheetAsync(It.IsAny<IFormFile>(), default)).ReturnsAsync(uploadReturn);

            // Act
            var result = await _controller.AddNewUploadSubmit(mockFile.Object) as ViewResult;


            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/UploadSpreadsheet.cshtml"));
        }

        [Test]
        public async Task GetValidatedDataAssetsSpreadsheet_ReturnsViewWithModel()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var response = _fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            _mockCatalogSpreadsheetService.Setup(s => s.GetValidatedDataAssetsSpreadsheetAsync(default)).ReturnsAsync(response);
            
            var duplicateResponse = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse>();
            _mockCatalogSpreadsheetService.Setup(s => s.CheckForPotentialDuplicatesInValidatedSpreadsheetContentAsync(default)).ReturnsAsync(duplicateResponse);

            // Act
            var result = await _controller.GetValidatedDataAssetsSpreadsheet() as ViewResult;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/SpreadsheetDataAssets.cshtml"));
        }

        [Test]
        public async Task GetValidatedDataAssetSpreadsheet_ReturnsViewWithModel()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var response = new ValidatedDataAssetSpreadsheetItemSummary() 
            { 
                AssetTitle="test", 
                CoreProperties = null,
                DataProperties= null
            };
            _mockCatalogSpreadsheetService.Setup(s => s.GetValidatedDataAssetSpreadsheetItemAsync(It.IsAny<string>(), default)).ReturnsAsync(response);

            var duplicateResponse = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse>();
            _mockCatalogSpreadsheetService.Setup(s => s.CheckForPotentialDuplicatesInValidatedSpreadsheetItemAsync(It.IsAny<string>(), default)).ReturnsAsync(duplicateResponse);

            // Act
            var result = await _controller.GetValidatedDataAssetSpreadsheet("testRecordId") as ViewResult;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/SpreadsheetDataAssetSummary.cshtml"));
        }

        [Test]
        public async Task ClearSpreadsheetDataAssets_RedirectsToAddNewUploadSpreadsheet()
        {
            // Arrange
            SetAuthenticatedUser(true);

            // Act
            var result = await _controller.ClearSpreadsheetDataAssets() as RedirectToActionResult;
            
            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("AddNewUploadSpreadsheet"));
        }

        [Test]
        public void ConvertByteArrayToFormFile_ReturnsValidFormFile()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("test", "test");

            var fileBytes = _fixture.Create<byte[]>();
            var fileName = "test.xlsx";
            
            // Act
            var formFile = _controller.ConvertByteArrayToFormFile(fileBytes, fileName);

            // Assert
            Assert.That(formFile, Is.Not.Null);
            Assert.That(formFile.FileName, Is.EqualTo(fileName));
            Assert.That(formFile.Length, Is.EqualTo(fileBytes.Length));
        }
        [Test]
        public async Task DataShareRequestNotificationsSelection_ReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var domain = new Mock<IDomainInformation>();
            _mockUserProfilePresenter.Setup(x => x.GetDomainInformationOfInitiatingUserAsync()).ReturnsAsync(domain.Object);
            
            // Act
            var result = await _controller.DataShareRequestNotificationsSelection();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task PublishSpreadsheetDataAssets__RedirectsOnErrorResponse()
        {
            // Arrange
            var formData = new Mock<IFormCollection>();
            var resultData = new Mock<IPublishSpreadsheetDataAssetsResult>();
            var validationResult = _fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            validationResult.Errors = _fixture.Create<List<string>>();
            validationResult.Success = false;
            resultData.Setup(x => x.DataShareRequestNotificationAddressValidationResult.RequestWasValid).Returns(true);
            resultData.Setup(x => x.Response).Returns(validationResult);


            _mockCatalogSpreadsheetService.Setup(s => s.PublishSpreadsheetDataAssetsAsync(formData.Object, default))
                .ReturnsAsync(resultData.Object);

            // Act
            var result = await _controller.PublishSpreadsheetDataAssets(formData.Object);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectresult = result as RedirectToActionResult;
            Assert.That(redirectresult.ActionName, Is.EqualTo("GetValidatedDataAssetsSpreadsheet"));
        }

        [Test]
        public async Task PublishSpreadsheetDataAssets_ReturnsView_WhenRequestIsInvalid()
        {
            // Arrange
            var formData = new Mock<IFormCollection>();
            var resultData = new Mock<IPublishSpreadsheetDataAssetsResult>();
            resultData.Setup(x => x.DataShareRequestNotificationAddressValidationResult.RequestWasValid).Returns(false);
            resultData.Setup(x => x.DataShareRequestNotificationAddressValidationResult.ValidationErrors).Returns(_fixture.Create<Dictionary<string,string>>());

            _mockCatalogSpreadsheetService.Setup(s => s.PublishSpreadsheetDataAssetsAsync(formData.Object, default))
                .ReturnsAsync(resultData.Object);

            // Act
            var result = await _controller.PublishSpreadsheetDataAssets(formData.Object) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/DataShareRequestNotificationsSelection.cshtml"));
        }

        [Test]
        public async Task PublishSpreadsheetDataAssets_ReturnsConfirmationView_OnSuccess()
        {
            // Arrange
            var formData = new Mock<IFormCollection>();
            var resultData = new Mock<IPublishSpreadsheetDataAssetsResult>();
            resultData.Setup(x => x.DataShareRequestNotificationAddressValidationResult.RequestWasValid).Returns(true);
            resultData.Setup(x => x.Response).Returns(_fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>());
            var validationResult = _fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentResponse>();
            validationResult.Success = true;
            resultData.Setup(x => x.DataShareRequestNotificationAddressValidationResult.RequestWasValid).Returns(true);
            resultData.Setup(x => x.Response).Returns(validationResult);
            //resultData.Setup(x => x.Response.Success).Returns(true);

            _mockCatalogSpreadsheetService.Setup(s => s.PublishSpreadsheetDataAssetsAsync(formData.Object, default))
                .ReturnsAsync(resultData.Object);

            // Act
            var result = await _controller.PublishSpreadsheetDataAssets(formData.Object) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Upload/UploadConfirmation.cshtml"));
        }
    }
}
