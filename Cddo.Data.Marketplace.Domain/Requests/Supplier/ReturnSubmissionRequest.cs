using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Supplier;

public class ReturnSubmissionRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }

    [Required]
    public string CommentsToAcquirer { get; set; }
}