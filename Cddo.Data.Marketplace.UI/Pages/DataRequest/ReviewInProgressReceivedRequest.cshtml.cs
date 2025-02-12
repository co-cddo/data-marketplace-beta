using Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataRequest
{
    public class ReviewInProgressReceivedRequestModel : PageModel
    {
        public required SubmissionDetails SubmissionDetails { get; init; }

        public required string SupplierNotes { get; init; }

        public required DataShareRequestAuditLog DataShareRequestAuditLog { get; init; }

    }
}
