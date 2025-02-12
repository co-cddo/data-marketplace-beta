using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

public class DataShareRequestAnswersSummaryQuestionPartAnswerResponseOptionSelection : DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.OptionSelection;

    public List<DataShareRequestAnswersSummaryQuestionPartAnswerResponseOptionSelectionSelectedOption> SelectedOptions { get; set; }
}