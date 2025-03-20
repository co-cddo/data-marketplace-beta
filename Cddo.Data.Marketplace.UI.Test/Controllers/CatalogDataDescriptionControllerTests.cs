using Agm.Catalog.DotNet.Core.Validation.EmailAddress;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets.Models;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Model.Enum;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Test.Controllers
{
    [TestFixture]
    public class CatalogDataDescriptionControllerTests
    {
        private Mock<ICatalogDataService> _mockCatalogDataService;
        private Mock<ICatalogQuestionsService> _mockCatalogQuestionsService;
        private Mock<IUserRoleService> _mockUserRoleService;
        private Mock<IAppInsightsLogger> _mockLogger;
        private Mock<ICddoEmailAddressValidation> _mockEmailValidator;
        private IFixture _fixture;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private CatalogDataDescriptionController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

        #region SetUp
        public CatalogDataDescriptionControllerTests()
        {
            _fixture = new Fixture();
            _mockCatalogDataService = new Mock<ICatalogDataService>();
            _mockCatalogQuestionsService = new Mock<ICatalogQuestionsService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockLogger = new Mock<IAppInsightsLogger>();
            _mockEmailValidator = new Mock<ICddoEmailAddressValidation>();

            _controller = new CatalogDataDescriptionController(
                _mockCatalogDataService.Object,
                _mockCatalogQuestionsService.Object,
                _mockUserRoleService.Object,
                _mockLogger.Object,
                _mockEmailValidator.Object);
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

        public void ClearInvocations()
        {
            _mockCatalogDataService.Invocations.Clear();
            _mockCatalogQuestionsService.Invocations.Clear();
            _mockUserRoleService.Invocations.Clear();
            _mockLogger.Invocations.Clear();
        }

        #endregion

        [Test]
        public async Task DataDescriptionDashboard_ReturnsViewResultWithCorrectViewPath()
        {
            // Arrange
            var userProfile = _fixture.Create<UserProfile>();
            _mockUserRoleService.Setup(s => s.GetUserProfileAsync())
                .ReturnsAsync(userProfile);

            // Act
            var result = await _controller.DataDescriptionDashboard();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/DataDescriptionDashboard.cshtml"));
        }
        [Test]
        public async Task AddNewDataDescription_ReturnsViewResultWithCorrectViewPath()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            // Act
            var result = await _controller.AddNewDataDescription();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/AddNewDataDescription.cshtml"));
        }
        [Test]
        public async Task AddNewDataDescription_ReturnsRedirectToPageResult_WhenUserNotInRole()
        {
            // Arrange
            SetAuthenticatedUser(false);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(false);
            // Act
            var result = await _controller.AddNewDataDescription();

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/NoPermissions"));
        }
        [Test]
        public async Task AddDataDescriptionMethodSubmit_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            // Arrange
            SetAuthenticatedUser(false);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(false);
            _controller.ModelState.AddModelError("TestError", "Invalid Model State");

            // Act
            var result = await _controller.AddDataDescriptionMethodSubmit(null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/400"));
        }

        [Test]
        public async Task AddDataDescriptionMethodSubmit_UserWithoutRole_RedirectsToIndex()
        {
            // Arrange
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(false);

            // Act
            var result = await _controller.AddDataDescriptionMethodSubmit(NewDataDescriptionMethod.Manual);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Index"));
        }

        [Test]
        public async Task AddDataDescriptionMethodSubmit_ManualMethod_RedirectsToDataDescriptionType()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            // Act
            var result = await _controller.AddDataDescriptionMethodSubmit(NewDataDescriptionMethod.Manual);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo("SecurityClassification"));
        }

        [Test]
        public async Task AddDataDescriptionMethodSubmit_ApiMethod_RedirectsToDataDescriptionApiStart()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            // Act
            var result = await _controller.AddDataDescriptionMethodSubmit(NewDataDescriptionMethod.API);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo("DataDescriptionApiStart"));
        }

        [Test]
        public async Task AddDataDescriptionMethodSubmit_SpreadsheetMethod_RedirectsToCatalogSpreadsheetController()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            // Act
            var result = await _controller.AddDataDescriptionMethodSubmit(NewDataDescriptionMethod.Spreadsheet);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo("AddNewUploadSpreadsheet"));
            Assert.That(redirectResult.ControllerName, Is.EqualTo("CatalogSpreadsheet"));
        }

        [Test]
        public async Task AddDataDescriptionMethodSubmit_NullMethod_ReturnsViewWithModelError()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            // Act
            var result = await _controller.AddDataDescriptionMethodSubmit(null);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/Manual/SecurityClassification.cshtml"));
            Assert.That(_controller.ModelState.ContainsKey("dataDescriptionMethod"), Is.True);
        }
        [Test]
        public async Task DataDescriptionApiStart_ReturnsCorrectView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            // Act
            var result = await _controller.DataDescriptionApiStart();

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Api/Start.cshtml"));
        }

        //[Test]
        //public async Task DataDescriptionType_ReturnsCorrectView()
        //{
        //    // Arrange
        //    SetAuthenticatedUser(true);
        //    _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
        //    // Act
        //    var result = await _controller.data();

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<ViewResult>());
        //    var viewResult = result as ViewResult;
        //    Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/DataDescriptionType.cshtml"));
        //}
        //[Test]
        //public async Task DataDescriptionTypeSubmit_InvalidModelState_ReturnsBadRequest()
        //{
        //    // Arrange
        //    _controller.ModelState.AddModelError("TestError", "Invalid Model State");

        //    // Act
        //    var result = await _controller.DataDescriptionTypeSubmit(true);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        //    var redirectResult = result as RedirectToPageResult;
        //    Assert.That(redirectResult.PageName, Is.EqualTo("/Error/400"));
        //}

        //[Test]
        //public async Task DataDescriptionTypeSubmit_UserWithoutRole_RedirectsToIndex()
        //{
        //    // Arrange
        //    SetAuthenticatedUser(false);
        //    _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(false);

        //    // Act
        //    var result = await _controller.DataDescriptionTypeSubmit(true);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        //    var redirectResult = result as RedirectToPageResult;
        //    Assert.That(redirectResult.PageName, Is.EqualTo("/Index"));
        //}

        //[Test]
        //public async Task DataDescriptionTypeSubmit_ConfirmDataDescription_RedirectsToSecurityClassification()
        //{
        //    // Arrange
        //    SetAuthenticatedUser(true);
        //    _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

        //    // Act
        //    var result = await _controller.DataDescriptionTypeSubmit(true);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        //    var redirectResult = result as RedirectToActionResult;
        //    Assert.That(redirectResult.ActionName, Is.EqualTo("SecurityClassification"));
        //}

        //[Test]
        //public async Task DataDescriptionTypeSubmit_FalseConfirmDataDescription_ReturnsViewWithModelError()
        //{
        //    // Arrange
        //    SetAuthenticatedUser(true);
        //    _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

        //    // Act
        //    var result = await _controller.DataDescriptionTypeSubmit(false);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<ViewResult>());
        //    var viewResult = result as ViewResult;
        //    Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/DataDescriptionType.cshtml"));
        //    Assert.That(_controller.ModelState.ContainsKey("confirmDataDescription"), Is.True);
        //}
        [Test]
        public async Task SecurityClassification_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("TestError", "Invalid Model State");
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();

            // Act
            var result = await _controller.SecurityClassification(request);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/400"));
        }

        [Test]
        public async Task SecurityClassification_WithIdentifier_SetsSecurityClassification()
        {
            // Arrange

            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();
            var identifier = Guid.NewGuid().ToString();
            var mockDataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret"} };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockDataAsset);

            // Act
            var result = await _controller.SecurityClassification(request);

            // Assert
            Assert.That(request.SecurityClassification.ToString(), Is.EqualTo(SecurityClassificationEnum.TopSecret.ToString()));
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        //[Test]
        //public async Task SecurityClassification_SetsViewBagProperties_Correctly()
        //{
        //    // Arrange
        //    SetAuthenticatedUser(true);
        //    _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
        //    var request = _fixture.Create<QuestionSecurityClassificationRequest>();
        //    var mockDataAsset = new GetCddoDataAssetResponse();

        //    _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
        //    .ReturnsAsync(mockDataAsset);

        //    // Act
        //    var result = await _controller.SecurityClassification(request);
        //    var viewResult = result as ViewResult;

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<ViewResult>());
        //    Assert.That(viewResult.ViewData["isCheckList"], Is.EqualTo(true));
        //    Assert.That(viewResult.ViewData["isCheckAnswers"], Is.EqualTo(true));
        //    Assert.That(viewResult.ViewData["isEditMode"], Is.EqualTo(true));
        //}
        [Test]
        public async Task SecurityClassificationSubmit_InvalidModelState_ReturnsView()
        {
            // Arrange
            var mockDataAsset = new GetCddoDataAssetResponse();

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(mockDataAsset);
            _controller.ModelState.AddModelError("SecurityClassification", "Error");
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();
            request.SecurityClassification = null;

            // Act
            var result = await _controller.SecurityClassificationSubmit(request);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/SecurityClassification.cshtml"));
        }

        [Test]
        public async Task SecurityClassificationSubmit_ValidRequestWithoutIdentifier_RedirectsToAddTitle()
        {
            // Arrange
            var mockDataAsset = new GetCddoDataAssetResponse();

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(mockDataAsset);
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();
            request.Identifier = null;
            request.SecurityClassification = (Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum?)SecurityClassificationEnum.Official;

            // Act
            var result = await _controller.SecurityClassificationSubmit(request);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo("AddTitle"));
        }

        [Test]
        public async Task SecurityClassificationSubmit_WithValidIdentifier_CallsUpdateSecurityClassification()
        {
            // Arrange
            var mockDataAsset = new GetCddoDataAssetResponse();

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(mockDataAsset);
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();
            request.Identifier = Guid.NewGuid().ToString();
            request.SecurityClassification = (Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum?)SecurityClassificationEnum.Official;
            var response = _fixture.Create<PatchProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateSecurityClassificationAsync(request, DataAssetType.DataSet))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.SecurityClassificationSubmit(request);

            // Assert
            _mockCatalogQuestionsService.Verify(s => s.UpdateSecurityClassificationAsync(request, DataAssetType.DataSet), Times.Once);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        }

        [Test]
        public async Task SecurityClassificationSubmit_UnauthorizedAccessException_ReturnsAccessDenied()
        {
            // Arrange
            var mockDataAsset = new GetCddoDataAssetResponse();

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(mockDataAsset);
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();
            request.Identifier = Guid.NewGuid().ToString();
            request.SecurityClassification = (Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum?)SecurityClassificationEnum.Official;

            _mockCatalogQuestionsService.Setup(s => s.UpdateSecurityClassificationAsync(request, DataAssetType.DataSet))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.SecurityClassificationSubmit(request);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }
        [Test]
        public async Task SecurityClassificationSubmit_ResponseIsNull_ReturnsView()
        {
            // Arrange
            var request = _fixture.Create<QuestionSecurityClassificationRequest>();
            request.Identifier = Guid.NewGuid().ToString();
            request.SecurityClassification = (Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum?)SecurityClassificationEnum.Official;

            _mockCatalogQuestionsService.Setup(s => s.UpdateSecurityClassificationAsync(request, DataAssetType.DataSet))
                .ReturnsAsync((PatchProfiledDataAssetResponse)null);

            // Act
            var result = await _controller.SecurityClassificationSubmit(request);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/SecurityClassification.cshtml"));
        }
        [Test]
        public async Task AddTitle_InvalidModelState_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Error");
            var request = _fixture.Create<QuestionTitleRequest>();

            // Act
            var result = await _controller.AddTitle(request, null, false, false, false, null);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/Title.cshtml"));
        }

        [Test]
        public async Task AddTitle_ValidIdentifier_SetsTitleFromDataAsset()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            var request = _fixture.Create<QuestionTitleRequest>();
            var identifier = Guid.NewGuid().ToString();
            var dataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { Title = "Sample Title"} };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddTitle(request, identifier, false, false, false, null);

            // Assert
            _mockCatalogDataService.Verify(s => s.GetDataAssetAsync(It.Is<Guid>(g => g.ToString() == identifier), default), Times.Once);
            Assert.That(request.Title, Is.EqualTo("Sample Title"));
        }

        [Test]
        public async Task AddTitle_ValidRequest_ReturnsSecureActionAsync()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            var request = _fixture.Create<QuestionTitleRequest>();

            // Act
            var result = await _controller.AddTitle(request, null, false, false, false, null);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        [Test]
        public async Task AddTitleSubmit_ModelStateInvalid_ReturnsViewWithError()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(), 
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionTitleRequest>();
            request.Title = string.Empty;
            _controller.ModelState.AddModelError("Title", "Error");

            // Act
            var result = await _controller.AddTitleSubmit(request, "false", "false", "false", "false", null);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/Title.cshtml"));
            Assert.That(_controller.ModelState.ContainsKey("Title"), Is.True);
        }

        [Test]
        public async Task AddTitleSubmit_UnauthorizedAccess_ReturnsAccessDeniedPage()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionTitleRequest>();
            _mockCatalogQuestionsService.Setup(s => s.UpdateTitleAsync(It.IsAny<QuestionTitleRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddTitleSubmit(request, "false", "false", "false", "false", null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task AddTitleSubmit_ResponseIsNull_ReturnsViewWithError()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionTitleRequest>();
            _mockCatalogQuestionsService.Setup(s => s.UpdateTitleAsync(It.IsAny<QuestionTitleRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync((PatchProfiledDataAssetResponse)null);

            // Act
            var result = await _controller.AddTitleSubmit(request, "false", "false", "false", "false", null);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataDescription/NewDescription/Manual/Title.cshtml"));
        }

        [Test]
        public async Task AddTitleSubmit_ValidData_RedirectsToAddDescription()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionTitleRequest>();
            var response = _fixture.Create<PatchProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateTitleAsync(It.IsAny<QuestionTitleRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddTitleSubmit(request, "false", "false", "false", "false", null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo("AddDescription"));
        }
        [Test]
        public async Task AddTitleSubmit_IdentifierIsEmpty_CreatesNewDataAsset()
        {
            // Arrange
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionTitleRequest>();
            request.Identifier = string.Empty;
            var response = _fixture.Create<AddProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.CreateProfiledDataAssetTitleAsync(It.IsAny<QuestionFirstCreationRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.AddTitleSubmit(request, "false", "false", "false", "false", (Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums.SecurityClassificationEnum?)SecurityClassification.Official);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo("AddDescription"));
        }
        [Test]
        public async Task AddDescription_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            _controller.ModelState.AddModelError("Description", "Error");
            var request = _fixture.Create<QuestionDescriptionRequest>();

            // Act
            var result = await _controller.AddDescription(request, null, "false", "false", "false");

            // Assert
            _mockLogger.Verify(l => l.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public async Task AddDescription_ValidIdentifier_SetsDescriptionFromDataAsset()
        {
            // Arrange
            var request = _fixture.Create<QuestionDescriptionRequest>();
            var identifier = Guid.NewGuid().ToString();
            var dataAsset = new GetCddoDataAssetResponse() { CddoDataAsset =new CddoDataAsset() { Description = "Sample Description" } };
            dataAsset.CddoDataAsset.Description = "Sample Description";

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddDescription(request, identifier, "false", "false", "false");

            // Assert
            _mockCatalogDataService.Verify(s => s.GetDataAssetAsync(It.Is<Guid>(g => g.ToString() == identifier), default), Times.Once);
            Assert.That(request.Description, Is.EqualTo("Sample Description"));
        }

        [Test]
        public async Task AddDescription_ValidRequest_ReturnsSecureActionAsync()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionDescriptionRequest>();

            // Act
            var result = await _controller.AddDescription(request, null, "false", "false", "false");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task AddDescriptionSubmit_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            _controller.ModelState.AddModelError("Description", "Error");
            var request = _fixture.Create<QuestionDescriptionRequest>();

            // Act
            var result = await _controller.AddDescriptionSubmit(request, "false", "false", "false", "false");

            // Assert
            _mockLogger.Verify(l => l.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public async Task AddDescriptionSubmit_ValidRequest_CallsUpdateDescription()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionDescriptionRequest>();
            var response = _fixture.Create<PatchProfiledDataAssetResponse>();
            _mockCatalogQuestionsService.Setup(s => s.UpdateDescriptionAsync(It.IsAny<QuestionDescriptionRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddDescriptionSubmit(request, "false", "false", "false", "false");

            // Assert
            _mockCatalogQuestionsService.Verify(s => s.UpdateDescriptionAsync(request, DataAssetType.DataSet), Times.Once);
        }

        [Test]
        public async Task AddDescriptionSubmit_ResponseIsNull_ReturnsViewOrRedirect()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionDescriptionRequest>();
            _mockCatalogQuestionsService.Setup(s => s.UpdateDescriptionAsync(It.IsAny<QuestionDescriptionRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync((PatchProfiledDataAssetResponse?)null);

            // Act
            var result = await _controller.AddDescriptionSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.InstanceOf<IActionResult>());
        }
        [Test]
        public async Task AddDescriptionSubmit_UnauthorizedAccessException_ReturnsAccessDeniedPage()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionDescriptionRequest>();
            _mockCatalogQuestionsService.Setup(s => s.UpdateDescriptionAsync(It.IsAny<QuestionDescriptionRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddDescriptionSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }
        [Test]
        public async Task AddInternalIdentifier_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            _controller.ModelState.AddModelError("Identifier", "Error");
            var request = _fixture.Create<QuestionSupplierIdentifierRequest>();

            // Act
            var result = await _controller.AddInternalIdentifier(request, null, "false", "false", "false");

            // Assert
            _mockLogger.Verify(l => l.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public async Task AddInternalIdentifier_ValidIdentifier_SetsSupplierIdentifierFromDataAsset()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionSupplierIdentifierRequest>();
            var identifier = Guid.NewGuid().ToString();
            var dataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret",InternalIdentifier = "Sample Identifier" } };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddInternalIdentifier(request, identifier, "false", "false", "false");

            // Assert
            _mockCatalogDataService.Verify(s => s.GetDataAssetAsync(It.Is<Guid>(g => g.ToString() == identifier), default), Times.Once);
            Assert.That(request.SupplierIdentifier, Is.EqualTo("Sample Identifier"));
        }

        [Test]
        public async Task AddInternalIdentifier_ValidRequest_ReturnsSecureActionAsync()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionSupplierIdentifierRequest>();

            // Act
            var result = await _controller.AddInternalIdentifier(request, null, "false", "false", "false");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        [Test]
        public async Task AddInternalIdentifierSubmit_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            _controller.ModelState.AddModelError("Identifier", "Error");
            var request = _fixture.Create<QuestionSupplierIdentifierRequest>();

            // Act
            var result = await _controller.AddInternalIdentifierSubmit(request, "false", "false", "false", "false");

            // Assert
            _mockLogger.Verify(l => l.LogEvent(EventTypes.MetadataEvent.MetadataEdited, It.IsAny<Dictionary<string, string>>(), null), Times.Once);
        }

        [Test]
        public async Task AddInternalIdentifierSubmit_UnauthorizedAccessException_ReturnsAccessDeniedPage()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionSupplierIdentifierRequest>();
            _mockCatalogQuestionsService.Setup(s => s.UpdateIdentifierAsync(It.IsAny<QuestionSupplierIdentifierRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddInternalIdentifierSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task AddInternalIdentifierSubmit_ValidRequest_UpdatesIdentifierAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = _fixture.Create<QuestionSupplierIdentifierRequest>();
            var response = _fixture.Create<PatchProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateIdentifierAsync(It.IsAny<QuestionSupplierIdentifierRequest>(), DataAssetType.DataSet))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddInternalIdentifierSubmit(request, "false", "false", "false", "false");

            // Assert
            _mockCatalogQuestionsService.Verify(s => s.UpdateIdentifierAsync(It.IsAny<QuestionSupplierIdentifierRequest>(), DataAssetType.DataSet), Times.Once);
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddThemes_ValidIdentifier_FetchesThemesAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = new Guid().ToString();
            var request = new QuestionThemeRequest();
            var dataAsset = new GetCddoDataAssetResponse() 
            { 
                CddoDataAsset = new CddoDataAsset() 
                { 
                    SecurityClassification = "TopSecret", 
                    Themes = new List<string>() 
                    { 
                        "Agriculture, fisheries and forestry" 
                    } 
                } 
             };
            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddThemes(request, identifier, "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(request.Theme, Is.Not.Null);
            Assert.That(request.Theme.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task AddThemes_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionThemeRequest();
            _controller.ModelState.AddModelError("Theme", "Theme is required");

            // Act
            var result = await _controller.AddThemes(request, "", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddThemesSubmit_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionThemeRequest { Theme = new List<string>() { "TransportAndInfrastructure" } };
            _controller.ModelState.AddModelError("Theme", "Theme is required");

            // Act
            var result = await _controller.AddThemesSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddThemesSubmit_ValidRequest_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionThemeRequest { Theme = new List<string> { "AgricultureFisheriesAndForestry" } };
            _mockCatalogQuestionsService.Setup(s => s.UpdateThemesAsync(It.IsAny<QuestionThemeRequest>(), DataAssetType.DataSet))
                .ReturnsAsync(new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() });

            // Act
            var result = await _controller.AddThemesSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }

        [Test]
        public async Task AddThemesSubmit_UnauthorizedAccess_ThrowsException()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionThemeRequest { Theme = new List<string> { "AgricultureFisheriesAndForestry" } };
            _mockCatalogQuestionsService.Setup(s => s.UpdateThemesAsync(It.IsAny<QuestionThemeRequest>(), DataAssetType.DataSet))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddThemesSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        }
        [Test]
        public async Task AddKeywords_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string>() };
            _controller.ModelState.AddModelError("Keyword", "Keyword is required");

            // Act
            var result = await _controller.AddKeywords(request, null, "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddKeywords_ValidRequest_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string> { "data" } };
            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } });

            // Act
            var result = await _controller.AddKeywords(request, Guid.NewGuid().ToString(), "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddKeywordsSubmit_ValidRequest_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string> { "valid" } };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            _mockCatalogQuestionsService.Setup(s => s.UpdateKeywordsAsync(It.IsAny<QuestionKeywordRequest>(), DataAssetType.DataSet))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddKeywordsSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddKeywordsSubmit_InvalidKeywords_ReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string> { "1" } };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            _mockCatalogQuestionsService.Setup(s => s.UpdateKeywordsAsync(It.IsAny<QuestionKeywordRequest>(), DataAssetType.DataSet))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddKeywordsSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddKeywordsSubmit_CatalogReturnsNull_ReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string> { "111" } };

            _mockCatalogQuestionsService.Setup(s => s.UpdateKeywordsAsync(It.IsAny<QuestionKeywordRequest>(), DataAssetType.DataSet))
                .ReturnsAsync((PatchProfiledDataAssetResponse)null);

            // Act
            var result = await _controller.AddKeywordsSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddKeywordsSubmit_KeywordsNull_ReturnsRedirect()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string> {  } };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            _mockCatalogQuestionsService.Setup(s => s.UpdateKeywordsAsync(It.IsAny<QuestionKeywordRequest>(), DataAssetType.DataSet))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddKeywordsSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }

        [Test]
        public async Task AddKeywordsSubmit_UnauthorizedAccessException_ReturnsAccessDenied()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionKeywordRequest { Keyword = new List<string> { "valid" } };
            _mockCatalogQuestionsService.Setup(s => s.UpdateKeywordsAsync(It.IsAny<QuestionKeywordRequest>(), DataAssetType.DataSet))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddKeywordsSubmit(request, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        }
        [Test]
        public async Task AddContactPoint_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionContactPointRequest();
            _controller.ModelState.AddModelError("ContactPoint", "Invalid contact point");

            // Act
            var result = await _controller.AddContactPoint(request, new Guid().ToString(), "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddContactPoint_ValidRequest_CallsServiceAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionContactPointRequest { ContactPoint = new List<Contact> { new Contact { Name = "John Doe", Email = "john.doe@example.com", Role = ContactRoleEnum.Owner } } };
            var dataAsset =  new GetCddoDataAssetResponse() 
            { 
                CddoDataAsset = new CddoDataAsset() 
                {
                    SecurityClassification = "TopSecret",
                    DataAssetContacts = _fixture.Create<List<CddoDataAssetContact>>()
                } 
            }; 

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddContactPoint(request, new Guid().ToString(), "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddContactPointSubmit_InvalidModelState_ReturnsView()
        {
            // Arrange
            var contact = new Contact { Name = "", Email = "" };
            _controller.ModelState.AddModelError("Contact", "Invalid contact details");

            // Act
            var result = await _controller.AddContactPointSubmit(contact, "identifier", "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddContactPointSubmit_ValidRequest_CallsUpdateAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var contact = new Contact { Name = "John Doe", Email = "johndoeexampleom", Role = ContactRoleEnum.Contact };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            var mockDataAsset = new GetCddoDataAssetResponse() 
            { 
                CddoDataAsset = new CddoDataAsset() 
                { 
                    SecurityClassification = "TopSecret",
                    DataAssetContacts =  new List<CddoDataAssetContact>() { new CddoDataAssetContact() { Email = "test", Name = "test", Role = DataAssetContactRoleType.Owner } }
                } 
            };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockDataAsset);


            _mockCatalogQuestionsService.Setup(s => s.UpdateContactPointAsync(It.IsAny<QuestionContactPointRequest>(), DataAssetType.DataSet))
                .ReturnsAsync(response);

            _mockEmailValidator.Setup(x => x.CddoEmailAddressRegex).Returns(new System.Text.RegularExpressions.Regex(".*"));

            // Act
            var result = await _controller.AddContactPointSubmit(contact, new Guid().ToString(), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddContactPointSubmit_ValidRequest_CallsUpdateAndThrowsException()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var contact = new Contact { Name = "John Doe", Email = "john.doe@example.com", Role = ContactRoleEnum.Contact };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            var mockDataAsset = new GetCddoDataAssetResponse() 
            { 
                CddoDataAsset = new CddoDataAsset() 
                { 
                    SecurityClassification = "TopSecret",
                    DataAssetContacts =  new List<CddoDataAssetContact>() { new CddoDataAssetContact() { Email = "test", Name = "test", Role = DataAssetContactRoleType.Owner } }
                } 
            };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockDataAsset);


            _mockCatalogQuestionsService.Setup(s => s.UpdateContactPointAsync(It.IsAny<QuestionContactPointRequest>(), DataAssetType.DataSet))
                .ThrowsAsync(new UnauthorizedAccessException());

            _mockEmailValidator.Setup(x => x.CddoEmailAddressRegex).Returns(new System.Text.RegularExpressions.Regex(".*"));


            // Act
            var result = await _controller.AddContactPointSubmit(contact, new Guid().ToString(), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddDataOwner_InvalidModelState_LogsAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionContactPointRequest();
            _controller.ModelState.AddModelError("Contact", "Invalid contact details");

            // Act
            var result = await _controller.AddDataOwner(request, new Guid().ToString(), "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddDataOwner_ValidRequest_ReturnsSecureActionResult()
        {
            // Arrange
            var request = new QuestionContactPointRequest { Identifier = "identifier" };
            var dataAsset = new GetCddoDataAssetResponse()
            {
                CddoDataAsset = new CddoDataAsset()
                {
                    SecurityClassification = "TopSecret",
                    DataAssetContacts = new List<CddoDataAssetContact>() { new CddoDataAssetContact() { Email = "test", Name = "test", Role = DataAssetContactRoleType.Owner } }
                }
            };
            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddDataOwner(request, new Guid().ToString(), "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddDataOwnerSubmit_InvalidModelState_ReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var contact = new Contact { Name = "", Email = "" };
            _controller.ModelState.AddModelError("Contact", "Invalid contact details");
            _mockLogger.Setup(x => x.LogEvent(It.IsAny<MetadataEvent>(), It.IsAny<Dictionary<string, string>>(), null));

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync((GetCddoDataAssetResponse)null);


            // Act
            var result = await _controller.AddDataOwnerSubmit(contact, new Guid().ToString(), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddDataOwnerSubmit_ValidRequest_UpdatesContactPointAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var contact = new Contact { Name = "John Doe", Email = "john.doe@example.com", Role = ContactRoleEnum.Owner };
            var response = new PatchProfiledDataAssetResponse() { DataAssetId = Guid.NewGuid() };

            var dataAsset = new GetCddoDataAssetResponse()
            {
                CddoDataAsset = new CddoDataAsset()
                {
                    SecurityClassification = "TopSecret",
                    DataAssetContacts = new List<CddoDataAssetContact>() { new CddoDataAssetContact() { Email = "test", Name = "test", Role = DataAssetContactRoleType.Contact } }
                }
            };
            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            _mockCatalogQuestionsService.Setup(s => s.UpdateContactPointAsync(It.IsAny<QuestionContactPointRequest>(), DataAssetType.DataSet)).ReturnsAsync(response);

            // Act
            var result = await _controller.AddDataOwnerSubmit(contact, new Guid().ToString(), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddDataOwnerSubmit_ValidRequest_UpdatesContactReturnsNull()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var contact = new Contact { Name = "John Doe", Email = "john.doe@example.com", Role = ContactRoleEnum.Owner };
            var response = new PatchProfiledDataAssetResponse() { DataAssetId = Guid.NewGuid() };

            var dataAsset = new GetCddoDataAssetResponse()
            {
                CddoDataAsset = new CddoDataAsset()
                {
                    SecurityClassification = "TopSecret",
                    DataAssetContacts = new List<CddoDataAssetContact>() { new CddoDataAssetContact() { Email = "test", Name = "test", Role = DataAssetContactRoleType.Contact } }
                }
            };
            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            _mockCatalogQuestionsService.Setup(s => s.UpdateContactPointAsync(It.IsAny<QuestionContactPointRequest>(), DataAssetType.DataSet)).ReturnsAsync((PatchProfiledDataAssetResponse)null);

            // Act
            var result = await _controller.AddDataOwnerSubmit(contact, new Guid().ToString(), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddPublishedDate_InvalidModelState_LogsAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var questionIssuedRequest = new QuestionIssuedRequestModel();
            _controller.ModelState.AddModelError("IssuedDate", "Invalid date");

            // Act
            var result = await _controller.AddPublishedDate(questionIssuedRequest, "", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddPublishedDate_ValidIdentifier_FetchesDataAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionIssuedRequest = new QuestionIssuedRequestModel() { Identifier = identifier };
            var dataAsset = new GetCddoDataAssetResponse() 
            { 
                CddoDataAsset = new CddoDataAsset() 
                { 
                    SecurityClassification = "TopSecret" ,
                    Issued = DateTime.UtcNow
                } 
            };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddPublishedDate(questionIssuedRequest, identifier, "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddPublishedDateSubmit_ValidDate_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionIssuedRequest = new QuestionIssuedRequestModel()
            {
                metadataIssuedDay = "1",
                metadataIssuedMonth = "1",
                metadataIssuedYear = "2024",
                Identifier = identifier
            };

            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };
            _mockCatalogQuestionsService.Setup(s => s.UpdateIssuedAsync(It.IsAny<QuestionIssuedRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddPublishedDateSubmit(questionIssuedRequest, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddPublishedDateSubmit_InvalidDate_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionIssuedRequest = new QuestionIssuedRequestModel()
            {
                metadataIssuedDay = "0",
                metadataIssuedMonth = "0",
                metadataIssuedYear = "0",
                Identifier = identifier
            };

            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };
            _mockCatalogQuestionsService.Setup(s => s.UpdateIssuedAsync(It.IsAny<QuestionIssuedRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddPublishedDateSubmit(questionIssuedRequest, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddPublishedDateSubmit_InvalidDate_AndModelError_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            _controller.ModelState.AddModelError("IssuedDate", "Invalid date");

            var identifier = Guid.NewGuid().ToString();
            var questionIssuedRequest = new QuestionIssuedRequestModel()
            {
                metadataIssuedDay = "0",
                metadataIssuedMonth = "0",
                metadataIssuedYear = "0",
                Identifier = identifier
            };

            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };
            _mockCatalogQuestionsService.Setup(s => s.UpdateIssuedAsync(It.IsAny<QuestionIssuedRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddPublishedDateSubmit(questionIssuedRequest, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddPublishedDateSubmit_DateInFuture_ReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionIssuedRequest = new QuestionIssuedRequestModel()
            {
                metadataIssuedDay = "1",
                metadataIssuedMonth = "1",
                metadataIssuedYear = "2026",
                Identifier = identifier
            };

            _mockCatalogQuestionsService.Setup(s => s.UpdateIssuedAsync(It.IsAny<QuestionIssuedRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddPublishedDateSubmit(questionIssuedRequest, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddPublishedDateSubmit_ValidDate_CallsServiceAndThrowsException()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionIssuedRequest = new QuestionIssuedRequestModel
            {
                metadataIssuedDay = "1",
                metadataIssuedMonth = "1",
                metadataIssuedYear = "2024",
                Identifier = identifier
            };

            _mockCatalogQuestionsService.Setup(s => s.UpdateIssuedAsync(It.IsAny<QuestionIssuedRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddPublishedDateSubmit(questionIssuedRequest, "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        }

        [Test]
        public async Task AddFrequency_InvalidModelState_LogsAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            var identifier = Guid.NewGuid().ToString();

            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var questionUpdateFrequencyRequest = new QuestionUpdateFrequencyRequest();
            _controller.ModelState.AddModelError("UpdateFrequency", "Invalid frequency");

            // Act
            var result = await _controller.AddFrequency(questionUpdateFrequencyRequest, identifier, "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddFrequency_ValidIdentifier_FetchesDataAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionUpdateFrequencyRequest = new QuestionUpdateFrequencyRequest { Identifier = identifier };
            var dataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret", UpdateFrequencyString = "Monthly" } };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddFrequency(questionUpdateFrequencyRequest, identifier, "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddFrequencySubmit_InvalidModelState_LogsAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var questionUpdateFrequencyRequest = new QuestionUpdateFrequencyRequest { UpdateFrequency = "Invalid" };
            _controller.ModelState.AddModelError("UpdateFrequency", "Invalid frequency");
            _mockLogger.Setup(x => x.LogEvent(It.IsAny<MetadataEvent>(), It.IsAny<Dictionary<string, string>>(), null));

            _mockCatalogQuestionsService.Setup(s => s.UpdateUpdateFrequencyAsync(It.IsAny<QuestionUpdateFrequencyRequest>(), It.IsAny<DataAssetType>()))
            .ReturnsAsync((PatchProfiledDataAssetResponse)null);
            // Act
            var result = await _controller.AddFrequencySubmit(questionUpdateFrequencyRequest, "", "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddFrequencySubmit_ValidFrequency_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionUpdateFrequencyRequest = new QuestionUpdateFrequencyRequest { UpdateFrequency = "Weekly", Identifier = identifier };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            _mockCatalogQuestionsService.Setup(s => s.UpdateUpdateFrequencyAsync(It.IsAny<QuestionUpdateFrequencyRequest>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.AddFrequencySubmit(questionUpdateFrequencyRequest, "", "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }
        [Test]
        public async Task AddFrequencySubmit_ValidFrequency_CallsServiceAndThrowsException()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var questionUpdateFrequencyRequest = new QuestionUpdateFrequencyRequest { UpdateFrequency = "Other", Identifier = identifier };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };

            _mockCatalogQuestionsService.Setup(s => s.UpdateUpdateFrequencyAsync(It.IsAny<QuestionUpdateFrequencyRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddFrequencySubmit(questionUpdateFrequencyRequest, "", "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        }
        [Test]
        public async Task AddSupplyFormat_InvalidModelState_LogsErrorAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionDistributionRequest();
            _controller.ModelState.AddModelError("Distribution", "Invalid distribution");

            // Act
            var result = await _controller.AddSupplyFormat(request, "", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddSupplyFormat_ValidIdentifier_FetchesDataAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var request = new QuestionDistributionRequest { Identifier = identifier };
            var dataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret", DataAssetDistribution = new List<CddoDataAssetDistribution>() { new CddoDataAssetDistribution() { MediaType = "application/json" } } } };
   
            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAsset);

            // Act
            var result = await _controller.AddSupplyFormat(request, identifier, "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task AddSupplyFormatSubmit_InvalidModelState_LogsErrorAndReturnsView()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionDistributionRequest();
           var dist = new List<Distribution>();
            _controller.ModelState.AddModelError("Distribution", "Invalid distribution");

            // Act
            var result = await _controller.AddSupplyFormatSubmit(request, null, "[]", "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task AddSupplyFormatSubmit_ValidRequest_CallsServiceAndRedirects()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionDistributionRequest { Identifier = Guid.NewGuid().ToString() };
            var response = new PatchProfiledDataAssetResponse { DataAssetId = Guid.NewGuid() };
            var dist = _fixture.Create<List<Distribution>>();


            _mockCatalogQuestionsService.Setup(s => s.UpdateDistributionAsync(request, DataAssetType.DataSet)).ReturnsAsync(response);

            // Act
            var result = await _controller.AddSupplyFormatSubmit(request, "false", JsonConvert.SerializeObject(dist, Formatting.Indented), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }

        [Test]
        public async Task AddSupplyFormatSubmit_UnauthorizedAccessException_RedirectsToAccessDeniedPage()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var request = new QuestionDistributionRequest { Identifier = Guid.NewGuid().ToString() };
            var dist = _fixture.Create<List<Distribution>>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateDistributionAsync(request, DataAssetType.DataSet)).ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.AddSupplyFormatSubmit(request, "false", JsonConvert.SerializeObject(dist, Formatting.Indented), "false", "false", "false", "false");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
        }
        [Test]
        public async Task TaskList_InvalidModelState_LogsErrorAndReturnsView()
        {
            // Arrange 
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            _controller.ModelState.AddModelError("Identifier", "Invalid identifier");

            // Act and Assert
            Assert.That(() => _controller.TaskList(null, false), Throws.Exception.TypeOf<ArgumentNullException>());
            _mockLogger.Verify(logger => logger.LogEvent(EventTypes.MetadataEvent.MetadataAccessDenied, It.IsAny<Dictionary<string, string>>(), null), Times.Once);
        }

        [Test]
        public async Task TaskList_ValidRequest_ReturnsViewWithData()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockUserRoleService.Setup(x => x.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            _mockLogger.Setup(x => x.LogAdminEventBase(It.IsAny<AdminAuditEvent>(), It.IsAny<string>(), It.IsAny<string>(),
                                                       It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
            var identifier = Guid.NewGuid().ToString();
            var dataAssetResponse = new  GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };
            var duplicateResponse = new CheckForPotentialDuplicatesToDataAssetResponse() 
            { 
                PotentialDuplicatesToDataAsset = _fixture.Create<List<PotentialDuplicateDataAssetInformation>>() 
            };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAssetResponse);
            _mockCatalogDataService.Setup(s => s.CheckForPotentialDuplicatesToDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(duplicateResponse);

            // Act
            var result = await _controller.TaskList(identifier, false);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task CheckAnswers_InvalidModelState_LogsErrorAndReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Identifier", "Invalid identifier");

            // Act and Assert
            Assert.That(() =>  _controller.CheckAnswers(null), Throws.Exception.TypeOf<ArgumentNullException>());
            _mockLogger.Verify(logger => logger.LogEvent(EventTypes.MetadataEvent.MetadataAccessDenied, It.IsAny<Dictionary<string, string>>(), null), Times.Once);
        }

        [Test]
        public async Task CheckAnswers_ValidRequest_ReturnsViewWithData()
        {
            // Arrange
            var identifier = Guid.NewGuid().ToString();
            var dataAssetResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } }; ;
            var validationErrorsResponse = new GetCddoDataAssetValidationErrorsResponse { PropertyValidationErrors = new List<DataAssetValidationPropertyResult>() };
            var duplicateResponse = new CheckForPotentialDuplicatesToDataAssetResponse()
            {
                PotentialDuplicatesToDataAsset = _fixture.Create<List<PotentialDuplicateDataAssetInformation>>()
            };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(dataAssetResponse);
            _mockCatalogDataService.Setup(s => s.GetDataAssetValidationErrorsAsync(It.IsAny<Guid>(), default)).ReturnsAsync(validationErrorsResponse);
            _mockCatalogDataService.Setup(s => s.CheckForPotentialDuplicatesToDataAssetAsync(It.IsAny<Guid>(), default)).ReturnsAsync(duplicateResponse);

            // Act
            var result = await _controller.CheckAnswers(identifier);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
    }
}
