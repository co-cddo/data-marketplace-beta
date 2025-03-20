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
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;
using Cddo.Data.Marketplace.UI.Model;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Logic.Services.Users.Model.External;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.DataShareRequests;

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

            _loggerMock = new Mock<ILogger<ReportsController>>();
            _catalogReportsServiceMock = new Mock<ICatalogReportsService>();
            _userRoleServiceMock = new Mock<IUserRoleService>();
            _catalogDataServiceMock = new Mock<ICatalogDataService>();

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

        [Test]
        public async Task DownloadMetadataReport_ShouldReturnFile_WhenUserIsAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");
            var templateDetails = _fixture.Create<QueryCatalogReportsDataResponse>();

            _userRoleServiceMock.Setup(s => s.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(s => s.IsUserRoleAdmin()).ReturnsAsync(true);
            _catalogReportsServiceMock.Setup(s => s.DownloadCatalogReportsDataAsync(It.IsAny<QueryCatalogReportsDataRequest>(), default))
                .ReturnsAsync(templateDetails);

            // Act
            var result = await _controller.DownloadMetadataReport(templateId);

            // Assert
            Assert.That(result, Is.InstanceOf<FileContentResult>());
            var fileResult = (FileContentResult)result;
            Assert.That(fileResult.ContentType, Is.EqualTo(_csvString));
        }

        [Test]
        public async Task DownloadMetadataReport_ShouldRedirectToAccessDenied_WhenUserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var templateId = Guid.NewGuid();

            // Act
            var result = await _controller.DownloadMetadataReport(templateId);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo(_accessDeniedPage));
        }

        [Test]
        public async Task DownloadMetadataReport_ShouldProceed_WhenModelStateIsValidAndUserIsAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");
            var templateDetails = _fixture.Create<QueryCatalogReportsDataResponse>();

            _userRoleServiceMock.Setup(s => s.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(s => s.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(s => s.DownloadCatalogReportsDataAsync(It.IsAny<QueryCatalogReportsDataRequest>(), default))
                .ReturnsAsync(templateDetails);

            // Act
            var result = await _controller.DownloadMetadataReport(templateId);

            // Assert
            Assert.That(result, Is.InstanceOf<FileContentResult>());
        }
        [Test]
        public async Task GetCatalogMetadataReportsData_Should_LogWarning_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);

            _controller.ModelState.AddModelError("templateId", "Invalid template ID");

            // Act
            var result = await _controller.GetCatalogMetadataReportsData(
                _fixture.Create<QueryCatalogReportsDataRequestFilter>(),
                Guid.NewGuid(),
                cancellationToken: CancellationToken.None
            );

            // Assert
            var redirectResult = (RedirectToPageResult)result;

            Assert.That(redirectResult.PageName, Is.EqualTo(_accessDeniedPage));

        }

        [Test]
        public async Task GetCatalogMetadataReportsData_Should_ReturnRedirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            var requestFilter = _fixture.Create<QueryCatalogReportsDataRequestFilter>();
            SetAuthenticatedUser(false);

            // Act
            var result = await _controller.GetCatalogMetadataReportsData(requestFilter, Guid.NewGuid(), false);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetCatalogMetadataReportsData_Should_ReturnView_When_UserIsAdmin()
        {
            // Arrange
            var requestFilter = new QueryCatalogReportsDataRequestFilter() { SelectableOrganisations = "[ \"test\"]" };

            SetAuthenticatedUser(true);
            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");

            var isSystemAdmin = true;
            var isOrganisationAdmin = false;
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(isSystemAdmin);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(isOrganisationAdmin);

            var mockResult = _fixture.Create<QueryCatalogReportsDataResponse>();

            _catalogReportsServiceMock.Setup(x => x.GetCatalogReportsDataAsync(It.IsAny<QueryCatalogReportsDataRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _controller.GetCatalogMetadataReportsData(requestFilter, templateId, false);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task GetCatalogMetadataReportsData_Should_ReturnFileResult_When_IsDownloadIsTrue()
        {
            // Arrange
            var requestFilter = new QueryCatalogReportsDataRequestFilter() { SelectableOrganisations = "[ \"test\", \"test2\"]" };
            SetAuthenticatedUser(true);
            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");

            var isSystemAdmin = true;
            var isOrganisationAdmin = false;
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(isSystemAdmin);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(isOrganisationAdmin);

            var mockResult = _fixture.Create<QueryCatalogReportsDataResponse>();

            _catalogReportsServiceMock.Setup(x => x.GetCatalogReportsDataAsync(It.IsAny<QueryCatalogReportsDataRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _controller.GetCatalogMetadataReportsData(requestFilter, templateId, true);

            // Assert
            Assert.That(result, Is.InstanceOf<FileContentResult>());
        }

        [Test]
        public async Task GetCatalogMetadataReportsData_Should_ReturnRedirectToPage_When_NoPermission()
        {
            // Arrange
            var requestFilter = _fixture.Create<QueryCatalogReportsDataRequestFilter>();
            SetAuthenticatedUser(true);


            var isSystemAdmin = false;
            var isOrganisationAdmin = false;
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(isSystemAdmin);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(isOrganisationAdmin);

            // Act
            var result = await _controller.GetCatalogMetadataReportsData(requestFilter, Guid.NewGuid(), false);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }
        [Test]
        public async Task GetMetadataStatsReport_Should_LogWarning_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);

            _controller.ModelState.AddModelError("templateId", "Invalid template ID");

            // Act
            var result = await _controller.GetMetadataStatsReport(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }

        [Test]
        public async Task GetMetadataStatsReport_Should_ReturnRedirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);

            // Act
            var result = await _controller.GetMetadataStatsReport(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetMetadataStatsReport_Should_ReturnRedirect_When_UserIsNotAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.GetMetadataStatsReport(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetMetadataStatsReport_Should_ReturnView_When_UserIsAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var isSystemAdmin = true;
            var isOrganisationAdmin = true;
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(isSystemAdmin);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(isOrganisationAdmin);

            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");

            // Act
            var result = await _controller.GetMetadataStatsReport(templateId);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/MetadataReportStats.cshtml"));
            var model = viewResult.Model as MetadataReportsStats;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TemplateId, Is.EqualTo(templateId));
        }
        [Test]
        public async Task MetadataStatsPost_Should_ReturnRedirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);

            // Act
            var result = await _controller.MetadataStatsPost(Guid.NewGuid(), "OrgName", "AssetType", false);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task MetadataStatsPost_Should_ReturnRedirect_When_UserIsNotAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.MetadataStatsPost(Guid.NewGuid(), "OrgName", "AssetType", false);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task MetadataStatsPost_Should_ReturnViewResult_When_UserIsAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");

            // Act
            var result = await _controller.MetadataStatsPost(templateId, "OrgName", "AssetType", false);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/MetadataReportStats.cshtml"));
            var model = viewResult.Model as MetadataReportsStats;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.TemplateId, Is.EqualTo(templateId));
        }

        [Test]
        public async Task MetadataStatsPost_Should_ReturnCsvFile_When_DownloadIsTrue()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            var templateId = new Guid("3d04aac5-0ac3-4e41-b45c-63aa8688ca42");

            // Act
            var result = await _controller.MetadataStatsPost(templateId, "OrgName", "AssetType", true);

            // Assert
            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.That(fileResult.ContentType, Is.EqualTo("text/csv"));
        }
        [Test]
        public async Task GetTelemetryReportsData_Should_Redirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);

            var templateId = new Guid("0a87e370-0428-427c-8de9-313776944c35");

            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "1.00:00:00");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetTelemetryReportsData_Should_Redirect_When_UserIsNotAdminOrSystemAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);

            var templateId = new Guid("0a87e370-0428-427c-8de9-313776944c35");


            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(_fixture.Create<LogsQueryDataResult>());

            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "1.00:00:00");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetTelemetryReportsData_Should_ReturnViewResult_When_UserIsAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            var logResult = new LogsQueryDataResult()
            {
                Results = new
                TelemetryQueryResultsData()
                {
                    ColumnData = _fixture.Create<TelemetryQueryResultsTableColumnSet>(),
                    RowData = _fixture.Create<TelemetryQueryResultsTableRowSet>(),
                }
                
            };
            logResult.Results.RowData.Rows.First().RowValues.First().Value = "Total";
            logResult.Results.ColumnData.Columns.First().Name = "UserId";

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(logResult);
            var templateId = new Guid("0a87e370-0428-427c-8de9-313776944c35");


            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "1.00:00:00");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/TelemetryReport.cshtml"));
        }

        [Test]
        public async Task GetTelemetryReportsData_Should_ReturnViewResult_When_UserIsAdmin_Empty_Time_Range()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            var logResult = new LogsQueryDataResult()
            {
                Results = new
                TelemetryQueryResultsData()
                {
                    ColumnData = _fixture.Create<TelemetryQueryResultsTableColumnSet>(),
                    RowData = _fixture.Create<TelemetryQueryResultsTableRowSet>(),
                }
                
            };
            logResult.Results.RowData.Rows.First().RowValues.First().Value = "Total";
            logResult.Results.ColumnData.Columns.First().Name = "UserId";

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(logResult);
            var templateId = new Guid("0a87e370-0428-427c-8de9-313776944c35");


            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/TelemetryReport.cshtml"));
        }
        [Test]
        public async Task GetTelemetryReportsData_Should_ReturnViewResult_When_UserIsAdmin_And_templateId_Is_Specific()
        {
            // Arrange
            SetAuthenticatedUser(true);

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(_fixture.Create<LogsQueryDataResult>());
            var templateId = new Guid("6806d74c-a510-4add-b8ce-ee808dd6efc7");


            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "1.00:00:00");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/TelemetryReport.cshtml"));
        }

        [Test]
        public async Task GetTelemetryReportsData_Should_ReturnViewResult_When_ExceptionIsThrown()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var templateId = new Guid("0a87e370-0428-427c-8de9-313776944c35");

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "1.00:00:00");

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/TelemetryReport.cshtml"));
            var model = viewResult.Model as PaginatedLogsDataResult;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.ReportName, Is.EqualTo("Custom Report"));
        }

        [Test]
        public async Task GetTelemetryReportsData_Should_ReturnBadRequest_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var templateId = new Guid("0a87e370-0428-427c-8de9-313776944c35");

            _controller.ModelState.AddModelError("key", "error");

            // Act
            var result = await _controller.GetTelemetryReportsData(templateId, "1.00:00:00");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }


        [Test]
        public async Task CreateReportDetails_Should_ReturnViewResult()
        {
            // Act
            var result = await _controller.CreateReportDetails();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/CreateReport.cshtml"));
        }


        [Test]
        public async Task CreateReportDetailsTemplate_Should_LogWarning_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);

            _controller.ModelState.AddModelError("key", "error");

            var reportTemplate = _fixture.Create<ReportTemplate>();

            // Act
            var result = await _controller.CreateReportDetailsTemplate(reportTemplate);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }

        [Test]
        public async Task CreateReportDetailsTemplate_Should_Redirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var reportTemplate = _fixture.Create<ReportTemplate>();

            // Act
            var result = await _controller.CreateReportDetailsTemplate(reportTemplate);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task CreateReportDetailsTemplate_Should_Redirect_When_UserIsNotAdminOrSystemAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var reportTemplate = _fixture.Create<ReportTemplate>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.CreateReportDetailsTemplate(reportTemplate);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task CreateReportDetailsTemplate_Should_Redirect_When_UserIsAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var reportTemplate = _fixture.Create<ReportTemplate>();
            var userProfile = _fixture.Create<Api.Dto.Models.UserProfile>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Act
            var result = await _controller.CreateReportDetailsTemplate(reportTemplate);

            // Assert
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.PageName, Is.EqualTo("/Reports/MetadataReports"));
        }

        [Test]
        public async Task CreateReportDetailsTemplate_Should_Redirect_To_Appropriate_Page_BasedOn_ReportType()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var reportTemplate = _fixture.Create<ReportTemplate>();
            var userProfile = _fixture.Create<Api.Dto.Models.UserProfile>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Test for each ReportType
            var reportTypes = new[]
            {
                ReportType.Telemetry,
                ReportType.Metadata,
                ReportType.Users,
                ReportType.Datasharerequests,
                ReportType.None
            };

            foreach (var reportType in reportTypes)
            {
                reportTemplate.ReportType = reportType;

                // Act
                var result = await _controller.CreateReportDetailsTemplate(reportTemplate);

                // Assert
                var redirectResult = result as RedirectToPageResult;
                Assert.That(redirectResult, Is.Not.Null);

                switch (reportType)
                {
                    case ReportType.Telemetry:
                        Assert.That(redirectResult.PageName, Is.EqualTo("/Reports/TelemetryReports"));
                        break;
                    case ReportType.Metadata:
                        Assert.That(redirectResult.PageName, Is.EqualTo("/Reports/MetadataReports"));
                        break;
                    case ReportType.Users:
                        Assert.That(redirectResult.PageName, Is.EqualTo("/Reports/UsersReports"));
                        break;
                    case ReportType.Datasharerequests:
                        Assert.That(redirectResult.PageName, Is.EqualTo("/Reports/DatashareReports"));
                        break;
                    case ReportType.None:
                    default:
                        Assert.That(redirectResult.PageName, Is.EqualTo("/Reports/ReportsList"));
                        break;
                }
            }
        }

        [Test]
        public async Task QueryTelemetryData_Should_LogWarning_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _controller.ModelState.AddModelError("key", "error");

            // Act
            var result = await _controller.QueryTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }

        [Test]
        public async Task QueryTelemetryData_Should_Redirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();

            // Act
            var result = await _controller.QueryTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task QueryTelemetryData_Should_Redirect_When_UserIsNotAdminOrSystemAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.QueryTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task QueryTelemetryData_Should_Return_View_When_ReportProcessingIsSuccessful()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(_fixture.Create<LogsQueryDataResult>());

            // Act
            var result = await _controller.QueryTelemetryData(telemetryRequest);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/TelemetryReport.cshtml"));
        }

        [Test]
        public async Task QueryTelemetryData_Should_Return_View_With_EmptyResult_When_ExceptionOccurs()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                            .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.QueryTelemetryData(telemetryRequest);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/TelemetryReport.cshtml"));
            Assert.That(viewResult.Model, Is.InstanceOf<PaginatedLogsDataResult>());
        }

        [Test]
        public async Task DownloadTelemetryData_Should_LogWarning_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            _controller.ModelState.AddModelError("key", "error");

            // Act
            var result = await _controller.DownloadTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }

        [Test]
        public async Task DownloadTelemetryData_Should_Redirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();

            // Act
            var result = await _controller.DownloadTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task DownloadTelemetryData_Should_Redirect_When_UserIsNotAdminOrSystemAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);

            // Act
            var result = await _controller.DownloadTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task DownloadTelemetryData_Should_Return_FileResult_When_ReportIsSuccessfullyGenerated()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(_fixture.Create<LogsQueryDataResult>());

            // Act
            var result = await _controller.DownloadTelemetryData(telemetryRequest);

            // Assert
            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.That(fileResult.ContentType, Is.EqualTo("text/csv"));
        }

        [Test]
        public async Task DownloadTelemetryData_Should_LogError_When_ExceptionOccurs()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var telemetryRequest = _fixture.Create<LogsQueryDataRequest>();
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetTelemetryReportsDataAsync(It.IsAny<string>(), It.IsAny<string>(), default)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.DownloadTelemetryData(telemetryRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }
        [Test]
        public async Task GetDSRReportData_Should_LogError_When_ModelStateIsInvalid()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var query = _fixture.Create<DataShareRequestCountQuery>();
            _controller.ModelState.AddModelError("key", "error");

            // Act
            var result = await _controller.GetDSRReportData(query, "08/09/2024", "08/09/2025");

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

        }

        [Test]
        public async Task GetDSRReportData_Should_Redirect_When_UserIsNotAuthenticated()
        {
            // Arrange
            SetAuthenticatedUser(false);
            var query = _fixture.Create<DataShareRequestCountQuery>();

            // Act
            var result = await _controller.GetDSRReportData(query, null, null);

            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetDSRReportData_Should_Redirect_When_UserIsNotAuthorized()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var query = _fixture.Create<DataShareRequestCountQuery>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleSupplier()).ReturnsAsync(false);

            // Act
            var result = await _controller.GetDSRReportData(query, "08/09/2024", "08/09/2025");


            // Assert
            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
        }

        [Test]
        public async Task GetDSRReportData_Should_Return_FileResult_When_DownloadCsvIsTrue()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var query = _fixture.Create<DataShareRequestCountQuery>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleSupplier()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetDSRReportDataAsync(It.IsAny<QueryDataShareRequestsCountsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_fixture.Create<QueryDataShareRequestsCountsResponse>());

            // Act
            var result = await _controller.GetDSRReportData(query, null, null, downloadCsv: true, isSystemAdministrator: true);

            // Assert
            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.That(fileResult.ContentType, Is.EqualTo("text/csv"));
        }       
        [Test]
        public async Task GetDSRReportData_Should_Return_FileResult_When_DownloadCsvIsTrue_NotSysAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var query = _fixture.Create<DataShareRequestCountQuery>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(false);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleSupplier()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetDSRReportDataAsync(It.IsAny<QueryDataShareRequestsCountsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_fixture.Create<QueryDataShareRequestsCountsResponse>());

            // Act
            var result = await _controller.GetDSRReportData(query, "09/10/2024", "09/10/2025", downloadCsv: true);

            // Assert
            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.That(fileResult.ContentType, Is.EqualTo("text/csv"));
        }

        [Test]
        public async Task GetDSRReportData_Should_Render_View_When_DownloadCsvIsFalse()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var query = _fixture.Create<DataShareRequestCountQuery>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);

            _catalogReportsServiceMock.Setup(x => x.GetDSRReportDataAsync(It.IsAny<QueryDataShareRequestsCountsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_fixture.Create<QueryDataShareRequestsCountsResponse>());

            // Act
            var result = await _controller.GetDSRReportData(query, null, null, downloadCsv: false);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/DataShareReport.cshtml"));
        }
        [Test]
        public async Task GetDSRReportData_Should_Render_View_When_DownloadCsvIsFalseAndUser_Is_sysAdmin()
        {
            // Arrange
            SetAuthenticatedUser(true);
            var query = _fixture.Create<DataShareRequestCountQuery>();

            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.IsUserRoleSystemAdmin()).ReturnsAsync(true);
            _userRoleServiceMock.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(new Api.Dto.Models.UserProfile());

            _catalogReportsServiceMock.Setup(x => x.GetDSRReportDataAsync(It.IsAny<QueryDataShareRequestsCountsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_fixture.Create<QueryDataShareRequestsCountsResponse>());

            // Act
            var result = await _controller.GetDSRReportData(query, null, null, downloadCsv: false);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Reports/DataShareReport.cshtml"));
        }
        [Test]
        public void ClearViewBagAndRefresh_Should_Clear_ViewBag_And_RedirectToCorrectAction()
        {
            
            // Act
            var result = _controller.ClearViewBagAndRefresh();

            // Assert
            Assert.That(_controller.ViewData.Count, Is.EqualTo(0), "ViewData should be cleared.");

            var redirectToActionResult = result as RedirectToActionResult;
            Assert.That(redirectToActionResult, Is.Not.Null);
            Assert.That(redirectToActionResult.ActionName, Is.EqualTo("GetTelemetryReportsData"));
            Assert.That(redirectToActionResult.RouteValues["templateId"], Is.EqualTo(new Guid("6806d74c-a510-4add-b8ce-ee808dd6efc7")));
        }

    }
}
