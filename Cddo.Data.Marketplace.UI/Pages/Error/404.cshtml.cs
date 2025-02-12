using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Error
{
    public class Error404Model : PageModel
    {
        public void OnGet()
        {
            Response.StatusCode = 404;
        }
    }
}
