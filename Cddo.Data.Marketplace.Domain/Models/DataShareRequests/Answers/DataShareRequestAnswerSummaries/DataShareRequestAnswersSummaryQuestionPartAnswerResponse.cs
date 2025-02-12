namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

public class DataShareRequestAnswersSummaryQuestionPartAnswerResponse
{
    public int OrderWithinQuestionPartAnswer { get; set; }

    public DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemBase? QuestionPartAnswerResponseItem { get; set; }
}