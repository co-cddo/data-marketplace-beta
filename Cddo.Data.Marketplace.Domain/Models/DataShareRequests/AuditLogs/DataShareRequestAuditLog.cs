namespace Agrimetrics.DataShare.Api.Dto.Models.AuditLogs
{
    public class DataShareRequestAuditLog
    {
        public Guid DataShareRequestId { get; set; }

        public List<DataShareRequestAuditLogEntry> AuditLogEntries { get; set; } = [];
    }
}
