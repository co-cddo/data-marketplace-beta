using Cddo.Data.Marketplace.Api.Gateway.Boot;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureOcelot();

var services = builder.Services;
services.AddControllers();
services.RegisterServiceDependencies();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.LaunchOcelot();

// Call swagger methods after launching Ocelot, as part of that configures the usage of downstream swagger docs
app.UseSwagger();
app.UseSwaggerUI();

await app.RunAsync();
