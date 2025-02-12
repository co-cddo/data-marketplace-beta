using Agm.Catalog.DotNet.Core.Utilities;
using Agm.Catalog.DotNet.Logic.Services.Ckan.Configuration;
using Agm.Catalog.DotNet.Logic.Services.Ckan;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetConversion;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.DataAssetMigration;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Duplication;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.SpreadsheetIngestion.Validation;
using Agm.Catalog.DotNet.Logic.Services.EmbeddedResourceProvision;
using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Api.Controllers;
using Cddo.Data.Marketplace.Api.Validation;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Conversion;
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
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Http;

namespace Cddo.Data.Marketplace.Logic.Test
{
    public static class TestsSetUp
    {

        public static TestItems CreateTestItems()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var mockReportsService = Mock.Get(fixture.Freeze<IReportsService>());
            var mockDataAssetService = Mock.Get(fixture.Freeze<IDataAssetService>());
            var mockReportsResponseFactory = Mock.Get(fixture.Freeze<IReportsResponseFactory>());
            var mockUserProfilePresenter = Mock.Get(fixture.Freeze<IUserProfilePresenter>());
            var mockAppInsightLogger = Mock.Get(fixture.Freeze<IAppInsightsLogger>());
            var mockEnumMemberConvertor = Mock.Get(fixture.Freeze<IEnumMemberConverter>());
            var mockValidationService = Mock.Get(fixture.Freeze<IModelValidationService>());
            var _loggerMock = new Mock<ILogger<DataAssetService>>();
            var _profiledDataAssetConverterPresenterMock = new Mock<IProfiledDataAssetConverterPresenter>();
            var _cddoDataAssetConverterMock = new Mock<ICddoDataAssetConverter>();
            var _ckanConnectionMock = new Mock<ICkanConnection>();
            var _validatedProfiledDataAssetSpreadsheetContentStoreMock = new Mock<IValidatedProfiledDataAssetSpreadsheetContentStore>();
            var _embeddedResourcesProviderMock = new Mock<IEmbeddedResourcesProvider>();
            var _profiledDataAssetsMigrationV1P0ToV3P1Mock = new Mock<IProfiledDataAssetsMigrationV1p0ToV3p1>();
            var _serviceOperationResultFactoryMock = new Mock<IServiceOperationResultFactory>();
            var _dataAssetDuplicationDeterminationMock = new Mock<IDataAssetDuplicationDetermination>();
            var _ckanConfigurationPresenterMock = new Mock<ICkanConfigurationPresenter>();
            var _appInsightsLoggerMock = new Mock<IAppInsightsLogger>();
            var _agmUserInformationBuilderMock = new Mock<IAgmUserInformationBuilder>();

            var _dataAssetService = new DataAssetService(
                _loggerMock.Object,
                _profiledDataAssetConverterPresenterMock.Object,
                _cddoDataAssetConverterMock.Object,
                _ckanConnectionMock.Object,
                _validatedProfiledDataAssetSpreadsheetContentStoreMock.Object,
                _embeddedResourcesProviderMock.Object,
                _profiledDataAssetsMigrationV1P0ToV3P1Mock.Object,
                _serviceOperationResultFactoryMock.Object,
                _dataAssetDuplicationDeterminationMock.Object,
                _ckanConfigurationPresenterMock.Object,
                _appInsightsLoggerMock.Object,
                _agmUserInformationBuilderMock.Object
            );

            ConfigureHappyPathTesting();

            return new TestItems(fixture, _dataAssetService,
                 mockReportsService, mockReportsResponseFactory, mockUserProfilePresenter, mockDataAssetService, mockValidationService
                , _loggerMock, _profiledDataAssetConverterPresenterMock, _cddoDataAssetConverterMock, _ckanConnectionMock, _validatedProfiledDataAssetSpreadsheetContentStoreMock,
                _embeddedResourcesProviderMock, _profiledDataAssetsMigrationV1P0ToV3P1Mock, _serviceOperationResultFactoryMock, _dataAssetDuplicationDeterminationMock, _ckanConfigurationPresenterMock,
                _appInsightsLoggerMock, _agmUserInformationBuilderMock);

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
                    .ReturnsAsync(userProfile);
            }
        }
    }

    public class TestItems(
        IFixture fixture,
        DataAssetService dataAssetService,
        Mock<IReportsService> mockReportsService,
        Mock<IReportsResponseFactory> mockReportsResponseFactory,
        Mock<IUserProfilePresenter> mockUserPresenter,
        Mock<IDataAssetService> mockDataAssetService,
        Mock<IModelValidationService> mockModelValidationService,
        Mock<ILogger<DataAssetService>> _loggerMock,
        Mock<IProfiledDataAssetConverterPresenter> profiledDataAssetConverterPresenterMock,
        Mock<ICddoDataAssetConverter> cddoDataAssetConverterMock,
        Mock<ICkanConnection> ckanConnectionMock,
        Mock<IValidatedProfiledDataAssetSpreadsheetContentStore> validatedProfiledDataAssetSpreadsheetContentStoreMock,
        Mock<IEmbeddedResourcesProvider> embeddedResourcesProviderMock,
        Mock<IProfiledDataAssetsMigrationV1p0ToV3p1> profiledDataAssetsMigrationV1P0ToV3P1Mock,
        Mock<IServiceOperationResultFactory> serviceOperationResultFactoryMock,
        Mock<IDataAssetDuplicationDetermination> dataAssetDuplicationDeterminationMock,
        Mock<ICkanConfigurationPresenter> ckanConfigurationPresenterMock,
        Mock<IAppInsightsLogger> appInsightsLoggerMock,
        Mock<IAgmUserInformationBuilder> agmUserInformationBuilderMock

       )
    {
        public IFixture Fixture { get; } = fixture;
        public DataAssetService DataAssetService { get; } = dataAssetService;
        public Mock<ILogger<DataAssetService>> LoggerMock { get; } = _loggerMock;
        public Mock<IReportsService> MockReportsService { get; } = mockReportsService;
        public Mock<IReportsResponseFactory> MockReportsResponseFactory { get; } = mockReportsResponseFactory;
        public Mock<IUserProfilePresenter> MockUserPresenter { get; } = mockUserPresenter;
        public Mock<IDataAssetService> MockDataAssetService { get; } = mockDataAssetService;
        public Mock<IModelValidationService> MockModelValidationService { get; } = mockModelValidationService;
        public Mock<IProfiledDataAssetConverterPresenter> _profiledDataAssetConverterPresenterMock { get; } = profiledDataAssetConverterPresenterMock;
        public Mock<ICddoDataAssetConverter> _cddoDataAssetConverterMock { get; } = cddoDataAssetConverterMock;
        public Mock<ICkanConnection> _ckanConnectionMock { get; } = ckanConnectionMock;
        public Mock<IValidatedProfiledDataAssetSpreadsheetContentStore> _validatedProfiledDataAssetSpreadsheetContentStoreMock { get; } = validatedProfiledDataAssetSpreadsheetContentStoreMock;
        public Mock<IEmbeddedResourcesProvider> _embeddedResourcesProviderMock { get; } = embeddedResourcesProviderMock;
        public Mock<IProfiledDataAssetsMigrationV1p0ToV3p1> _profiledDataAssetsMigrationV1P0ToV3P1Mock { get; } = profiledDataAssetsMigrationV1P0ToV3P1Mock;
        public Mock<IServiceOperationResultFactory> _serviceOperationResultFactoryMock { get; } = serviceOperationResultFactoryMock;
        public Mock<IDataAssetDuplicationDetermination> _dataAssetDuplicationDeterminationMock { get; } = dataAssetDuplicationDeterminationMock;
        public Mock<ICkanConfigurationPresenter> _ckanConfigurationPresenterMock { get; } = ckanConfigurationPresenterMock;
        public Mock<IAppInsightsLogger> _appInsightsLoggerMock { get; } = appInsightsLoggerMock;
        public Mock<IAgmUserInformationBuilder> _agmUserInformationBuilderMock { get; } = agmUserInformationBuilderMock;
    }
}
