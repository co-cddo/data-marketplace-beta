using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class DeleteDataShareRequestRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }
}