using Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare
{
    public class ReviewAnswersModel : PageModel
    {
        public required DataShareRequestAnswersSummary AnswersSummary { get; init; }

        public required DataShareRequestAuditLog DataShareRequestAuditLog { get; init; }
    }
}
