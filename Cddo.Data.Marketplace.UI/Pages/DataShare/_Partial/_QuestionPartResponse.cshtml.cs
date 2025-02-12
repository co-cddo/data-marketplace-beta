using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;
using Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial
{
    public class QuestionPartResponseModel : PageModel
    {
        public required Guid QuestionPartId { get; init; }

        public required int ResponseNumber { get; init; }
        
        public required QuestionPartResponseFormatType ResponseFormat { get; init; }

        public required bool MultipleResponsesAreAllowed { get; init; }

        public required string? ResponseItemDescriptionIfMultipleResponsesAreAllowed { get; init; }

        public required bool AttachRemoveButton { get; set; }

        public List<string> ValidationErrors { get; init; }

        public required QuestionPartResponseItemModel ResponseItem { get; init; }

        public required int MaxResponseLength { get; init; }

        public string?inputAreaID { get; init; }
    }
}
