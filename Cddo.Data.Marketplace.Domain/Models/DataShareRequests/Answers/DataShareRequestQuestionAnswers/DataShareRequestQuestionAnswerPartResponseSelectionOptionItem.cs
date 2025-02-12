namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

public class DataShareRequestQuestionAnswerPartResponseSelectionOptionItem
{
    public Guid OptionSelectionItemId { get; set; }

    public DataShareRequestQuestionAnswerPart? SupplementaryQuestionAnswerPart { get; set; }
}