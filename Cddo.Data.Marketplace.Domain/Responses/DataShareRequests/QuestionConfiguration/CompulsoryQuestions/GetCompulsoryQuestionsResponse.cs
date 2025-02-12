using Agrimetrics.DataShare.Api.Dto.Models.QuestionConfiguration.CompulsoryQuestions;

namespace Agrimetrics.DataShare.Api.Dto.Responses.QuestionConfiguration.CompulsoryQuestions;

public class GetCompulsoryQuestionsResponse
{
    public int RequestingUserId { get; set; }

    public CompulsoryQuestionSet CompulsoryQuestionSet { get; set; }
}