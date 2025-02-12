using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Supplier;

public class GetSubmissionReviewInformationRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }
}