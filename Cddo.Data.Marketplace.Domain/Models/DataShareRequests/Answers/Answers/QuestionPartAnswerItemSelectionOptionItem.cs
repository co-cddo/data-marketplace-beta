namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;

public class QuestionPartAnswerItemSelectionOptionItem
{
    public Guid OptionSelectionItemId { get; set; }

    public QuestionPartAnswer? SupplementaryQuestionPartAnswer { get; set; }
}