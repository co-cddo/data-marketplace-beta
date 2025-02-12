using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class CancelDataShareRequestRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }

    public string ReasonsForCancellation { get; set; }
}