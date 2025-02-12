namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

public class DataShareRequestQuestionAnswerPart
{
    public Guid QuestionPartId { get; set; }

    public List<DataShareRequestQuestionAnswerPartResponseBase> AnswerPartResponses { get; set; }
}