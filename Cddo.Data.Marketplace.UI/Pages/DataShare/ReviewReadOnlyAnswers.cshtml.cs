using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare
{
    public class ReviewReadOnlyAnswersModel : PageModel
    {
        public required Guid DataShareRequestId { get; init; }

        public required DataShareRequestAnswersSummary AnswersSummary { get; init; }

        public required bool UserCanDeleteDataShareRequest { get; init; }
    }
}
