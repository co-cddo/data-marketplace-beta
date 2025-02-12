using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class GetDataShareRequestQuestionInformationRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }

    [Required]
    public Guid QuestionId { get; set; }
}