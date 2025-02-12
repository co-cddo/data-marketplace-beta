using Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial
{
    public class AuditLogReturnCommentsModel : PageModel
    {
        public DataShareRequestAuditLog DataShareRequestAuditLog { get; init; }
    }
}
