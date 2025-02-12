using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats
{
    public abstract class QuestionPartResponseItemModel : PageModel
    {
        public required Guid QuestionPartId { get; init; }

        public required int QuestionPartNumber { get; init; }

        public required int ResponseNumber { get; init; }

        public required bool ResponseIsInvalid { get; init; }

        public string? inputAreaID { get; init; }

    }
}
