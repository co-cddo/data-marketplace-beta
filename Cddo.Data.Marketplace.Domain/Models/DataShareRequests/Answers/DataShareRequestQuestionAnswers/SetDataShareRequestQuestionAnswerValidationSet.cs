namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

public class SetDataShareRequestQuestionAnswerPartResponseValidationError
{
    public Guid QuestionPartId { get; set; }

    public int ResponseOrderWithinAnswerPart { get; set; }

    public List<string> ValidationErrors { get; set; }
}