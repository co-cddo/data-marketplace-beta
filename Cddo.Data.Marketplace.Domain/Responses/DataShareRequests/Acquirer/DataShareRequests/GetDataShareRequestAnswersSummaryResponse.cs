using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class GetDataShareRequestAnswersSummaryResponse
{
    public Guid DataShareRequestId { get; set; }

    public DataShareRequestAnswersSummary AnswersSummary { get; set; }
}