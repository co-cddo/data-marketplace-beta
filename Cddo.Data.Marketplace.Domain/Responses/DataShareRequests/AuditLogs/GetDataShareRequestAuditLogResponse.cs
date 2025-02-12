using Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.AuditLogs
{
    public class GetDataShareRequestAuditLogResponse
    {
        public Guid DataShareRequestId { get; set; }

        public List<DataShareRequestStatus> ToStatuses { get; set; }

        public DataShareRequestAuditLog DataShareRequestAuditLog { get; set; }
    }
}
