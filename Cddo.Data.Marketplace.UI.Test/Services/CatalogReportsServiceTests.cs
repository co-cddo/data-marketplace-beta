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
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using FluentAssertions;
using System.Net;
using Flurl.Http.Testing;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Cddo.Data.Marketplace.Api.Dto.Responses.EventLogs;
using Moq;
using Org.BouncyCastle.Asn1.Cmp;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    [TestFixture]
    public class CatalogReportsServiceTests
    {
        [Test]
        [TestCaseSource(nameof(ConstructionWithNullParameterTestCaseData))]
        public void GivenANullParameter_WhenIConstructAnInstanceOfCatalogQuestionsService_ThenAnArgumentNullExceptionIsThrown(
            string expectedExceptionParameterName,
            ILogger<CatalogReportsService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IUserRoleService userRoleService)
        {
            Assert.That(() => new CatalogReportsService(logger, configuration, httpContextAccessor, userRoleService),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo(expectedExceptionParameterName));
        }
        private static IEnumerable<TestCaseData> ConstructionWithNullParameterTestCaseData()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var logger = fixture.Create<ILogger<CatalogReportsService>>();
            var configuration = fixture.Create<IConfiguration>();
            var httpContextAccessor = fixture.Create<IHttpContextAccessor>();
            var userRoleService = fixture.Create<IUserRoleService>();

            yield return new TestCaseData("logger", null, configuration, httpContextAccessor, userRoleService);
            yield return new TestCaseData("configuration", logger, null, httpContextAccessor, userRoleService);
            yield return new TestCaseData("httpContextAccessor", logger, configuration, null, userRoleService);
            yield return new TestCaseData("userRoleService", logger, configuration, httpContextAccessor, null);
        }

        #region Download
        [Test]
        public async Task DownloadCatalogReportsDataAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

            //Act
            var result = await testItems.CatalogReportsService.DownloadCatalogReportsDataAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task DownloadCatalogReportsDataAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/download-catalog-reports-data")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogReportsService.DownloadCatalogReportsDataAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task DownloadCatalogReportsDataAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogReportsService.DownloadCatalogReportsDataAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task DownloadCatalogReportsDataAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();
            var testResponse = testItems.Fixture.Create<QueryCatalogReportsDataResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/download-catalog-reports-data")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogReportsService.DownloadCatalogReportsDataAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        [Test]
        public async Task DownloadCatalogReportsDataAsync_WhenAddProfileApiEndpointIsCalledWithPublisherFilters_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();
            var testResponse = testItems.Fixture.Create<QueryCatalogReportsDataResponse>();

            request.Filter = new Api.Dto.Models.Reports.CatalogData.Filters.CatalogReportsFilter()
            {
                FieldFilters = new List<Api.Dto.Models.Reports.CatalogData.Filters.CatalogReportFieldFilter> { 
                    new Api.Dto.Models.Reports.CatalogData.Filters.CatalogReportFieldFilter() {
                        Field = Agm.Catalog.DotNet.Dto.Models.CatalogData.CatalogAssetField.Publisher,
                        Values = new List<string> {"filter field1", "filter-field2"}
                    }
                }
            };

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/download-catalog-reports-data")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogReportsService.DownloadCatalogReportsDataAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        #endregion

        #region GetCatalogReportsDataAsync
        [Test]
        public async Task GetCatalogReportsDataAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

            //Act
            var result = await testItems.CatalogReportsService.GetCatalogReportsDataAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetCatalogReportsDataAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/query-catalog-reports-data")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogReportsService.GetCatalogReportsDataAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetCatalogReportsDataAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogReportsService.GetCatalogReportsDataAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetCatalogReportsDataAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();
            var testResponse = testItems.Fixture.Create<QueryCatalogReportsDataResponse>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();

            httpTest.ForCallsTo($"http://xyz/query-catalog-reports-data")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogReportsService.GetCatalogReportsDataAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        #endregion

        #region GetTelemetryReportsDataAsync
        [Test]
        public async Task GetTelemetryReportsDataAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<string>();
            var timeRange = testItems.Fixture.Create<string>();

            //Act
            var result = await testItems.CatalogReportsService.GetTelemetryReportsDataAsync(request, timeRange);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetTelemetryReportsDataAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<string>();
            var timeRange = testItems.Fixture.Create<string>();

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/User/GetEventLogs/{request}")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogReportsService.GetTelemetryReportsDataAsync(request, timeRange);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetTelemetryReportsDataAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<string>();
            var timeRange = testItems.Fixture.Create<string>();

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogReportsService.GetTelemetryReportsDataAsync(request, timeRange);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetTelemetryReportsDataAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<string>();
            var timeRange = testItems.Fixture.Create<string>();
            var testResponse = testItems.Fixture.Create<LogsQueryDataResult>();

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();
            var clean = request.Replace("\r", "").Replace("\n", "").Replace("\\", "");
            httpTest.ForCallsTo($"http://xyz/User/GetEventLogs?searchQuery={clean}&timeRange={timeRange}")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogReportsService.GetTelemetryReportsDataAsync(request, timeRange);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }

        #endregion

        #region GetTelemetryReportsDataAsync
        [Test]
        public async Task GetDSRReportDataAsync_WhenUserIsNotAllowedRoles_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryDataShareRequestsCountsRequest>();

            testItems.MockUserRoleService.Setup(u=>u.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(false);

            //Act
            var result = await testItems.CatalogReportsService.GetDSRReportDataAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetDSRReportDataAsync_WhenTokenIsNull_Default()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryDataShareRequestsCountsRequest>();
            testItems.MockUserRoleService.Setup(u => u.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);
            //Act
            var result = await testItems.CatalogReportsService.GetDSRReportDataAsync(request);

            //Assert
            result.Should().Be(null);
        }

        [Test]
        public async Task GetDSRReportDataAsync_WhenApiCallThrowsFlurlHttpException_Throws()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryDataShareRequestsCountsRequest>();
            testItems.MockUserRoleService.Setup(u => u.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            using var httpTest = new HttpTest();
            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            httpTest.ForCallsTo($"http://xyz/Reporting/QueryDataShareRequestCounts")
            .RespondWith("", (int)HttpStatusCode.InternalServerError);

            //Act
            var result = await testItems.CatalogReportsService.GetDSRReportDataAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDSRReportDataAsync_WhenApiCallThrowsHttpException_Null()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryDataShareRequestsCountsRequest>();
            testItems.MockUserRoleService.Setup(u => u.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            testItems.MockHttpContextAccessor.Setup(c => c.HttpContext).Throws(new Exception("not here lad"));

            //Act
            var result = await testItems.CatalogReportsService.GetDSRReportDataAsync(request);
            //Assert
            result.Should().Be(null);

        }

        [Test]
        public async Task GetDSRReportDataAsync_WhenAddProfileApiEndpointIsCalled_CreateProfiledDataAssert()
        {
            //Arrange
            var testItems = ServicesTestSetUp.CreateTestItems();
            var request = testItems.Fixture.Create<QueryDataShareRequestsCountsRequest>();
            var testResponse = testItems.Fixture.Create<QueryDataShareRequestsCountsResponse>();

            testItems.MockUserRoleService.Setup(u => u.IsUserInRoleAsync(It.IsAny<List<string>>())).ReturnsAsync(true);

            ServicesTestSetUp.SetupTestHttpContext(testItems.MockHttpContextAccessor, "Bearer mytestToken");
            using var httpTest = new HttpTest();
            httpTest.ForCallsTo($"http://xyz/Reporting/QueryDataShareRequestCounts")
               .RespondWithJson(testResponse);

            //Act
            var result = await testItems.CatalogReportsService.GetDSRReportDataAsync(request);
            //Assert
            result.Should().BeEquivalentTo(testResponse);

        }
        #endregion

        [Test]
        public void PrettifyString_WhenInputIsNull_EmptyString()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();

            var result = testItems.CatalogReportsService.PrettifyString(null);

            result.Should().BeEquivalentTo("");
        }

        [Test]
        public void PrettifyString_WhenInputIsNotEmptyPretifiedString()
        {
            var testItems = ServicesTestSetUp.CreateTestItems();
            var testString = "This is my-test-string";

            var result = testItems.CatalogReportsService.PrettifyString(testString);

            result.Should().BeEquivalentTo("This is my Test String");
        }
    }
}
