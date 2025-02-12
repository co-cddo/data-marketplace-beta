using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace Cddo.Data.Marketplace.Api.Gateway.Boot
{
    public static class OcelotConfiguration
    {
        private const string ocelotConfigurationFileName = "ocelot.json";
        private const string defaultAuthenticationScheme = JwtBearerDefaults.AuthenticationScheme; // 'Bearer'

        public static void ConfigureOcelot(this WebApplicationBuilder builder)
        {
            AddConfiguration();
            AddDownstreamSwaggerConfiguration();
            AddServices();

            void AddConfiguration()
            {
                builder.Configuration.AddJsonFile(ocelotConfigurationFileName, optional: false, reloadOnChange: true);
            }

            void AddDownstreamSwaggerConfiguration()
            {
                builder.Services.AddSwaggerForOcelot(builder.Configuration);
            }

            void AddServices()
            {
                builder.Services.AddOcelot(builder.Configuration);
            }
        }

        public static void LaunchOcelot(this WebApplication app)
        {
            app.UseSwaggerForOcelotUI(opt =>
            {
                opt.PathToSwaggerGenerator = "/swagger/docs";
            });

            app.UseOcelot().Wait();
        }
    }
}
