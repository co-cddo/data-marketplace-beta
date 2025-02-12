using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;

public class DataShareRequestAuditLogEntry
{
    public Guid DataShareRequestId { get; set; }

    public DataShareRequestStatus? FromStatus { get; set; }

    public DataShareRequestStatus? ToStatus { get; set; }

    public int ChangedByOrganisationId { get; set; }

    public int ChangedByDomainId { get; set; }

    public int ChangedByUserId { get; set; }

    public DateTime ChangedOnUtc { get; set; }

    public List<DataShareRequestAuditLogEntryComment> Comments { get; set; } = [];
}