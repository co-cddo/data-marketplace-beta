using Cddo.Data.Marketplace.Api.DependencyInjection;
using Cddo.Data.Marketplace.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using Cddo.Data.Marketplace.Audit;
using AutoMapper;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Cddo.Data.Marketplace.Api.CustomErrors;
using Agm.Catalog.DotNet.Logic.DependencyInjection;
using Cddo.Data.Marketplace.Logic.Services.Users.Conversion;
using Cddo.Data.Marketplace.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// Define the test user flag
bool testUserEnabled = builder.Configuration.GetValue<bool>("TestUser:Enabled");
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Suppress automatic 400 responses from ModelState validation
        options.SuppressModelStateInvalidFilter = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CDDO Utilities", Version = "v1" });
    c.OperationFilter<SwaggerFileUploadOperation>();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
    c.CustomSchemaIds(type => type.FullName); 
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "InteractiveScheme"; // Default scheme for authentication
    options.DefaultAuthenticateScheme = "InteractiveScheme";
    options.DefaultChallengeScheme = "InteractiveScheme";
})
.AddJwtBearer("InteractiveScheme", options =>
{
    if (testUserEnabled)
    {
        // Test user validation settings
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])), // Test user's signing key
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    }
    else
    {
        // Normal validation settings for real users
        var secretKey = builder.Configuration["AGMJwtSettings:SecretKey"];
        var issuer = builder.Configuration["BaseUrl"];
        var previewIssuer = "https://preview.datamarketplace.gov.uk/"; // Add preview base URL

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), // Use the same key to sign the JWT

            ValidateIssuer = true,
            ValidIssuers = new[] // Allow both base URLs for issuer validation
            {
            issuer,
            previewIssuer
        },

            ValidateAudience = true,
            ValidAudiences = new[] // Allow both base and preview URLs for audience validation
            {
            $"{issuer}api",      // Base URL audience
            $"{previewIssuer}api" // Preview URL audience
        },

            ValidateLifetime = true, // Ensure the token has not expired
            ClockSkew = TimeSpan.Zero // Remove default clock skew
        };

        // Optionally handle token validation and error handling
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                if (context.Request.Path.StartsWithSegments("/datamarketplaceapi/datasets") ||
                context.Request.Path.StartsWithSegments("/clientauth/get-token"))
                {
                    var myServiceLogger = context.HttpContext.RequestServices.GetRequiredService<AppInsightsLogger>();
                    myServiceLogger.LogEventMain(EventTypes.ErrorEvent.AccessError, "Middleware", "CDDO", "Error", "IngestionApiError", "", new Dictionary<string, string>() { { "ErrorDetails", context.Exception.Message } });
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var utcNow = DateTime.UtcNow;
                var issuedAtClaim = context.Principal.FindFirstValue(JwtRegisteredClaimNames.Iat);

                if (issuedAtClaim != null)
                {
                    // Ensure the issued at claim is properly parsed as a Unix timestamp
                    var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(issuedAtClaim)).UtcDateTime;

                    if (issuedAt.Date != utcNow.Date)
                    {
                        context.Fail("Token was not issued today.");
                    }
                }
                

                return Task.CompletedTask;
            }
        };
    }
})
.AddJwtBearer("ApiAuthScheme", options =>
{
    // Validation for API requests
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Authentication:ApiKey"])), // Your API signing key
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Authentication:ApiIssuer"], // Your API issuer
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Authentication:ClientId"], // Your API audience
        ClockSkew = TimeSpan.Zero // Optional: Set clock skew to zero
    };
});

builder.Services.AddAuthorization(options =>
{
    // Policy for 'publish' scope
    options.AddPolicy("PublishScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "publish");
    });

    // Policy for 'discover' scope
    options.AddPolicy("DiscoverScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "discover");
    });

    // Policy for 'delete' scope
    options.AddPolicy("DeleteScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "delete");
    });
});

builder.Services.Configure<HealthCheckSettings>(builder.Configuration.GetSection("HealthCheckSettings"));

builder.Services.AddHealthChecks()
    .AddCheck<CatalogDataStoreHealthCheck>("catalog_data_store");


builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

string allowedOriginsConfig = builder.Configuration["CorsSettings:AllowedOrigins"];

// Check if allowedOriginsConfig is null or empty and handle it properly
if (string.IsNullOrEmpty(allowedOriginsConfig))
{
    throw new ArgumentNullException("CorsSettings:AllowedOrigins", "CORS settings for allowed origins are not defined.");
}

// Split the allowed origins string into an array
string[] allowedOrigins = allowedOriginsConfig.Split(",", StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins(allowedOrigins) // Use the array of allowed origins from CorsSettings
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});


builder.Services.AddBusinessLogic();
builder.Services.RegisterAgmCatalogDotNetDependencies();
builder.Services.AddTransient<IModelValidationService, ModelValidationService>();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<AppInsightsLogger>();
Cddo.Data.Marketplace.Audit.LoggerExtensions.Initialize(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Utilities API V1");
    });
}

// Middleware to handle the test user backdoor and set the id_token
app.Use(async (context, next) =>
{
    bool enableTestUserBackdoor = builder.Configuration.GetValue<bool>("TestUser:Enabled");
    if (enableTestUserBackdoor)
    {
        var idToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(idToken))
        {
            context.Request.Headers["Authorization"] = $"Bearer {idToken}";
        }
    }
    await next();
});

app.MapGet("/", () => Results.Ok("API is up and running."))
    .WithMetadata(new AllowAnonymousAttribute());

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.ToString(),
            details = report.Entries.Select(entry => new
            {
                key = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
                duration = entry.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsJsonAsync(result, new JsonSerializerOptions { WriteIndented = true });
    }
}).WithMetadata(new AllowAnonymousAttribute());

await app.RunAsync();
