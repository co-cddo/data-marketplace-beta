namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;

public class QuestionPartAnswer
{
    public Guid QuestionPartId { get; set; }

    public List<QuestionPartAnswerResponse> AnswerPartResponses { get; set; }
}