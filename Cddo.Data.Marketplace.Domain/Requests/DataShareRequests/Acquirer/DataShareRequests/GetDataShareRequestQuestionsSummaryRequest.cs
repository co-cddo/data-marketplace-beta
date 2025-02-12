using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class GetDataShareRequestQuestionsSummaryRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }
}