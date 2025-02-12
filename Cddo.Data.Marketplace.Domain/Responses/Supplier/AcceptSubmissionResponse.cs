using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests.Decisions;
using Agrimetrics.DataShare.Api.Dto.Responses.Notifications;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class AcceptSubmissionResponse
{
    public Guid DataShareRequestId { get; set; }

    public AcceptedDecisionSummary AcceptedDecisionSummary { get; set; }

    public NotificationSuccess NotificationSuccess { get; set; }
}