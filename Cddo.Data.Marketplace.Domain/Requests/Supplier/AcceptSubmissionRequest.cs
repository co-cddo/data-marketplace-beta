using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Supplier;

public class AcceptSubmissionRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }

    public string CommentsToAcquirer { get; set; }
}