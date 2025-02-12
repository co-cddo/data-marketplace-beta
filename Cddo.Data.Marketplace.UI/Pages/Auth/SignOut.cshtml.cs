using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Microsoft.AspNetCore.Authorization;

namespace Cddo.Data.Marketplace.UI.Pages.Auth;

[AllowAnonymous]
public class SignOutModel : PageModel
{
    private IUserRoleService UserRoleService { get; }

    public SignOutModel(IUserRoleService userRoleService)
    {
        UserRoleService = userRoleService;
    }

    public async Task<IActionResult> OnGetSignOut()
    {
        // Log the user sign-out event if needed (audit log, etc.)
        try
        {
            var userRoleService = HttpContext.RequestServices.GetRequiredService<IUserRoleService>();
            UserProfile userProfile = await userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userProfile);
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<SignOutModel>>();
        }
        catch (Exception ex)
        {
            // Handle any exception during audit log
        }

        // Clear the custom token cookie
        HttpContext.Response.Cookies.Delete("CO-Datamarketplace");

        // Sign out the local cookie authentication scheme
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Optionally, redirect the user to the login page or home page after signing out
        return RedirectToPage("/Index");
    }
}
