using Agrimetrics.DataShare.Api.Dto.Responses.Notifications;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class CancelDataShareRequestResponse
{
    public Guid DataShareRequestId { get; set; }
    
    public string ReasonsForCancellation { get; set; }

    public NotificationSuccess NotificationSuccess { get; set; }
}