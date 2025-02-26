using Agm.Catalog.DotNet.Core.Utilities;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V1_0;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Pages.DataDescription;
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
    public class DataDescriptionControllerTests
    {
        private Mock<ICatalogDataService> _mockCatalogDataService;
        private Mock<ICatalogQuestionsService> _mockCatalogQuestionsService;
        private Mock<IUserRoleService> _mockUserRoleService;
        private Mock<IAppInsightsLogger> _mockAppInsightLogger;
        private Mock<IUserProfilePresenter> _mockUserProfilePresenter;
        private Mock<IDataShareRequestMailboxAddressValidation> _mockMailboxValidation;
        private Mock<ILogger<DataDescriptionController>> _mockLogger;
        private IFixture _fixture;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private DataDescriptionController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method


        public DataDescriptionControllerTests()
        {
            _fixture = new Fixture();
            _mockCatalogDataService = new Mock<ICatalogDataService>();
            _mockCatalogQuestionsService = new Mock<ICatalogQuestionsService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockAppInsightLogger = new Mock<IAppInsightsLogger>();
            _mockUserProfilePresenter = new Mock<IUserProfilePresenter>();
            _mockMailboxValidation = new Mock<IDataShareRequestMailboxAddressValidation>();
            _mockLogger = new Mock<ILogger<DataDescriptionController>>();

            _controller = new DataDescriptionController(
                _mockLogger.Object,
                _mockCatalogDataService.Object,
                _mockCatalogQuestionsService.Object,
                _mockUserRoleService.Object,
                Mock.Of<IEnumMemberConverter>(),
                _mockAppInsightLogger.Object,
                _mockUserProfilePresenter.Object,
                _mockMailboxValidation.Object);

        }

        private void ClearInvocations()
        {
            _mockLogger.Invocations.Clear();
            _mockCatalogDataService.Invocations.Clear();
            _mockCatalogQuestionsService.Invocations.Clear();
            _mockUserRoleService.Invocations.Clear();
            _mockAppInsightLogger.Invocations.Clear();
            _mockUserProfilePresenter.Invocations.Clear();
            _mockMailboxValidation.Invocations.Clear();
            _controller.ModelState.Clear();
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
        public async Task DataShareRequestNotificationsSelection_ReturnsViewResult_WithValidModel()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            var identifier = Guid.NewGuid().ToString();
            var recipientType = (DataShareRequestNotificationRecipientType?)null;
            var enteredCustomAddress = "test@example.com";
            var domainInfo = new Mock<IDomainInformation>();
            var dataAssetResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };
            domainInfo.Setup(d => d.DataShareRequestMailboxAddress).Returns(enteredCustomAddress);

            _mockUserProfilePresenter.Setup(p => p.GetDomainInformationOfInitiatingUserAsync())
                .ReturnsAsync(domainInfo.Object);

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(dataAssetResponse);

            // Act
            var result = await _controller.DataShareRequestNotificationsSelection(identifier, recipientType, enteredCustomAddress);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.InstanceOf<DataShareRequestNotificationsSelectionRequest>());
        }

        [Test]
        public async Task DataShareRequestNotificationsSelection_LogsWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(true);
            ClearInvocations();
            _controller.ModelState.AddModelError("Error", "Invalid Model State");

            var identifier = Guid.NewGuid().ToString();
            var recipientType = (DataShareRequestNotificationRecipientType?)null;
            var enteredCustomAddress = "test@example.com";
            var domainInfo = new Mock<IDomainInformation>();
            var dataAssetResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } }; ;
            domainInfo.Setup(d => d.DataShareRequestMailboxAddress).Returns(enteredCustomAddress);

            _mockUserProfilePresenter.Setup(p => p.GetDomainInformationOfInitiatingUserAsync())
                .ReturnsAsync(domainInfo.Object);

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(dataAssetResponse);

            // Act
            var result = await _controller.DataShareRequestNotificationsSelection(identifier, recipientType, enteredCustomAddress);

            // Assert
            _mockAppInsightLogger.Verify(logger =>
                logger.LogWarning("Model state is invalid for DataShareRequestNotificationsSelection."), Times.Once);
            _controller.ModelState.Clear();

        }
        //[Test]
        //public async Task DataShareRequestNotificationsSelectionSubmit_ReturnsViewResult_WhenModelStateIsInvalid()
        //{
        //    ClearInvocations();

        //    SetAuthenticatedUser(true);
        //    var request = _fixture.Create<DataShareRequestNotificationsRequest>();
        //    var dataAssetType = _fixture.Create<DataAssetType>();
        //    _controller.ModelState.AddModelError("Error", "Invalid Model State");

        //    var result = await _controller.DataShareRequestNotificationsSelectionSubmit(request, dataAssetType);

        //    Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        //    _mockAppInsightLogger.Verify(logger =>
        //        logger.LogWarning("Model state is invalid for DataShareRequestNotificationsSelectionSubmit."), Times.Once);
        //    _controller.ModelState.Clear();
        //}

        //[Test]
        //public async Task DataShareRequestNotificationsSelectionSubmit_RedirectsToAccessDenied_OnUnauthorizedAccessException()
        //{

        //    SetAuthenticatedUser(true);
        //    ClearInvocations();
        //    _controller.ModelState.Clear();

        //    var request = _fixture.Create<DataShareRequestNotificationsRequest>();
        //    var dataAssetType = _fixture.Create<DataAssetType>();

        //    _mockCatalogQuestionsService.Setup(s => s.UpdateDataShareRequestNotificationsSelectionAsync(It.IsAny<DataShareRequestNotificationsRequest>(), It.IsAny<DataAssetType>()))
        //        .ThrowsAsync(new UnauthorizedAccessException("test"));

        //    var result = await _controller.DataShareRequestNotificationsSelectionSubmit(request, dataAssetType);

        //    Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        //    var redirectResult = (RedirectToPageResult)result;
        //    Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        //}

        //[Test]
        //public async Task DataShareRequestNotificationsSelectionSubmit_Redirects_When_ResultIsNull()
        //{
        //    ClearInvocations();

        //    SetAuthenticatedUser(true);
        //    var request2 = _fixture.Create<DataShareRequestNotificationsRequest>();
        //    var dataAssetType2 = _fixture.Create<DataAssetType>();

        //    _mockCatalogQuestionsService.Setup(s => s.UpdateDataShareRequestNotificationsSelectionAsync(request2, dataAssetType2))
        //        .ReturnsAsync((PatchProfiledDataAssetResponse?)null);

        //    var result = await _controller.DataShareRequestNotificationsSelectionSubmit(request2, dataAssetType2);

        //    Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        //    var redirectResult = (RedirectToActionResult)result;
        //    Assert.That(redirectResult.ActionName, Is.EqualTo(nameof(_controller.DataShareRequestNotificationsSelection)));
        //}

        [Test]
        public async Task DataShareRequestNotificationsSelectionSubmit_CallsPublishDataAssetSubmit_WhenResultIsNotNull()
        {

            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataShareRequestNotificationsRequest>();
            request.Identifier = new Guid().ToString();
            var dataAssetType = _fixture.Create<DataAssetType>();
            var mockResponse = _fixture.Create<PatchProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataShareRequestNotificationsSelectionAsync(request, dataAssetType))
                .ReturnsAsync(mockResponse);

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataAssetStatusAsync(It.IsAny<string>(), It.IsAny<DataAssetStatus>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(mockResponse);

            _mockUserRoleService.Setup(s => s.GetUserProfileAsync()).ReturnsAsync(_fixture.Create<UserProfile>());

            var result = await _controller.DataShareRequestNotificationsSelectionSubmit(request, dataAssetType);

            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("CheckAnswers"));
        }
        [Test]
        public async Task DataShareRequestNotificationsSelectionSubmit_ValidateReturnsFalse()
        {

            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataShareRequestNotificationsRequest>();
            request.Identifier = new Guid().ToString();
            request.SelectedDataShareRequestNotificationRecipientType = null;
            var dataAssetType = _fixture.Create<DataAssetType>();
            var mockResponse = _fixture.Create<PatchProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataShareRequestNotificationsSelectionAsync(request, dataAssetType))
                .ReturnsAsync(mockResponse);

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataAssetStatusAsync(It.IsAny<string>(), It.IsAny<DataAssetStatus>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(mockResponse);

            _mockUserRoleService.Setup(s => s.GetUserProfileAsync()).ReturnsAsync(_fixture.Create<UserProfile>());

            var result = await _controller.DataShareRequestNotificationsSelectionSubmit(request, dataAssetType);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName.Contains("DataShareRequestNotificationsSelection"));
        }
        [Test]
        public async Task DataShareRequestNotificationsSelectionSubmit_RecipientIs_EsdaCustomDsrNotificationAddress()
        {

            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataShareRequestNotificationsRequest>();
            request.CustomDsrNotificationAddress = "test@test.com";
            request.Identifier = new Guid().ToString();
            request.SelectedDataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress;
            var dataAssetType = _fixture.Create<DataAssetType>();
            var mockResponse = _fixture.Create<PatchProfiledDataAssetResponse>();

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataShareRequestNotificationsSelectionAsync(request, dataAssetType))
                .ReturnsAsync(mockResponse);

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataAssetStatusAsync(It.IsAny<string>(), It.IsAny<DataAssetStatus>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(mockResponse);

            _mockUserRoleService.Setup(s => s.GetUserProfileAsync()).ReturnsAsync(_fixture.Create<UserProfile>());

            var result = await _controller.DataShareRequestNotificationsSelectionSubmit(request, dataAssetType);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName.Contains("DataShareRequestNotificationsSelection"));
        }

        [Test]
        public async Task UpdateDataAssetStatusSubmit_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            _controller.ModelState.AddModelError("Error", "Invalid Model State");

            var result = await _controller.UpdateDataAssetStatusSubmit("test-identifier", DataAssetStatus.Published, DataAssetType.DataSet);

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task UpdateDataAssetStatusSubmit_RedirectsToAccessDenied_OnUnauthorizedAccessException()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            _mockCatalogQuestionsService.Setup(s => s.UpdateDataAssetStatusAsync(It.IsAny<string>(), It.IsAny<DataAssetStatus>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.UpdateDataAssetStatusSubmit("test-identifier", DataAssetStatus.Published, DataAssetType.DataSet);

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task UpdateDataAssetStatusSubmit_ReturnsViewResult_WithValidResponse()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            var mockResponse = _fixture.Create<PatchProfiledDataAssetResponse>();
            var mockDataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } }; ;
            mockResponse.DataAssetId = new Guid();

            _mockCatalogQuestionsService.Setup(s => s.UpdateDataAssetStatusAsync(It.IsAny<string>(), It.IsAny<DataAssetStatus>(), It.IsAny<DataAssetType>()))
                .ReturnsAsync(mockResponse);

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockDataAsset);

            _mockUserRoleService.Setup(s => s.GetUserProfileAsync()).ReturnsAsync(_fixture.Create<UserProfile>());

            var result = await _controller.UpdateDataAssetStatusSubmit("test-identifier", DataAssetStatus.Published, DataAssetType.DataSet);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(mockDataAsset.CddoDataAsset));
        }
        [Test]
        public async Task EditDataAssetManagementSettings_ReturnsRedirectToPageResult_WhenUserIsNotAuthenticated()
        {
            SetAuthenticatedUser(false);
            ClearInvocations();
            var result = await _controller.EditDataAssetManagementSettings(Guid.NewGuid());

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }
        [Test]
        public async Task EditDataAssetManagementSettings_ReturnsRedirectToPageResult_WhenModelError()
        {
            SetAuthenticatedUser(false);
            ClearInvocations();
            _controller.ModelState.AddModelError("error", "test error");
            var result = await _controller.EditDataAssetManagementSettings(Guid.NewGuid());

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task EditDataAssetManagementSettings_CallsDoShowEditDataAssetManagementSettings_WhenUserIsAuthenticated()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            var dataAssetId = Guid.NewGuid();
            var mockResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };
            var mockEsdaDomainInfo = new Mock<IDomainInformation>();

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(dataAssetId, default))
                .ReturnsAsync(mockResponse);

            _mockUserProfilePresenter.Setup(s => s.GetOrganisationDomainInformationAsync(
                    It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(mockEsdaDomainInfo.Object);

            var result = await _controller.EditDataAssetManagementSettings(dataAssetId);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.InstanceOf<EditDataAssetManagementSettingsModel>());
        }
        [Test]
        public async Task EditDataAssetManagementSettingsSubmit_ReturnsRedirectToPageResult_WhenUserIsNotAuthenticated()
        {
            SetAuthenticatedUser(false);
            ClearInvocations();
            var request = _fixture.Create<DataAssetManagementSettingsRequest>();
            var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task EditDataAssetManagementSettingsSubmit_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            SetAuthenticatedUser(false);
            ClearInvocations();
            _controller.ModelState.AddModelError("Error", "Invalid state");
            var request = _fixture.Create<DataAssetManagementSettingsRequest>();
            request.Identifier = new Guid().ToString();

            var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        //[Test]
        //public async Task EditDataAssetManagementSettingsSubmit_CallsUpdateDataShareRequestNotificationsSelectionAsync_WhenValid()
        //{
        //    SetAuthenticatedUser(true);
        //    ClearInvocations();
        //    var request = _fixture.Create<DataAssetManagementSettingsRequest>();
        //    request.Identifier = new Guid().ToString();
        //    var mockResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };

        //    _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
        //        .ReturnsAsync(mockResponse);

        //    var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

        //    _mockCatalogQuestionsService.Verify(s => s.UpdateDataShareRequestNotificationsSelectionAsync(
        //        It.IsAny<DataShareRequestNotificationsRequest>(), It.IsAny<DataAssetType>()), Times.Once);

        //    Assert.That(result, Is.InstanceOf<ViewResult>());
        //}
        [Test]
        public async Task EditDataAssetManagementSettingsSubmit_ReturnsViewResult_WhenRecipientTypeIsNull()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataAssetManagementSettingsRequest>();
            request.Identifier = new Guid().ToString();
            request.SelectedDataShareRequestNotificationRecipientType = null;
            var mockResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockResponse);
            var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

            Assert.That(result, Is.InstanceOf<ViewResult>());
            _mockCatalogQuestionsService.Verify(s => s.UpdateDataShareRequestNotificationsSelectionAsync(
                It.IsAny<DataShareRequestNotificationsRequest>(), It.IsAny<DataAssetType>()), Times.Never);
        }
        [Test]
        public async Task EditDataAssetManagementSettingsSubmit_ReturnsViewResult_WhenCustomAddressTooLong()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataAssetManagementSettingsRequest>();
            request.Identifier = new Guid().ToString();
            request.SelectedDataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress;
            request.CustomDsrNotificationAddress = new string('a', 256); // 256 characters (exceeds limit)
            var mockResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockResponse);
            var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        [Test]
        public async Task EditDataAssetManagementSettingsSubmit_ReturnsViewResult_WhenMailboxValidationFails()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataAssetManagementSettingsRequest>();
            request.Identifier = new Guid().ToString();
            request.SelectedDataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaCustomDsrNotificationAddress;
            request.CustomDsrNotificationAddress = "invalid-email";
            var mockResponse = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };

            _mockCatalogDataService.Setup(s => s.GetDataAssetAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(mockResponse);
            _mockMailboxValidation
                .Setup(v => v.TryValidateDataShareRequestMailboxAddress(It.IsAny<string>(), out It.Ref<string>.IsAny))
                .Returns(false);

            var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        [Test]
        public async Task EditDataAssetManagementSettingsSubmit_RedirectsToAccessDenied_WhenUnauthorizedAccessExceptionThrown()
        {
            SetAuthenticatedUser(true);
            ClearInvocations();
            var request = _fixture.Create<DataAssetManagementSettingsRequest>();
            request.Identifier = new Guid().ToString();

            _mockCatalogQuestionsService
                .Setup(s => s.UpdateDataShareRequestNotificationsSelectionAsync(
                    It.IsAny<DataShareRequestNotificationsRequest>(), It.IsAny<DataAssetType>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            var result = await _controller.EditDataAssetManagementSettingsSubmit(request);

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

    }
}
