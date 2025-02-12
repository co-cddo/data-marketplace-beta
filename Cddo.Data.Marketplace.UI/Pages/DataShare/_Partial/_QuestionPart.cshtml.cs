using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial
{
    public class QuestionPartModel : PageModel
    {
        public required Guid QuestionPartId { get; init; }

        public required QuestionPartResponseFormatType ResponseFormat { get; init; }

        public required int OrderWithinQuestion { get; init; }

        public required string? QuestionText { get; init; }

        public required string? HintText { get; set; }

        public required bool QuestionIsOptional { get; init; }

        public required bool MultipleResponsesAreAllowed { get; init; }

        public required string? ItemDescriptionIfMultipleResponsesAreAllowed { get; init; }

        public required List<QuestionPartResponseModel> QuestionPartResponses { get; init; }

        public string? inputAreaID { get; init; }
    }
}
