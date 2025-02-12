namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

public class DataShareRequestAnswersSummaryQuestionGroup
{
    public int OrderWithinSection { get; set; }

    public DataShareRequestAnswersSummaryQuestion MainQuestionSummary { get; set; }

    public List<DataShareRequestAnswersSummaryQuestion> BackingQuestionSummaries { get; set; }
}