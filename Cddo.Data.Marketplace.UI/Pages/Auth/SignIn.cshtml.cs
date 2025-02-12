using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Auth
{
    public class SignInModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                // Redirect the user to the login page if not authenticated
                return Challenge(new AuthenticationProperties { RedirectUri = Url.Page("/Index") }, OpenIdConnectDefaults.AuthenticationScheme); // Use the correct scheme
            }

            // Redirect the user to the home page if already signed in
            return RedirectToPage("/Index");
        }
    }

}
