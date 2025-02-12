using Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionSets;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare
{
    public class RequestTasksModel : PageModel
    {
        public required Guid DataShareRequestId { get; init; }
        public required string DataShareRequestRequestId { get; init; }
        public required string EsdaName { get; init; }
        public Guid DatasetId { get; init; }
        public required QuestionSetSummary QuestionSetSummary { get; init; }
        public required DataShareRequestAuditLog DataShareRequestAuditLog { get; init; }
        public required bool UserCanDeleteDataShareRequest { get; init; }
    }
}
