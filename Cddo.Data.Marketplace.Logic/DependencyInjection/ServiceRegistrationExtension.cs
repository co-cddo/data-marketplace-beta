using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Configuration;
using Cddo.Data.Marketplace.Logic.Services.Users.Conversion;
using Cddo.Data.Marketplace.Logic.Services.Users.UserIdPresentation;
using Microsoft.Extensions.DependencyInjection;

namespace Cddo.Data.Marketplace.Logic.DependencyInjection;

public static class ServiceRegistrationExtension
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddTransient<IAgmUserInformationBuilder, AgmUserInformationBuilder>();

        services.AddTransient<IDataAssetService, DataAssetService>();

        services.AddTransient<IReportsService, ReportsService>();
        services.AddTransient<IReportsResponseFactory, ReportsResponseFactory>();

        services.AddTransient<ICatalogReportsDataItemsBuilder, CatalogReportsDataItemsBuilder>();
        services.AddTransient<IReportFieldFilterConverter, ReportFieldFilterConverter>();

        services.AddTransient<IServiceOperationResultFactory, ServiceOperationResultFactory>();

        services.AddTransient<IUserProfilePresenter, UserProfilePresenter>();
        services.AddTransient<IUserIdPresenter, UserIdPresenter>();
        services.AddTransient<IUsersServiceConfigurationPresenter, UsersServiceConfigurationPresenter>();
        services.AddTransient<IDataShareRequestMailboxAddressValidation, DataShareRequestMailboxAddressValidation>();
        services.AddTransient<IAppInsightsLogger, AppInsightsLogger>();

        services.AddTransient<ICddoFlurlExceptionBuilder, CddoFlurlExceptionBuilder>();
        
        return services;
    }
}
