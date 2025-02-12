using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class GetDataShareRequestQuestionInformationResponse
{
    public Guid DataShareRequestId { get; set; }

    public Guid QuestionId { get; set; }

    public DataShareRequestQuestion DataShareRequestQuestion { get; set; }
}