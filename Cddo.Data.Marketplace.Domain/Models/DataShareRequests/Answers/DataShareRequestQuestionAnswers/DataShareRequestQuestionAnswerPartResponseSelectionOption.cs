using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

public class DataShareRequestQuestionAnswerPartResponseSelectionOption : DataShareRequestQuestionAnswerPartResponseBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.OptionSelection;

    public List<DataShareRequestQuestionAnswerPartResponseSelectionOptionItem> SelectedOptionItems { get; set; }
}