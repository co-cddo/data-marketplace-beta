using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataRequest
{
    public class DeleteReadAnswersRequestModel : PageModel
    {
        public required Guid DataShareRequestId { get; init; }

        public required string DataShareRequestRequestId { get; init; }

        public required string EsdaName { get; init; }
    }
}
