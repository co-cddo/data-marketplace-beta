using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

public class DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemFreeForm : DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.FreeForm;

    public string AnswerValue { get; set; }

    public bool ValueEntryDeclined { get; set; }
}