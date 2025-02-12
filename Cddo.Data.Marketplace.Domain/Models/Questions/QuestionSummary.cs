namespace Agrimetrics.DataShare.Api.Dto.Models.Questions;

public class QuestionSummary
{
    public Guid QuestionId { get; set; }

    public int QuestionOrderWithinQuestionSetSection { get; set; }

    public string QuestionHeader { get; set; }

    public QuestionStatus QuestionStatus { get; set; }

    public bool QuestionCanBeAnswered { get; set; }
}