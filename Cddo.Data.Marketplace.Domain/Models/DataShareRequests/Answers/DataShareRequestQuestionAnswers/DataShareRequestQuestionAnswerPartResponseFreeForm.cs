using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

public class DataShareRequestQuestionAnswerPartResponseFreeForm : DataShareRequestQuestionAnswerPartResponseBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.FreeForm;

    public string EnteredValue { get; set; }

    public bool ValueEntryDeclined { get; set; }
}