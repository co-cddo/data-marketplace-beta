using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Logic.Services.Ckan;
using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Logic.Test.Services.Reports;

[TestFixture]
public class ReportsServiceTests
{
    #region GetCatalogReportsDataAsync() Tests
    [Test]
    public void GivenANullSetOfRequiredFields_WhenIGetCatalogReportsDataAsync_ThenAnArgumentNullExceptionIsThrown()
    {
        var testItems = CreateTestItems();

        Assert.That(() => testItems.ReportsService.GetCatalogReportsDataAsync(
                CreateTestUserDetails(),
            null!,
            It.IsAny<ICatalogReportsFilter?>(), It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>()),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("requiredFields"));
    }

    [Test]
    public async Task GivenANullCatalogReportsFilter_WhenIGetCatalogReportsDataAsync_ThenNoReportFieldFiltersAreConverted()
    {
        var testItems = CreateTestItems();

        await testItems.ReportsService.GetCatalogReportsDataAsync(
            CreateTestUserDetails(),
            testItems.Fixture.CreateMany<CatalogAssetField>(),
            null,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>()
            );

        testItems.MockReportFieldFilterConverter.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GivenACatalogReportsFilterWithFieldFilters_WhenIGetCatalogReportsDataAsync_ThenTheFieldFiltersAreConverted()
    {
        var testItems = CreateTestItems();

        var testFieldFilters = testItems.Fixture.CreateMany<CatalogReportFieldFilter>().ToList();

        var mockCatalogReportsFilter = new Mock<ICatalogReportsFilter>();
        mockCatalogReportsFilter.SetupGet(x => x.FieldFilters).Returns(testFieldFilters);

        await testItems.ReportsService.GetCatalogReportsDataAsync(
            CreateTestUserDetails(),
            testItems.Fixture.CreateMany<CatalogAssetField>(),
            mockCatalogReportsFilter.Object,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>());

        Assert.Multiple(() =>
        {
            foreach (var testFieldFilter in testFieldFilters)
            {
                testItems.MockReportFieldFilterConverter.Verify(x => x.ConvertReportFieldFilter(testFieldFilter),
                    Times.Once);
            }
        });
    }

    [Test]
    public async Task GivenACatalogReportsFilterWithFilterByInitiatingUserPermissionsSet_WhenIGetCatalogReportsDataAsync_ThenOrganisationFiltersAreAppliedForTheInitiatingUser()
    {
        var testItems = CreateTestItems();

        var testInitiatingUserDetails = CreateTestUserDetails(
            organisationId: 123);

        var testFieldFilters = testItems.Fixture.CreateMany<CatalogReportFieldFilter>().ToList();

        var mockCatalogReportsFilter = new Mock<ICatalogReportsFilter>();
        mockCatalogReportsFilter.SetupGet(x => x.FilterByInitiatingUserPermissions).Returns(true);
        mockCatalogReportsFilter.SetupGet(x => x.FieldFilters).Returns(testFieldFilters);
        
        await testItems.ReportsService.GetCatalogReportsDataAsync(
            testInitiatingUserDetails,
            testItems.Fixture.CreateMany<CatalogAssetField>(),
            mockCatalogReportsFilter.Object,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>());

        testItems.MockCkanConnection.Verify(x => x.GetFilteredCatalogEntriesAsync(
                It.Is<ICatalogEntriesOrganisationFilter>(o =>
                    o.OrganisationId == "123" &&
                    o.FilterByOrganisationDiscoverability == true),
                It.IsAny<IEnumerable<ICatalogAssetFieldFilter>>(), 
                It.IsAny<ICatalogEntriesResultPagination>(),
            It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task GivenACatalogReportsFilterWithFilterByInitiatingUserPermissionsClear_WhenIGetCatalogReportsDataAsync_ThenOrganisationFiltersAreNotApplied()
    {
        var testItems = CreateTestItems();

        var testInitiatingUserDetails = CreateTestUserDetails(
            organisationId: 123);

        var testFieldFilters = testItems.Fixture.CreateMany<CatalogReportFieldFilter>().ToList();

        var mockCatalogReportsFilter = new Mock<ICatalogReportsFilter>();
        mockCatalogReportsFilter.SetupGet(x => x.FilterByInitiatingUserPermissions).Returns(false);
        mockCatalogReportsFilter.SetupGet(x => x.FieldFilters).Returns(testFieldFilters);

        await testItems.ReportsService.GetCatalogReportsDataAsync(
            testInitiatingUserDetails,
            testItems.Fixture.CreateMany<CatalogAssetField>(),
            mockCatalogReportsFilter.Object,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>());

        testItems.MockCkanConnection.Verify(x => x.GetFilteredCatalogEntriesAsync(
                It.Is<ICatalogEntriesOrganisationFilter>(o =>
                    o.FilterByOrganisationDiscoverability == false),
                It.IsAny<IEnumerable<ICatalogAssetFieldFilter>>(), 
                It.IsAny<ICatalogEntriesResultPagination>(),
            It.IsAny<string>()),
            Times.Once);
    }
    #endregion

    #region Test Data Creation
    private static IUserDetails CreateTestUserDetails(
        int? organisationId = null)
    {
        var mockUserIdSet = new Mock<IUserIdSet>();
        if (organisationId.HasValue)
        {
            mockUserIdSet.SetupGet(x => x.OrganisationId).Returns(organisationId.Value);
        }

        var mockUserDetails = new Mock<IUserDetails>();
        mockUserDetails.Setup(x => x.UserIdSet).Returns(mockUserIdSet.Object);
        
        return mockUserDetails.Object;
    }
    #endregion

    #region Test Item Creation
    private static TestItems CreateTestItems()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());

        var mockLogger = Mock.Get(fixture.Freeze<ILogger<ReportsService>>());
        var mockCkanConnection = Mock.Get(fixture.Freeze<ICkanConnection>());
        var mockCatalogReportsDataItemsBuilder = Mock.Get(fixture.Freeze<ICatalogReportsDataItemsBuilder>());
        var mockReportFieldFilterConverter = Mock.Get(fixture.Freeze<IReportFieldFilterConverter>());
        var mockServiceOperationResultFactory = Mock.Get(fixture.Freeze<IServiceOperationResultFactory>());

        var reportsService = new ReportsService(
            mockLogger.Object,
            mockCkanConnection.Object,
            mockCatalogReportsDataItemsBuilder.Object,
            mockReportFieldFilterConverter.Object,
            mockServiceOperationResultFactory.Object);

        return new TestItems(
            fixture,
            reportsService,
            mockCkanConnection,
            mockReportFieldFilterConverter);
    }

    private class TestItems(
        IFixture fixture,
        IReportsService reportsService,
        Mock<ICkanConnection> mockCkanConnection,
        Mock<IReportFieldFilterConverter> mockReportFieldFilterConverter)
    {
        public IFixture Fixture { get; } = fixture;
        public IReportsService ReportsService { get; } = reportsService;
        public Mock<ICkanConnection> MockCkanConnection { get; } = mockCkanConnection;
        public Mock<IReportFieldFilterConverter> MockReportFieldFilterConverter { get; } = mockReportFieldFilterConverter;
    }
    #endregion
}