namespace Agrimetrics.DataShare.Api.Dto.Models.AuditLogs;

public class DataShareRequestAuditLogEntryComment
{
    public int CommentOrder { get; set; }

    public string Comment { get; set; } = string.Empty;
}