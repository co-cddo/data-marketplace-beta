using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;
using Cddo.Data.Marketplace.UI.Pages.DataShare._Partial;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare
{
    public class QuestionModel : PageModel
    {
        public required Guid DataShareRequestId { get; init; }

        public required string DataShareRequestRequestId { get; init; }

        public required Guid QuestionId { get; init; }

        public required DataShareRequestQuestionFooter? Footer { get; init; }

        public required List<QuestionPartModel> QuestionParts { get; set; }
    }
}
