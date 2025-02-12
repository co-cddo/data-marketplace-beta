using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.ResponseCompression;
using Cddo.Data.Marketplace.Logic.Services;
using System.IO.Compression;
using Agm.Catalog.DotNet.Logic.DependencyInjection;
using Azure.Identity;
using Cddo.Data.Marketplace.Logic.Configuration;
using Cddo.Data.Marketplace.UI.Builders;
using Cddo.Data.Marketplace.Logic.DependencyInjection;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Services;
using Cddo.Data.Marketplace.UI.Model.Countries;

namespace Cddo.Data.Marketplace.UI.Configuration;

public static class ServiceRegistrations
{
    private static readonly IEnumerable<string> ResponseCompressionMimeTypes = new[]{
        "image/svg+xml",
        "application/javascript",
        "application/json",
        "application/xml",
        "text/css",
        "text/html",
        "text/json",
        "text/plain",
        "text/xml"
    };

    public static void AddServiceRegistrations(this WebApplicationBuilder builder)
    {
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        builder.Services.AddScoped<ICatalogDataService, CatalogDataService>();
        builder.Services.AddScoped<IDataShareRequestService, DataShareRequestService>();
        builder.Services.AddScoped<IQuestionDataBuilder, QuestionDataBuilder>();
        builder.Services.AddScoped<ICatalogQuestionsService, CatalogQuestionsService>();
        builder.Services.AddScoped<IDataShareRequestService, DataShareRequestService>();
        builder.Services.AddScoped<ICatalogSpreadsheetService, CatalogSpreadsheetService>();
        builder.Services.AddScoped<ICatalogReportsService, CatalogReportsService>();
        builder.Services.AddScoped<IUserRoleClaimService, UserRoleClaimService>();
        builder.Services.AddScoped<IRequestAccessService, RequestAccessService> ();
        builder.Services.AddScoped<IConfigurationKeys, UiConfigurationKeys>();
        builder.Services.AddScoped<ICountrySelectionPresenter, CountrySelectionPresenter>();
        builder.Services.AddScoped<IDeveloperService, DeveloperService>();
        builder.Services.AddLogicDependencies();
        builder.Services.RegisterAgmCatalogDotNetDependencies();

        //Manage
        builder.Services.AddScoped<IManageOrganisationService, ManageOrganisationService>();
        builder.Services.AddScoped<IManageOrganisationsService, ManageOrganisationsService>();
        builder.Services.AddScoped<IUserRoleService, UserRoleService>();
        builder.Services.AddScoped<IManageDepartmentsService, ManageDepartmentsService>();

        builder.Services
          .AddMvc(options => options.EnableEndpointRouting = false)
          .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix);
    }

    public static void AddResponseCompression(this WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.EnableForHttps = true;
                options.MimeTypes =
                    ResponseCompressionDefaults.MimeTypes.Concat(ResponseCompressionMimeTypes);
            })
            .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
            .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
    }

    public static DefaultAzureCredentialOptions AddAzureCredentialOptions(this WebApplicationBuilder builder)
    {      
        if (builder.Environment.IsDevelopment())
        {
            return new DefaultAzureCredentialOptions
            {
                ExcludeAzureCliCredential = true,
                ExcludeAzureDeveloperCliCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeEnvironmentCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeManagedIdentityCredential = true
            };
        }
        return new DefaultAzureCredentialOptions
        {
            ExcludeAzureCliCredential = true,
            ExcludeAzureDeveloperCliCredential = true,
            ExcludeAzurePowerShellCredential = true,
            ExcludeEnvironmentCredential = true,
            ExcludeInteractiveBrowserCredential = true,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeWorkloadIdentityCredential = true
        };
    }
}
