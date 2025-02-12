using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataRequest
{
    public class RequestAcceptanceDeclarationModel : PageModel
    {
        public Guid DataShareRequestId { get; set; }

        public string FeedbackToAcquirer { get; set; }
    }
}
