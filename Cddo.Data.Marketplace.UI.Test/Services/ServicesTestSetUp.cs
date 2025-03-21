using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.UI.Services;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Claims;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results.SpreadsheetIngestion.ValidatedDataAssetSpreadsheetItems;
using Cddo.Data.Marketplace.Logic.Services.Users;

namespace Cddo.Data.Marketplace.UI.Test.Services
{
    public static class ServicesTestSetUp
    {
        public static TestItems CreateTestItems(string? testBaseUrl = null)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var mockCataloguServiceLogger = Mock.Get(fixture.Create<ILogger<CatalogDataService>>());
            var mockCataloguQuestionServiceLogger = Mock.Get(fixture.Create<ILogger<CatalogQuestionsService>>());
            var mockCataloguReportsServiceLogger = Mock.Get(fixture.Create<ILogger<CatalogReportsService>>());
            var mockCatalogSpreadsheetServiceLogger = Mock.Get(fixture.Create<ILogger<CatalogSpreadsheetService>>());
            var mockDeveloperServiceLogger = Mock.Get(fixture.Create<ILogger<DeveloperService>>());
            var mockRequestAccessServiceLogger = Mock.Get(fixture.Create<ILogger<RequestAccessService>>());
            var mockUserRoleClaimServiceLogger = Mock.Get(fixture.Create<ILogger<UserRoleClaimService>>());
            var mockHttpContextAccessor = Mock.Get(fixture.Create<IHttpContextAccessor>());
            var mockCddoFlurlExceptionBuilder = Mock.Get(fixture.Create<ICddoFlurlExceptionBuilder>());
            var mockConfiguration = Mock.Get(fixture.Create<IConfiguration>());
            var mockUserRoleService = Mock.Get(fixture.Create<IUserRoleService>());
            var mockUserRoleClaimService = Mock.Get(fixture.Create<IUserRoleClaimService>());

            var mockValidatedDataAssetSpreadsheetItemSummaryBuilder = Mock.Get(fixture.Create<IValidatedDataAssetSpreadsheetItemSummaryBuilder>());
            var mockDataShareRequestMailboxAddressValidation = Mock.Get(fixture.Create<IDataShareRequestMailboxAddressValidation>());

            ConfigureHappyPathTesting();

            var catalogDataService = new CatalogDataService(
                mockCataloguServiceLogger.Object,
                mockConfiguration.Object,
                mockHttpContextAccessor.Object,
                mockCddoFlurlExceptionBuilder.Object);

            var catalogQuestionsService = new CatalogQuestionsService(
                mockCataloguQuestionServiceLogger.Object,
                mockConfiguration.Object,
                mockHttpContextAccessor.Object);

            var catalogReportService = new CatalogReportsService(
                mockCataloguReportsServiceLogger.Object,
                mockConfiguration.Object,
                mockHttpContextAccessor.Object,
                mockUserRoleService.Object);

            var catalogSpreadsheetService = new CatalogSpreadsheetService(
                mockCatalogSpreadsheetServiceLogger.Object,
                mockConfiguration.Object,
                mockHttpContextAccessor.Object,
                mockCddoFlurlExceptionBuilder.Object,
                mockValidatedDataAssetSpreadsheetItemSummaryBuilder.Object,
                mockDataShareRequestMailboxAddressValidation.Object
                );

            var developerService = new DeveloperService(
                mockHttpContextAccessor.Object,
                mockDeveloperServiceLogger.Object,
                mockUserRoleClaimService.Object,
                mockConfiguration.Object);

            var requestService = new RequestAccessService(
                mockRequestAccessServiceLogger.Object,
                mockConfiguration.Object,
                mockHttpContextAccessor.Object
                );

            var userRoleClaimService = new UserRoleClaimService(
                 mockHttpContextAccessor.Object,
                mockUserRoleClaimServiceLogger.Object,
                mockConfiguration.Object
                );

            return new TestItems(fixture,
                catalogDataService,
                mockHttpContextAccessor,
                catalogQuestionsService,
                catalogReportService,
                mockUserRoleService,
                catalogSpreadsheetService,
                mockValidatedDataAssetSpreadsheetItemSummaryBuilder,
                mockDataShareRequestMailboxAddressValidation,
                developerService,
                mockUserRoleClaimService,
                requestService,
                userRoleClaimService);

            void ConfigureHappyPathTesting()
            {
                SetTestBaseApiUrl(testBaseUrl ?? "http://xyz");

                SetupTestHttpContext(mockHttpContextAccessor, "");
            }

            void SetTestBaseApiUrl(
               string baseApiUrl)
            {
                var mockConfigurationSection = new Mock<IConfigurationSection>();
                mockConfigurationSection.SetupGet(x => x.Value)
                    .Returns(baseApiUrl);

                mockConfiguration.Setup(x => x.GetSection("Api:Main"))
                    .Returns(mockConfigurationSection.Object);

                mockConfiguration.Setup(x => x.GetSection("ApiSettings:UsersAPI"))
                    .Returns(mockConfigurationSection.Object);

                mockConfiguration.Setup(x => x.GetSection("Api:DataShare"))
                   .Returns(mockConfigurationSection.Object);
            }
        }

        public static void SetupTestHttpContext(
          Mock<IHttpContextAccessor> mockHttpContextAccessor,
          string? idTokenValue = null)
        {
            

            if (idTokenValue != null && !string.IsNullOrEmpty(idTokenValue))
            {
                var context = new DefaultHttpContext();
                context.Request.Headers["Authorization"] = $"{idTokenValue};";
                context.Request.Headers["Cookie"] = $"CO-Datamarketplace={idTokenValue}; CO-Datamarketplace={idTokenValue}";
                //context.Request.Cookies.Append("CO-Datamarketplace", idTokenValue);

                var claims = new List<Claim> { new Claim(ClaimTypes.Name, "TestUser") };
                var identity = new ClaimsIdentity(claims, "TestAuthType");
                var principal = new ClaimsPrincipal(identity);
                context.User = principal;

                mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);
            }

           
        }

    }

    public class TestItems(
        IFixture fixture,
        ICatalogDataService  catalogService,
         Mock<IHttpContextAccessor> mockHttpContextAccessor,
         ICatalogQuestionsService catalogQuestionService,
         ICatalogReportsService catalogReportService,
         Mock<IUserRoleService> mockUserRoleService,
         ICatalogSpreadsheetService catalogSpreadSheetService,
         Mock<IValidatedDataAssetSpreadsheetItemSummaryBuilder> validationDataAssertSpeadSheetItemSummaryBuilder,
         Mock<IDataShareRequestMailboxAddressValidation> mockDataShareRequestMailboxAddressValidation,
         IDeveloperService developerService,
         Mock<IUserRoleClaimService> mockUserRoleClaimService,
        IRequestAccessService requestAccessService,
        IUserRoleClaimService userRoleClaimService)
    {
        public IFixture Fixture { get; } = fixture;
        public ICatalogDataService CatalogService { get; } = catalogService;
        public ICatalogQuestionsService CatalogQuestionService { get; } = catalogQuestionService;
        public ICatalogReportsService CatalogReportsService { get; } = catalogReportService;
        public ICatalogSpreadsheetService CatalogSpreadsheetService { get; } = catalogSpreadSheetService;
        public IDeveloperService DeveloperService { get; } = developerService;
        public IRequestAccessService RequestAccessService { get; } = requestAccessService;
        public IUserRoleClaimService UserRoleClaimService { get; } = userRoleClaimService;
        public Mock<IHttpContextAccessor> MockHttpContextAccessor { get; } = mockHttpContextAccessor;
        public Mock<IUserRoleService> MockUserRoleService { get; } = mockUserRoleService;
        public Mock<IValidatedDataAssetSpreadsheetItemSummaryBuilder> MockValidatedDataAssetSpreadsheetItemSummaryBuilder { get; } = validationDataAssertSpeadSheetItemSummaryBuilder;
        public Mock<IDataShareRequestMailboxAddressValidation> MockDataShareRequestMailboxAddressValidation { get; } = mockDataShareRequestMailboxAddressValidation;
        public Mock<IUserRoleClaimService> MockUserRoleClaimService { get; } = mockUserRoleClaimService;

    }
}
