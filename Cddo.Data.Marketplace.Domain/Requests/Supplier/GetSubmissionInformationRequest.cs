using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Supplier;

public class GetSubmissionInformationRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }
}