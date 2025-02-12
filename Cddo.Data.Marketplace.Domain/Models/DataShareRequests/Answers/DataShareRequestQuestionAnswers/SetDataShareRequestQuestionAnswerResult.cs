using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

public class SetDataShareRequestQuestionAnswerResult
{
    public Guid? NextQuestionId { get; set; }

    public bool AnswerIsValid { get; set; }

    public DataShareRequestQuestion QuestionInformation { get; set; }

    public bool DataShareRequestQuestionsRemainThatRequireAResponse { get; set; }
}