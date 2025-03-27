using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Shared
{
    public class HeaderPartialModel : PageModel
    {
        public string? BaseUrl { get; set; }

        public HeaderPartialModel(string? baseUrl = "") 
        {
            BaseUrl = baseUrl;
        }
        public void OnGet()
        {
            // Read the cookie value
            if (Request.Cookies.ContainsKey("BaseUrl"))
            {
                BaseUrl = Request.Cookies["BaseUrl"];
            }
            else
            {
                BaseUrl = "https://www.gov.uk";
            }
        }
    }
}
