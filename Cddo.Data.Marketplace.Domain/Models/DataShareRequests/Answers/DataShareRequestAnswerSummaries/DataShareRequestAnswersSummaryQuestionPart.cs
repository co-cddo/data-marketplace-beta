using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

public class DataShareRequestAnswersSummaryQuestionPart
{
    public int OrderWithinQuestion { get; set; }

    public string QuestionPartText { get; set; }

    public bool MultipleResponsesAllowed { get; set; }

    public string MultipleResponsesCollectionHeaderIfMultipleResponsesAllowed { get; set; }

    public QuestionPartResponseInputType ResponseInputType { get; set; }

    public QuestionPartResponseFormatType ResponseFormatType { get; set; }

    public List<DataShareRequestAnswersSummaryQuestionPartAnswerResponse> Responses { get; set; }
}