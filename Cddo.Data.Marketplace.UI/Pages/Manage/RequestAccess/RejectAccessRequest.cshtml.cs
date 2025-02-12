using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Manage.RequestAccess
{
    public class RejectRequestConfirmationModel : PageModel
    {
        public int OrganisationRequestID {  get; set; }
    }
}
