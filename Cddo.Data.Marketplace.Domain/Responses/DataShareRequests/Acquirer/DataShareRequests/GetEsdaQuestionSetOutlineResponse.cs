using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionSets;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class GetEsdaQuestionSetOutlineResponse
{
    public Guid EsdaId { get; set; }

    public QuestionSetOutline QuestionSetOutline { get; set; }
}