using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.AuditLogs
{
    public class GetDataShareRequestAuditLogRequest
    {
        [Required]
        public Guid DataShareRequestId { get; set; }

        [Required]
        public List<DataShareRequestStatus> ToStatuses { get; set; } = [];
    }
}
