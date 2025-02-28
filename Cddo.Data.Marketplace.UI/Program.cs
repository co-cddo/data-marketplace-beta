using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using GovUk.Frontend.AspNetCore;
using System.Net;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;
using System.Text;
using HealthChecks.SqlServer;
using System.Security.Cryptography.X509Certificates;
using IdentityModel.Client;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Cddo.Data.Marketplace.Logic.Services.Audit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Cddo.Data.Marketplace.UI.Controllers;
using static Cddo.Data.Marketplace.Audit.EventTypes;
using Cddo.Data.Marketplace.UI.Model;

var builder = WebApplication.CreateBuilder(args);

// Add AGMTokenService (Singleton) for Custom Token Generation
builder.Services.AddSingleton<AGMTokenService>();

// Ensure TokenService is also Registered for Other Parts of the App
builder.Services.AddScoped<TokenService>(); // You still need TokenService for IAuthorizationHandler

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddTransient<IAuthorizationHandler, CustomAuthorizationHandler>();

builder.Services.Configure<HotjarSettings>(builder.Configuration.GetSection("HotjarSettings"));
builder.Services.AddAuthorization();


//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.Listen(System.Net.IPAddress.Any, 443, listenOptions =>
//    {
//        listenOptions.UseHttps(httpsOptions =>
//        {
//            httpsOptions.ServerCertificate = LoadCertificateFromStore("85ef80e0dbc43ff0cf30656fc4f9f384d3e0b011");
//        });
//    });
//});

//X509Certificate2 LoadCertificateFromStore(string thumbprintOrSubjectName)
//{
//    using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
//    {
//        store.Open(OpenFlags.ReadOnly);
//        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrSubjectName, validOnly: false);
//        if (certificates.Count > 0)
//        {
//            return certificates[0];
//        }
//        else
//        {
//            throw new FileNotFoundException($"Certificate not found for {thumbprintOrSubjectName}.");
//        }
//    }
//}

builder.Services.AddRazorPages();
builder.Services.AddSession();
builder.Services.AddGovUkFrontend();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.AddServiceRegistrations();
builder.AddResponseCompression();
builder.Logging.AddDebug();
builder.Logging.AddConsole();

builder.Services.AddAntiforgery();
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHealthCheckPublisher, ApplicationInsightsHealthCheckPublisher>();
builder.Services.Configure<HealthCheckPublisherOptions>(options =>
{
    options.Delay = TimeSpan.FromSeconds(2);
    options.Period = TimeSpan.FromMinutes(1);
    options.Timeout = TimeSpan.FromSeconds(30);
    options.Predicate = (check) => true;
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear(); // Clear known networks to avoid IP address issues
    options.KnownProxies.Clear();  // Clear known proxies to avoid proxy address issues
});
builder.Services.AddScoped<AppInsightsLogger>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LogoutPath = new PathString("/auth/signout");
    options.LoginPath = new PathString("/auth/signin");
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = DateTimeOffset.UtcNow.Date.AddDays(1).AddSeconds(-1) - DateTimeOffset.UtcNow; 
    options.SlidingExpiration = false;
    options.AccessDeniedPath = "/Auth/SignIn";
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.Redirect("/auth/signin");
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/auth/denied");
            return Task.CompletedTask;
        }
    };
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Authority = builder.Configuration["Authentication:Authority"];
    options.ClientId = builder.Configuration["Authentication:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
    options.ResponseType = "code"; // Use authorization code flow
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.SaveTokens = false; // Save tokens from IDP
    options.GetClaimsFromUserInfoEndpoint = true;
    options.CallbackPath = "/signin-oidc";
    options.SignedOutRedirectUri = builder.Configuration["BaseUrl"] + "auth/signout";

    // Handle events and ensure correct RedirectUri
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Ensure the RedirectUri uses the correct custom domain
            var baseUrl = builder.Configuration["BaseUrl"].TrimEnd('/'); // Remove any trailing slash from BaseUrl
            context.ProtocolMessage.RedirectUri = $"{baseUrl}{options.CallbackPath}";
            return Task.CompletedTask;
        },
        OnTokenValidated = async ctx =>
        {
            var idToken = ctx.SecurityToken as JwtSecurityToken;

            if (idToken == null)
            {
                throw new SecurityTokenException("ID Token not found.");
            }

            // Extract email from the IDP token
            var email = idToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            var name = idToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value
                        ?? idToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
          
            //    var myServiceLogger = ctx.HttpContext.RequestServices.GetRequiredService<AppInsightsLogger>();
            //    var userProfileService = ctx.HttpContext.RequestServices.GetRequiredService<IUserRoleService>();
            ////Get user profile
            //var userprofile = await userProfileService.GetUserProfileAsync();
            //var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userprofile);
               
            //myServiceLogger.LogUserEvent(EventTypes.UserEvent.UserLogin, "Login", "CDDO", userEventProperties);

            if (!string.IsNullOrEmpty(email))
            {
                // Generate custom token with the user's email
                var tokenService = ctx.HttpContext.RequestServices.GetRequiredService<AGMTokenService>();
                var customToken = tokenService.GenerateCustomIdToken(email, name);

                // Store the custom token in a secure cookie
                ctx.HttpContext.Response.Cookies.Append("CO-Datamarketplace", customToken, new CookieOptions
                {
                    HttpOnly = true,  // Prevent JavaScript access
                    Secure = true,    // Ensure the cookie is only sent over HTTPS
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.Date.AddDays(1).AddSeconds(-1), // Expiration at midnight
                    Path = "/",       // Make the cookie accessible throughout the app
                });

                // Optionally log this action for debugging purposes
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                if (!string.IsNullOrEmpty(customToken))
                {
                    var httpClientFactory = ctx.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient();

                    var usersApiUrl = builder.Configuration["ApiSettings:UsersAPI"] + "User/updateLastLogin";

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, usersApiUrl);
                    requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", customToken);

                    // Send request to update last login
                    var response = await httpClient.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Log the error, or take appropriate action
                        throw new Exception("Error updating last login: " + response.ReasonPhrase);
                    }

                    var userssignin = builder.Configuration["ApiSettings:UsersAPI"] + "User/SignInOrUpdateUser";
                    var signinorupdate = new HttpRequestMessage(HttpMethod.Post, userssignin);
                    signinorupdate.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    signinorupdate.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", customToken);

                    var signedin = await httpClient.SendAsync(signinorupdate);
                    if (!response.IsSuccessStatusCode)
                    {
                        // Log the error, or take appropriate action
                        throw new Exception("Error signing in or updating user: " + response.ReasonPhrase);
                    }
                    var _logger = ctx.HttpContext.RequestServices.GetRequiredService<AppInsightsLogger>();
                    var eventProperties = await AuditUtility.ParseResponseToDictionary(signedin);
                    _logger.LogUserEvent(UserEvent.UserLogin, "Login", "CDDO", eventProperties);
                }
            }
        },
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Redirect(builder.Configuration["BaseUrl"] + "auth/signin");
                context.HandleResponse();
                return Task.CompletedTask;
            }
            else
            {
                var myServiceLogger = context.HttpContext.RequestServices.GetRequiredService<AppInsightsLogger>();
                var additionalProperties = new Dictionary<string, string>
                {
                    { "user", $@"{{
                        ""userId"": ""-1"",
                        ""userName"": ""No User"",
                        ""userEmail"": """"
                                }}"} };

                myServiceLogger.LogUserEvent(EventTypes.UserEvent.UserFailedLoginAttempt, "Login", "CDDO", additionalProperties);

            }
            return Task.CompletedTask;
        }
    };
});



builder.Services.AddHealthChecks()
    .AddCheck("UI Health Check", () => HealthCheckResult.Healthy("UI is up and running."));

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/auth/signin");
    options.Conventions.AllowAnonymousToPage("/auth/signout");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/accessibility");
    options.Conventions.AllowAnonymousToPage("/cookies");
    options.Conventions.AllowAnonymousToPage("/cookie-details");
    options.Conventions.AllowAnonymousToPage("/privacy");
    options.Conventions.AllowAnonymousToFolder("/Error");
    options.Conventions.AllowAnonymousToFolder("/organisationaccess");
    options.Conventions.AllowAnonymousToFolder("/support");
    options.Conventions.AllowAnonymousToPage("/healthchecks");
    options.Conventions.AuthorizeFolder("/", "CustomPolicy");
});

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



builder.Services.AddSingleton<AppLifeTimeService>();
builder.Services.AddTransient<MockJwtDelegatingHandler>();
builder.Services.AddHttpClient("ApiClient")
    .AddHttpMessageHandler<MockJwtDelegatingHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomPolicy", policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
Cddo.Data.Marketplace.Audit.LoggerExtensions.Initialize(builder.Configuration);

var app = builder.Build();
app.UseForwardedHeaders();
var appLifetimeService = app.Services.GetRequiredService<AppLifeTimeService>();

appLifetimeService.StartupTime = DateTime.UtcNow;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseStatusCodePagesWithRedirects("/Error/{0}");
}
else
{
    IdentityModelEventSource.ShowPII = true;
    app.UseExceptionHandler(options =>
    {
        options.Run(
            async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "text/html";
                var ex = context.Features.Get<IExceptionHandlerFeature>();
                if (ex != null)
                {
                    var err = $"<h1>Error: {ex.Error.Message}</h1>{ex.Error.StackTrace}";
                    await context.Response.WriteAsync(err, CancellationToken.None).ConfigureAwait(false);
                }
            });
    });
}

// Assuming 'Configuration' is your IConfiguration instance.
var cspOptions = builder.Configuration.GetSection("ContentSecurityPolicy")
                              .Get<ContentSecurityPolicyOptions>();

var policyCollection = new HeaderPolicyCollection()
    .AddContentSecurityPolicy(innerBuilder =>
    {
        var defaultSrc = innerBuilder.AddDefaultSrc();
        foreach (var src in cspOptions.DefaultSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                defaultSrc.Self();
            else
                defaultSrc.From(src);
        }

        var scriptSrc = innerBuilder.AddScriptSrc();
        foreach (var src in cspOptions.ScriptSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                scriptSrc.Self();
            else if (src.Equals("unsafe-inline", StringComparison.OrdinalIgnoreCase))
                scriptSrc.UnsafeInline();
            else
                scriptSrc.From(src);
        }

        var connectSrc = innerBuilder.AddConnectSrc();
        foreach (var src in cspOptions.ConnectSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                connectSrc.Self();
            else
                connectSrc.From(src);
        }

        var imgSrc = innerBuilder.AddImgSrc();
        foreach (var src in cspOptions.ImgSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                imgSrc.Self();
            else if (src.Equals("data", StringComparison.OrdinalIgnoreCase))
                imgSrc.Data();
            else
                imgSrc.From(src);
        }

        var styleSrc = innerBuilder.AddStyleSrc();
        foreach (var src in cspOptions.StyleSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                styleSrc.Self();
            else if (src.Equals("unsafe-inline", StringComparison.OrdinalIgnoreCase))
                styleSrc.UnsafeInline();
            else
                styleSrc.From(src);
        }

        var fontSrc = innerBuilder.AddFontSrc();
        foreach (var src in cspOptions.FontSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                fontSrc.Self();
            else
                fontSrc.From(src);
        }

        var manifestSrc = innerBuilder.AddManifestSrc();
        foreach (var src in cspOptions.ManifestSrc)
        {
            if (src.Equals("self", StringComparison.OrdinalIgnoreCase))
                manifestSrc.Self();
            else
                manifestSrc.From(src);
        }
    });


app.UseSecurityHeaders(policyCollection);
app.UseResponseCompression();
app.UseAntiforgery();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseCors("AllowSpecificOrigin");

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseMiddleware<UserValidationMiddleware>();
app.UseMiddleware<TokenExpirationMiddleware>();
app.UseMiddleware<TestUserMiddleware>();
app.UseMiddleware<JwtToBearerMiddleware>();



app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
    {
        await next();
        return;
    }

    var isHomePage = context.Request.Path.Equals("/"); 

    var oldCookiesToRemove = new List<string>
    {
        ".AspNetCore.Cookies",    
        ".AspNet.ApplicationCookie", 
        ".AspNet.Cookies"         
    };

    var customCookie = context.Request.Cookies["CO-Datamarketplace"];
    bool customCookieExists = !string.IsNullOrEmpty(customCookie);

    if (!customCookieExists && context.User.Identity.IsAuthenticated)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("User authenticated via old cookie without custom token, clearing old cookies.");

        foreach (var cookieName in oldCookiesToRemove)
        {
            if (context.Request.Cookies.ContainsKey(cookieName))
            {
                logger.LogInformation($"Clearing old cookie: {cookieName}");
                context.Response.Cookies.Delete(cookieName);
            }
        }

        if (!isHomePage)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = context.Request.Path 
            });

            return;
        }
    }

    if (!context.User.Identity.IsAuthenticated && !isHomePage)
    {
        await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
        {
            RedirectUri = context.Request.Path // Redirect back to the original URL after login
        });

        return; 
    }

    await next();

    if (context.Response.StatusCode == 403)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect($"{builder.Configuration["BaseUrl"]}auth/signin");
            return;
        }

        context.Response.Redirect($"{builder.Configuration["BaseUrl"]}Error/NotAllowedYet");
        return;
    }

    if (context.Response.StatusCode == 401 && !isHomePage)
    {
        context.Response.Redirect($"{builder.Configuration["BaseUrl"]}auth/signin");
        return;
    }
});


app.UseCookiePolicy(new CookiePolicyOptions
{
    Secure = CookieSecurePolicy.Always
});
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
