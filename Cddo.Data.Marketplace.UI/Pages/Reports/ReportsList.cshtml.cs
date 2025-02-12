using Cddo.Data.Marketplace.Api.Dto.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Cddo.Data.Marketplace.UI.Pages.Reports
{
    public class ReportsListModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _usersAPI;
        public string idToken { get; set; }
        public UserProfile UserProfile { get; set; }
        public ReportsListModel(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _usersAPI = configuration["ApiSettings:UsersAPI"];
        }
            
        public async Task<IActionResult> OnGet()
        {

            // Retrieve the ID token stored in the current user's context
            idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
            if (string.IsNullOrEmpty(idToken))
            {
                idToken = HttpContext.Request.Cookies["CO-Datamarketplace"];
            }
            if (string.IsNullOrEmpty(idToken))
            {
                // Handle the case where the ID token is not available
                return RedirectToPage("/Error");
            }

            // Create a new HttpClient instance
            var httpClient = _clientFactory.CreateClient();

            // Set the Authorization header with the ID token as a Bearer token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

            // Make the POST request
            var emptyContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsJsonAsync($"{_usersAPI}User/userinfo", emptyContent);

            // Check if the response is successful
            if (!response.IsSuccessStatusCode)
            {
                // Handle HTTP error responses
                return RedirectToPage("/Error/403");
            }

            // Deserialize the response content to UserProfile
            UserProfile = await response.Content.ReadFromJsonAsync<UserProfile>();
            if (UserProfile == null)
            {
                // Handle the case where the user profile is not returned or is null
                return RedirectToPage("/Error/403");
            }
            var roles = new List<string> { "System Administrator", "Data Request Approver", "Metadata Publisher", "Organisation Administrator" };
            if(UserProfile.Roles != null && !UserProfile.Roles.Any(userRole => roles.Contains(userRole.RoleName)))
            {
                return RedirectToPage("/Error/403");
            }

            return Page();
        }
    }
}
