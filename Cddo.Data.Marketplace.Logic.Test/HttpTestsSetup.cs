using AutoFixture.AutoMoq;
using AutoFixture;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.Audit;

namespace Cddo.Data.Marketplace.Logic.Test
{
    public static class HttpTestsSetup
    {
        public static HttpTest CreateTestItems(string? testBaseUrl = null)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());

            var mockLogger = Mock.Get(fixture.Create<ILogger<DataShareRequestService>>());
            var mockConfiguration = Mock.Get(fixture.Create<IConfiguration>());
            var mockHttpContextAccessor = Mock.Get(fixture.Create<IHttpContextAccessor>());
            var mockUserRoleService = Mock.Get(fixture.Create<IUserRoleService>());
            var mockHttpClientFactory = Mock.Get(fixture.Create<IHttpClientFactory>());
            var mockAppInsightsLogger = Mock.Get(fixture.Create<IAppInsightsLogger>());

            ConfigureHappyPathTesting();

            var dataShareRequestService = new DataShareRequestService(
                mockLogger.Object,
                mockConfiguration.Object,
                mockHttpContextAccessor.Object,
                mockUserRoleService.Object);

            var userRoleService = new UserRoleService(
                mockHttpClientFactory.Object,
                mockConfiguration.Object,
                mockAppInsightsLogger.Object,
                mockHttpContextAccessor.Object);

            var manageOrganisationService = new ManageOrganisationService(
                     mockAppInsightsLogger.Object,
                      mockConfiguration.Object,
                     mockHttpContextAccessor.Object,
                     mockUserRoleService.Object
                );

            var manageDepartmentService = new ManageDepartmentsService(
                    mockAppInsightsLogger.Object,
                     mockConfiguration.Object,
                    mockHttpContextAccessor.Object,
                    mockUserRoleService.Object
               );

            var manageOrganisationsService = new ManageOrganisationsService(
                mockAppInsightsLogger.Object,
                     mockConfiguration.Object,
                    mockHttpContextAccessor.Object
                );

            return new HttpTest(
                fixture,
                dataShareRequestService,
                mockHttpContextAccessor,
                mockUserRoleService,
                userRoleService,
                mockHttpClientFactory,
                manageOrganisationService,
                manageDepartmentService,
                manageOrganisationsService);

            void ConfigureHappyPathTesting()
            {
                SetTestBaseApiUrl(testBaseUrl ?? "http://xyz");

                SetupTestHttpContext(mockHttpContextAccessor, "_");
            }

            void SetTestBaseApiUrl(
                string baseApiUrl)
            {
                var mockConfigurationSection = new Mock<IConfigurationSection>();
                mockConfigurationSection.SetupGet(x => x.Value)
                    .Returns(baseApiUrl);

                mockConfiguration.Setup(x => x.GetSection("Api:DataShare"))
                    .Returns(mockConfigurationSection.Object);

                mockConfiguration.Setup(x => x.GetSection("ApiSettings:UsersAPI"))
                    .Returns(mockConfigurationSection.Object);
            }

        }

        public static void SetupTestHttpContext(
           Mock<IHttpContextAccessor> mockHttpContextAccessor,
           string? idTokenValue = null)
        {
            var context = new DefaultHttpContext();

            if (idTokenValue != null)
            {
                context.Request.Headers["Cookie"] = $"CO-Datamarketplace={idTokenValue}; AnotherTestCookie=AnotherTestValue";
            }

            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);
        }

        public class HttpTest(
            IFixture fixture,
            IDataShareRequestService dataShareRequestService,
            Mock<IHttpContextAccessor> mockHttpContextAccessor,
            Mock<IUserRoleService> mockUserRoleService,
            IUserRoleService userRoleService,
            Mock<IHttpClientFactory> mockHttpClientFactory,
            IManageOrganisationService manageOrganisationService,
            IManageDepartmentsService manageDepartmentService,
            IManageOrganisationsService manageOrganisationsService)
        {
            public IFixture Fixture { get; } = fixture;
            public IDataShareRequestService DataShareRequestService { get; } = dataShareRequestService;
            public Mock<IHttpContextAccessor> MockHttpContextAccessor { get; } = mockHttpContextAccessor;
            public Mock<IUserRoleService> MockUserRoleService { get; } = mockUserRoleService;
            public Mock<IHttpClientFactory> MockHttpClientFactory { get; } = mockHttpClientFactory;
            public IUserRoleService UserRoleService { get; } = userRoleService;
            public IManageOrganisationService ManageOrganisationService { get; } = manageOrganisationService;
            public IManageDepartmentsService ManageDepartmentService { get; } = manageDepartmentService;
            public IManageOrganisationsService ManageOrganisationsService { get; } = manageOrganisationsService;
        }
    }
}
