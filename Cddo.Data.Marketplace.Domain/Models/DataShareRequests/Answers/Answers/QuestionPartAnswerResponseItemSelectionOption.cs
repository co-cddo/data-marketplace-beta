using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;

public class QuestionPartAnswerResponseItemSelectionOption
    : QuestionPartAnswerResponseItemBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.OptionSelection;

    public List<QuestionPartAnswerItemSelectionOptionItem> SelectedOptions { get; set; }
}