using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Controllers;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.Reports.Results;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;
using Cddo.Data.Marketplace.Api.Test.TestHelpers;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Api.Test.Controllers;

[TestFixture]
public class ReportsControllerTests
{
    #region QueryCatalogReportsData() Tests
    [Test]
    public void GivenANullQueryCatalogReportsDataRequest_WhenIQueryCatalogReportsData_ThenAnArgumentNullExceptionIsThrown()
    {
        var testItems = TestsSetUp.CreateTestItems();

        Assert.That(() => testItems.ReportsController.QueryCatalogReportsData(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("queryCatalogReportsDataRequest"));
    }

    [Test]
    public async Task GivenAQueryCatalogReportsDataRequest_WhenIQueryCatalogReportsData_ThenTheAnOkResultIsReturned()
    {
        var testItems = TestsSetUp.CreateTestItems();

        var testQueryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

        var testGetCatalogReportsDataResult = testItems.Fixture.Create<IGetCatalogReportsDataResult>();

        testItems.MockReportsService.Setup(x => x.GetCatalogReportsDataAsync(
                It.IsAny<IUserDetails>(),
                testQueryCatalogReportsDataRequest.RequiredFields,
                testQueryCatalogReportsDataRequest.Filter,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(() => CreateTestServiceOperationDataResult(success: true, data: testGetCatalogReportsDataResult));

        var testQueryCatalogReportsDataResponse = testItems.Fixture.Create<QueryCatalogReportsDataResponse>();

        testItems.MockReportsResponseFactory.Setup(x => x.CreateGetCatalogReportsDataResponse(
                testGetCatalogReportsDataResult))
            .Returns(() => testQueryCatalogReportsDataResponse);

        var result = await testItems.ReportsController.QueryCatalogReportsData(testQueryCatalogReportsDataRequest);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var typedResult = result as OkObjectResult;
            Assert.That(typedResult!.StatusCode, Is.EqualTo(200));
            Assert.That(typedResult.Value, Is.EqualTo(testQueryCatalogReportsDataResponse));
        });
    }

    [Test]
    public async Task GivenTheReportsServiceWillReturnFailure_WhenIQueryCatalogReportsData_ThenABadRequestIsReturnedReportingTheError()
    {
        var testItems = TestsSetUp.CreateTestItems();

        var testQueryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

        testItems.MockReportsService.Setup(x => x.GetCatalogReportsDataAsync(
                It.IsAny<IUserDetails>(),
                It.IsAny<IEnumerable<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(), 
                It.IsAny<string>()))
            .ReturnsAsync(() => CreateTestServiceOperationDataResult<IGetCatalogReportsDataResult>(success: false, error: "test error message"));

        var result = await testItems.ReportsController.QueryCatalogReportsData(testQueryCatalogReportsDataRequest);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

            var typedResult = result as BadRequestObjectResult;

            Assert.That(typedResult!.StatusCode, Is.EqualTo(400));
            Assert.That(typedResult.Value, Is.EqualTo("test error message"));
        });
    }

    [Test]
    public async Task GivenTheReportsServiceWillReturnFailure_WhenIQueryCatalogReportsData_ThenTheErrorIsLogged()
    {
        var testItems = TestsSetUp.CreateTestItems();

        var testQueryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

        testItems.MockReportsService.Setup(x => x.GetCatalogReportsDataAsync(
                It.IsAny<IUserDetails>(),
                It.IsAny<IEnumerable<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(), 
                It.IsAny<string>()))
            .ReturnsAsync(() => CreateTestServiceOperationDataResult<IGetCatalogReportsDataResult>(success: false, error: "test error message"));

        var result = await testItems.ReportsController.QueryCatalogReportsData(testQueryCatalogReportsDataRequest);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());

    }

    [Test]
    public async Task GivenTheReportsServiceWillThrowAnException_WhenIQueryCatalogReportsData_ThenABadRequestIsReturnedReportingTheError()
    {
        var testItems = TestsSetUp.CreateTestItems();

        var testQueryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

        testItems.MockReportsService.Setup(x => x.GetCatalogReportsDataAsync(
                It.IsAny<IUserDetails>(),
                It.IsAny<IEnumerable<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(), 
                It.IsAny<string>()))
            .Throws(new Exception("test exception error message"));

        var result = await testItems.ReportsController.QueryCatalogReportsData(testQueryCatalogReportsDataRequest);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

            var typedResult = result as BadRequestObjectResult;

            Assert.That(typedResult!.StatusCode, Is.EqualTo(400));
            Assert.That(typedResult.Value, Is.EqualTo("test exception error message"));
        });
    }

    [Test]
    public async Task GivenTheReportsServiceWillThrowAnException_WhenIQueryCatalogReportsData_ThenTheExceptionIsLogged()
    {
        var testItems = TestsSetUp.CreateTestItems();

        var testQueryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();

        var testException = new Exception("test exception error message");
        testItems.MockReportsService.Setup(x => x.GetCatalogReportsDataAsync(
                It.IsAny<IUserDetails>(),
                It.IsAny<IEnumerable<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<string>()))
            .Throws(testException);

        await testItems.ReportsController.QueryCatalogReportsData(testQueryCatalogReportsDataRequest);

        testItems.MockLogger.VerifyLog(LogLevel.Error, "test exception error message", testException);
    }
    #endregion

    [Test]
    public async Task DownloadCatalogReportsData_ShouldReturnOk_WhenSuccess()
    {
        // Arrange
        var testItems = TestsSetUp.CreateTestItems();

        var queryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();
        var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

        testItems.MockUserPresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

        var mockResult = new Mock<IServiceOperationDataResult<IGetCatalogReportsDataResult>>();
        mockResult.Setup(r => r.Success).Returns(true);
        mockResult.Setup(r => r.Data).Returns(testItems.Fixture.Create<IGetCatalogReportsDataResult>());

        testItems.MockReportsService
            .Setup(service => service.GetCatalogReportsDataAsync(It.IsAny<IUserDetails>(), It.IsAny<List<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(mockResult.Object);

        var response = testItems.Fixture.Create<QueryCatalogReportsDataResponse>();

        testItems.MockReportsResponseFactory
            .Setup(factory => factory.CreateGetCatalogReportsDataResponse(It.IsAny<IGetCatalogReportsDataResult>()))
            .Returns(response);

        // Act
        var result = await testItems.ReportsController.DownloadCatalogReportsData(queryCatalogReportsDataRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult?.Value, Is.EqualTo(response));
    }
    [Test]
    public async Task DownloadCatalogReportsData_ShouldReturnBadRequest_WhenFailure()
    {
        // Arrange
        var testItems = TestsSetUp.CreateTestItems();

        var queryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();
        var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

        testItems.MockUserPresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

        var mockResult = new Mock<IServiceOperationDataResult<IGetCatalogReportsDataResult>>();
        mockResult.Setup(r => r.Success).Returns(false);
        mockResult.Setup(r => r.Error).Returns("Failed to get catalog reports data");

        testItems.MockReportsService
            .Setup(service => service.GetCatalogReportsDataAsync(It.IsAny<IUserDetails>(), It.IsAny<List<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(mockResult.Object);

        // Act
        var result = await testItems.ReportsController.DownloadCatalogReportsData(queryCatalogReportsDataRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Failed to get catalog reports data"));
    }
    [Test]
    public async Task DownloadCatalogReportsData_ShouldReturnBadRequest_WhenExceptionThrown()
    {
        // Arrange
        var testItems = TestsSetUp.CreateTestItems();

        var queryCatalogReportsDataRequest = testItems.Fixture.Create<QueryCatalogReportsDataRequest>();
        var initiatingUserDetails = testItems.Fixture.Create<IUserDetails>();

        testItems.MockUserPresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

        testItems.MockReportsService
            .Setup(service => service.GetCatalogReportsDataAsync(It.IsAny<IUserDetails>(), It.IsAny<List<CatalogAssetField>>(),
                It.IsAny<ICatalogReportsFilter>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await testItems.ReportsController.DownloadCatalogReportsData(queryCatalogReportsDataRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
    }


    #region Test Data Creation
    private static IServiceOperationDataResult<T> CreateTestServiceOperationDataResult<T>(
        bool? success = null,
        string? error = null,
        T? data = default)
    {
        var mockServiceOperationDataResult = new Mock<IServiceOperationDataResult<T>>();

        mockServiceOperationDataResult.SetupGet(x => x.Success).Returns(success ?? true);
        mockServiceOperationDataResult.SetupGet(x => x.Error).Returns(error);
        mockServiceOperationDataResult.SetupGet(x => x.Data).Returns(data);

        return mockServiceOperationDataResult.Object;
    }
    #endregion
}