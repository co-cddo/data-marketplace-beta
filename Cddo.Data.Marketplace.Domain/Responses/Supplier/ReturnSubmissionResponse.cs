using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests.Decisions;
using Agrimetrics.DataShare.Api.Dto.Responses.Notifications;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class ReturnSubmissionResponse
{
    public Guid DataShareRequestId { get; set; }

    public ReturnedDecisionSummary ReturnedDecisionSummary { get; set; }

    public NotificationSuccess NotificationSuccess { get; set; }
}