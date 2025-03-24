using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V1_0;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Api.Dto.Requests.DataShareRequests;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.UI.Pages.Dataset;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Extensions.Logging;
using Moq;
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
    public class CatalogDataControllerTests
    {
        #region SetUp

        private Mock<ILogger<CatalogDataController>> _mockLogger;
        private Mock<ICatalogDataService> _mockCatalogDataService;
        private Mock<IUserRoleService> _mockUserRoleService;
        private Mock<IAppInsightsLogger> _mockAppInsightsLogger;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private CatalogDataController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private Fixture _fixture;

        public CatalogDataControllerTests()
        {
            _mockLogger = new Mock<ILogger<CatalogDataController>>();
            _mockCatalogDataService = new Mock<ICatalogDataService>();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockAppInsightsLogger = new Mock<IAppInsightsLogger>();
            _fixture = new Fixture();

            _controller = new CatalogDataController(
                _mockLogger.Object,
                _mockCatalogDataService.Object,
                _mockAppInsightsLogger.Object,
                _mockUserRoleService.Object
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

        public void ClearInvocations()
        {
            _mockCatalogDataService.Invocations.Clear();
            _mockAppInsightsLogger.Invocations.Clear();
            _mockUserRoleService.Invocations.Clear();
            _mockLogger.Invocations.Clear();
        }

        #endregion
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssets_ShouldReturnOkWithSuggestions()
        {
            // Arrange
            var searchText = "test";
            var expectedSuggestions = _fixture.CreateMany<string>(5).ToList();
            _mockCatalogDataService.Setup(s => s.GetSearchSuggestionsForPublishedDataAssetsAsync(searchText, default))
                .ReturnsAsync(expectedSuggestions);

            // Act
            var result = await _controller.GetSearchSuggestionsForPublishedDataAssets(searchText);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.EqualTo(expectedSuggestions));
        }

        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssets_ShouldReturnOkWithSuggestions()
        {
            // Arrange
            var searchText = "organisation test";
            var expectedSuggestions = _fixture.CreateMany<string>(5).ToList();
            _mockCatalogDataService.Setup(s => s.GetSearchSuggestionsForOrganisationDataAssetsAsync(searchText, default))
                .ReturnsAsync(expectedSuggestions);

            // Act
            var result = await _controller.GetSearchSuggestionsForOrganisationDataAssets(searchText);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.EqualTo(expectedSuggestions));
        }

        [Test]
        public async Task StartCddoDataAssetsSearch_ShouldLogWarning_WhenModelStateIsInvalid_AndReturnUnauthorisedWHenUserNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            _mockLogger.Setup(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ));


            // Act
            var result = await _controller.GetCddoDataAssets(null, new List<string>(), new List<string>(), new List<DataAssetType>(), null, null, null);

            // Assert
            _mockLogger.Verify(
                log => log.Log(
                    It.Is<LogLevel>(lvl => lvl == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Model state is invalid")),
                    It.IsAny<System.Exception>(),
                    It.IsAny<Func<It.IsAnyType, System.Exception, string>>()
                ), Times.Once);
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task StartCddoDataAssetsSearch_ShouldRedirectToGetCddoDataAssets_WithExpectedParameters()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            var searchText = "test search";
            var selectedTopics = _fixture.CreateMany<string>(3).ToList();
            var selectedOrganisations = _fixture.CreateMany<string>(2).ToList();
            var selectedDataAssetTypes = _fixture.CreateMany<DataAssetType>(2).ToList();
            int? selectedNumberOfRecordsToShow = 10;
            int? selectedPageNumber = 1;
            var expectedSortOption = "Relevance|Descending";

            // Act
            var result = await _controller.GetCddoDataAssets(
                searchText,
                selectedTopics,
                selectedOrganisations,
                selectedDataAssetTypes,
                selectedNumberOfRecordsToShow,
                selectedPageNumber,
                expectedSortOption
            );

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult?.ActionName, Is.EqualTo("GetCddoDataAssets"));
            Assert.That(redirectResult?.RouteValues?["searchText"], Is.EqualTo(searchText));
            Assert.That(redirectResult?.RouteValues?["selectedTopics"], Is.EqualTo(selectedTopics));
            Assert.That(redirectResult?.RouteValues?["selectedOrganisations"], Is.EqualTo(selectedOrganisations));
            Assert.That(redirectResult?.RouteValues?["selectedDataAssetTypes"], Is.EqualTo(selectedDataAssetTypes));
            Assert.That(redirectResult?.RouteValues?["selectedNumberOfRecordsToShow"], Is.EqualTo(selectedNumberOfRecordsToShow));
            Assert.That(redirectResult?.RouteValues?["selectedPageNumber"], Is.EqualTo(selectedPageNumber));
            Assert.That(redirectResult?.RouteValues?["sortOption"], Is.EqualTo(expectedSortOption));
        }
        [Test]
        public async Task GetCddoDataAssets_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            _controller.ModelState.AddModelError("Error", "Invalid model state");


            _mockLogger.Setup(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ));


            // Act
            var result = await _controller.GetCddoDataAssets(null, new List<string>(), new List<string>(), new List<DataAssetType>(), null, null, null);

            // Assert
            _mockLogger.Verify(
            log => log.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Model state is invalid")),
                It.IsAny<System.Exception>(),
                It.IsAny<Func<It.IsAnyType, System.Exception, string>>()
            ), Times.Once);
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task GetCddoDataAssets_ShouldReturnViewWithExpectedModel_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var orgList = new List<string>() { "testOrg", "test-org-2"};
            var mockDataAsset = new GetCddoDataAssetsResponse()
            {
                CddoDataAssets = new List<CddoDataAsset>() 
                {
                    new CddoDataAsset()
                    {
                        SecurityClassification = "test",
                        ServiceStatus = ServiceStatusEnum.PrivateBeta,
                        ServiceType = ServiceTypeEnum.Event,
                        AccessRights = "test",
                        Summary = "test",
                        AllowDSRRequest = true,
                        Themes = new List<string>(){ "test", "test", "test"}                        
                    }
                },
                TotalNumberOfMatchingCddoDataAssets = 1,               
                
            };
            var topics = _fixture.Create<List<string>>();

            _mockCatalogDataService.Setup(x => x.GetCddoOrganisationsAsync(It.IsAny<IEnumerable<DataAssetStatus>>(), default)).ReturnsAsync(orgList);

            _mockAppInsightsLogger.Setup(x => x.LogEvent(MetadataEvent.MetadataSearchPerformed, It.IsAny<Dictionary<string, string>>(), null));

            _mockCatalogDataService.Setup(x => x.GetDataAssetsAsync(It.IsAny<GetCddoDataAssetsRequest>(), default))
                .ReturnsAsync(mockDataAsset);

            _mockCatalogDataService.Setup(x => x.GetCddoTopicsAsync(null, default)).ReturnsAsync(topics);


            // Act
            var result = await _controller.GetCddoDataAssets("test", new List<string>() { "test "}, new List<string>() {  "test" }, new List<DataAssetType>(), 10, 1, "Relevance|Descending");


            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as DatasetResultsModel;

            Assert.That(model, Is.Not.Null);
            Assert.That(model?.DataAssets.Count, Is.EqualTo(1));
            Assert.That(model?.TotalNumberOfResults, Is.EqualTo(1));
        }
        [Test]
        public async Task GetCddoDataAssetsByUser_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);

            var request = _fixture.Create<GetCddoDataAssetsRequest>();

            // Act
            var result = await _controller.GetCddoDataAssetsByUser(request, null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task GetCddoDataAssetsByUser_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            _controller.ModelState.AddModelError("TestError", "Invalid model state");


            _mockLogger.Setup(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ));


            var request = _fixture.Create<GetCddoDataAssetsRequest>();

            // Act
            var result = await _controller.GetCddoDataAssetsByUser(request, null);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetCddoDataAssetsByUser_ShouldReturnViewWithExpectedModel_WhenUserIsAuthenticated()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var mockUser = new Mock<ClaimsPrincipal>();
            mockUser.Setup(u => u.Identity!.IsAuthenticated).Returns(true);
            httpContext.User = mockUser.Object;
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var request = _fixture.Create<GetCddoDataAssetsRequest>();
            var mockDataAsset = new GetCddoDataAssetsResponse()
            {
                CddoDataAssets = new List<CddoDataAsset>()
                {
                    new CddoDataAsset()
                    {
                        SecurityClassification = "test",
                        ServiceStatus = ServiceStatusEnum.PrivateBeta,
                        ServiceType = ServiceTypeEnum.Event,
                        AccessRights = "test",
                        Summary = "test",
                        AllowDSRRequest = true,
                        Themes = new List<string>(){ "test", "test", "test"}
                    }
                },
                TotalNumberOfMatchingCddoDataAssets = 1,

            };

            _mockCatalogDataService.Setup(service => service.GetDataAssetsByUserAsync(request, default))
                .ReturnsAsync(mockDataAsset);

            _mockLogger.Setup(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ));
            _mockAppInsightsLogger.Setup(x => x.LogEvent(MetadataEvent.MetadataSearchPerformed, It.IsAny<Dictionary<string, string>>(), null));


            // Act
            var result = await _controller.GetCddoDataAssetsByUser(request, "Relevance|Descending");

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.Model, Is.EqualTo(mockDataAsset));
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldRedirectToError400_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("TestError", "Invalid model state");

            var dataAssetId = Guid.NewGuid();

            // Act
            var result = await _controller.GetCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/400"));
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);

            var dataAssetId = Guid.NewGuid();

            // Act
            var result = await _controller.GetCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldRedirectToError404_WhenDataAssetNotFound()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            var dataAssetId = Guid.NewGuid();
            _mockCatalogDataService.Setup(service => service.GetDataAssetAsync(dataAssetId, default)).ReturnsAsync((GetCddoDataAssetResponse)null);

            // Act
            var result = await _controller.GetCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/404"));
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldReturnViewWithExpectedModel_WhenDataAssetExists()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            var dataAssetId = Guid.NewGuid();
            var mockDataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };

            _mockCatalogDataService.Setup(service => service.GetDataAssetAsync(dataAssetId, default)).ReturnsAsync(mockDataAsset);
            _mockAppInsightsLogger.Setup(x => x.LogEvent(MetadataEvent.MetadataViewed, It.IsAny<Dictionary<string, string>>(), null));


            // Act
            var result = await _controller.GetCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.Model, Is.EqualTo(mockDataAsset));
        }
        [Test]
        public async Task StartDataShareRequest_ShouldReturnError400View_WhenDataAssetIdIsEmpty()
        {
            // Arrange
            var dataAssetId = Guid.Empty;

            // Act
            var result = await _controller.StartDataShareRequest(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.ViewName, Is.EqualTo("~/Pages/Error/400.cshtml"));
        }

        [Test]
        public async Task StartDataShareRequest_ShouldReturnViewWithExpectedModel_WhenDataAssetExists()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var dataAssetId = Guid.NewGuid();
            var mockDataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };
            _mockCatalogDataService.Setup(service => service.GetDataAssetAsync(dataAssetId, default)).ReturnsAsync(mockDataAsset);

            // Act
            var result = await _controller.StartDataShareRequest(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.Model, Is.InstanceOf<DataShareRequest>());
            var model = viewResult?.Model as DataShareRequest;
            Assert.That(model?.DatasetId, Is.EqualTo(dataAssetId));
            Assert.That(model?.Title, Is.EqualTo(mockDataAsset.CddoDataAsset.Title));
        }

        [Test]
        public async Task StartDataShareRequest_ShouldLogUserEvent_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            var dataAssetId = Guid.NewGuid();
            var mockDataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };
            _mockCatalogDataService.Setup(service => service.GetDataAssetAsync(dataAssetId, default)).ReturnsAsync(mockDataAsset);
            _mockUserRoleService.Setup(service => service.GetUserProfileAsync()).ReturnsAsync(new UserProfile());
            _mockAppInsightsLogger.Setup(x => x.LogEventMainBase(It.IsAny<UserEvent>(),
                "DataShareRequest",
                "CDDO",
                "",
                "",
                "",
                It.IsAny<Dictionary<string, string>>()));

            // Act
            var result = await _controller.StartDataShareRequest(dataAssetId);

            // Assert
            _mockAppInsightsLogger.Verify(logger => logger.LogEventMainBase(
                It.IsAny<UserEvent>(),
                "DataShareRequest",
                "CDDO",
                "",
                "",
                "",
                It.IsAny<Dictionary<string, string>>()
            ), Times.Once);
        }
        [Test]
        public async Task StartDataShareRequestPrompt_ShouldRedirectToStartDataShareRequest_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var dataAssetId = Guid.NewGuid();

            // Act
            var result = await _controller.StartDataShareRequestPrompt(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult?.ActionName, Is.EqualTo("StartDataShareRequest"));
            Assert.That(redirectResult?.RouteValues?["dataAssetId"], Is.EqualTo(dataAssetId));
        }

        [Test]
        public async Task StartDataShareRequestPrompt_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var dataAssetId = Guid.NewGuid();

            // Act
            var result = await _controller.StartDataShareRequestPrompt(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task StartDataShareRequestPrompt_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            _controller.ModelState.AddModelError("TestError", "Invalid state");
            var dataAssetId = Guid.NewGuid();
            _mockLogger.Setup(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ));

            // Act
            var result = await _controller.StartDataShareRequestPrompt(dataAssetId);

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Model state is invalid")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
            ), Times.Once);
        }
        [Test]
        public async Task GetCddoDataAssetByUser_ShouldReturnViewWithExpectedModel_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var identifier = Guid.NewGuid().ToString();
            var mockDataAsset = new GetCddoDataAssetResponse() { CddoDataAsset = new CddoDataAsset() { SecurityClassification = "TopSecret" } };
            _mockCatalogDataService.Setup(service => service.GetDataAssetAsync(new Guid(identifier), default)).ReturnsAsync(mockDataAsset);
            _mockAppInsightsLogger.Setup(x => x.LogEvent(MetadataEvent.MetadataViewed, It.IsAny<Dictionary<string, string>>(), null));


            // Act
            var result = await _controller.GetCddoDataAssetByUser(identifier);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.Model, Is.EqualTo(mockDataAsset.CddoDataAsset));
        }

        [Test]
        public async Task GetCddoDataAssetByUser_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var identifier = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.GetCddoDataAssetByUser(identifier);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }
        [Test]
        public async Task DeleteCddoDataAssetConfirmation_ShouldReturnView_WhenModelStateIsValidAndUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();

            // Act
            var result = await _controller.DeleteCddoDataAssetConfirmation(deleteDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.Model, Is.EqualTo(deleteDataAssetModel));
        }

        [Test]
        public async Task DeleteCddoDataAssetConfirmation_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();

            // Act
            var result = await _controller.DeleteCddoDataAssetConfirmation(deleteDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task DeleteCddoDataAssetConfirmation_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("TestError", "Invalid model state");
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();

            // Act
            var result = await _controller.DeleteCddoDataAssetConfirmation(deleteDataAssetModel);

            // Assert
            _mockAppInsightsLogger.Verify(
                logger => logger.LogWarning("Model state is invalid for DeleteCddoDataAssetConfirmation."),
                Times.Once);
        }
        [Test]
        public async Task DeleteCddoDataAssetSubmit_ShouldRedirectToAction_WhenUserIsAuthenticatedAndModelIsValid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();
            deleteDataAssetModel.Identifier = new Guid().ToString();
            var deleteResponse = _fixture.Create<DeleteProfiledDataAssetResponse>();
            _mockCatalogDataService.Setup(service => service.DeleteDataAssetAsync(It.IsAny<DeleteProfiledDataAssetRequest>(), default)).ReturnsAsync(deleteResponse);
            _mockAppInsightsLogger.Setup(logger => logger.LogEvent(It.IsAny<MetadataEvent>(), It.IsAny<Dictionary<string, string>>(), null));

            // Act
            var result = await _controller.DeleteCddoDataAssetSubmit(deleteDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        }
        [Test]
        public async Task DeleteCddoDataAssetSubmit_ShouldRedirectToPage_WhenExceptionOccurs()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();
            deleteDataAssetModel.Identifier = new Guid().ToString();
            var deleteResponse = _fixture.Create<DeleteProfiledDataAssetResponse>();
            _mockCatalogDataService.Setup(service => service.DeleteDataAssetAsync(It.IsAny<DeleteProfiledDataAssetRequest>(), default))
                .Throws(new UnauthorizedAccessException());
            _mockAppInsightsLogger.Setup(logger => logger.LogEvent(It.IsAny<MetadataEvent>(), It.IsAny<Dictionary<string, string>>(), null));

            // Act
            var result = await _controller.DeleteCddoDataAssetSubmit(deleteDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task DeleteCddoDataAssetSubmit_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            ClearInvocations();
            SetAuthenticatedUser(false);
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();
            _mockAppInsightsLogger.Setup(logger => logger.LogEvent(It.IsAny<MetadataEvent>(), It.IsAny<Dictionary<string, string>>(), null));


            // Act
            var result = await _controller.DeleteCddoDataAssetSubmit(deleteDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task DeleteCddoDataAssetSubmit_ShouldLogEvent_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("TestError", "Invalid model state");
            var deleteDataAssetModel = _fixture.Create<DeleteDataAssetModel>();
            deleteDataAssetModel.Identifier = new Guid().ToString();

            // Act
            var result = await _controller.DeleteCddoDataAssetSubmit(deleteDataAssetModel);

            // Assert
            _mockAppInsightsLogger.Verify(
                logger => logger.LogEvent(MetadataEvent.MetadataDeleted, It.IsAny<Dictionary<string, string>>(), null),
                Times.Exactly(2));
        }

        [Test]
        public void ArchiveCddoDataAssetConfirmation_ShouldRedirectToErrorPage_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("TestError", "Invalid model state");
            var archiveDataAssetModel = _fixture.Create<ArchiveDataAssetModel>();

            // Act
            var result = _controller.ArchiveCddoDataAssetConfirmation(archiveDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/400"));
        }

        [Test]
        public void ArchiveCddoDataAssetConfirmation_ShouldReturnView_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var archiveDataAssetModel = _fixture.Create<ArchiveDataAssetModel>();

            // Act
            var result = _controller.ArchiveCddoDataAssetConfirmation(archiveDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult?.Model, Is.EqualTo(archiveDataAssetModel));
        }

        [Test]
        public void ArchiveCddoDataAssetConfirmation_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var archiveDataAssetModel = _fixture.Create<ArchiveDataAssetModel>();

            // Act
            var result = _controller.ArchiveCddoDataAssetConfirmation(archiveDataAssetModel);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult?.PageName, Is.EqualTo("/Error/403"));
        }
    }
}

