using Agm.Catalog.DotNet.Core.Utilities;
using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Api.Controllers;
using Cddo.Data.Marketplace.Api.Validation;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using CDDO.DataMarketplace.Controllers.External;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Api.Test
{
    public static class TestsSetUp
    {

        public static TestItems CreateTestItems()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            
            var mockLogger = Mock.Get(fixture.Freeze<ILogger<ReportsController>>());
            var mockApiLogger = Mock.Get(fixture.Freeze<ILogger<DataMarketplaceApiController>>());
            var mockLookupLogger = Mock.Get(fixture.Freeze<ILogger<LookupController>>());
            var mockReportsService = Mock.Get(fixture.Freeze<IReportsService>());
            var mockDataAssetService = Mock.Get(fixture.Freeze<IDataAssetService>());
            var mockReportsResponseFactory = Mock.Get(fixture.Freeze<IReportsResponseFactory>());
            var mockUserProfilePresenter = Mock.Get(fixture.Freeze<IUserProfilePresenter>());
            var mockAppInsightLogger = Mock.Get(fixture.Freeze<IAppInsightsLogger>());
            var mockEnumMemberConvertor = Mock.Get(fixture.Freeze<IEnumMemberConverter>());
            var mockValidationService = Mock.Get(fixture.Freeze<IModelValidationService>());

            var reportsController = new ReportsController(
                mockLogger.Object,
                mockReportsService.Object,
                mockReportsResponseFactory.Object,
                mockUserProfilePresenter.Object);

            var apiController = new DataMarketplaceApiController(
               mockApiLogger.Object,
               mockDataAssetService.Object,
               mockUserProfilePresenter.Object,
               mockAppInsightLogger.Object,
               mockEnumMemberConvertor.Object,
               mockValidationService.Object);

            var lookupController = new LookupController(
                mockLookupLogger.Object,
                mockDataAssetService.Object,
                mockUserProfilePresenter.Object
                );

            ConfigureHappyPathTesting();

            return new TestItems(fixture, reportsController, apiController,
                mockLogger, mockReportsService, mockReportsResponseFactory, mockUserProfilePresenter, mockDataAssetService, mockValidationService, lookupController);

            void ConfigureHappyPathTesting()
            {
                var userProfileMock = new Mock<IUserDetails>(MockBehavior.Loose);
                var userIdSet = new UserIdSet()
                {
                    UserId = 1,
                    DomainId = 1,
                    OrganisationId = 1
                };

                var userContactDetails = new UserContactDetails()
                {
                    UserName = "test user",
                    EmailAddress = "test@email.com"
                };
                var organisationDetails = new OrganisationInformation() 
                {
                    OrganisationId = 1,
                    OrganisationName = "Test Org",
                    Domains = new List<IDomainInformation>()

                };
               
                var userProfile = new UserDetails
                {
                    UserIdSet = userIdSet,
                    UserContactDetails = userContactDetails,
                    OrganisationInformation = organisationDetails
                };
                mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync())
                    .ReturnsAsync((IUserDetails)userProfile);
            }
        }
    }

    public class TestItems(
       IFixture fixture,
       ReportsController reportsController,
       DataMarketplaceApiController apiController,
       Mock<ILogger<ReportsController>> mockLogger,
       Mock<IReportsService> mockReportsService,
       Mock<IReportsResponseFactory> mockReportsResponseFactory,
       Mock<IUserProfilePresenter> mockUserPresenter,
       Mock<IDataAssetService> mockDataAssetService,
       Mock<IModelValidationService> mockModelValidationService,
       LookupController lookupController)
    {
        public IFixture Fixture { get; } = fixture;
        public ReportsController ReportsController { get; } = reportsController;
        public DataMarketplaceApiController DataMarketApiController { get; } = apiController;
        public LookupController LookupController { get; } = lookupController;
        public Mock<ILogger<ReportsController>> MockLogger { get; } = mockLogger;
        public Mock<IReportsService> MockReportsService { get; } = mockReportsService;
        public Mock<IReportsResponseFactory> MockReportsResponseFactory { get; } = mockReportsResponseFactory;
        public Mock<IUserProfilePresenter> MockUserPresenter { get; } = mockUserPresenter;
        public Mock<IDataAssetService> MockDataAssetService { get; } = mockDataAssetService;
        public Mock<IModelValidationService> MockModelValidationService { get; } = mockModelValidationService;
    }
}
