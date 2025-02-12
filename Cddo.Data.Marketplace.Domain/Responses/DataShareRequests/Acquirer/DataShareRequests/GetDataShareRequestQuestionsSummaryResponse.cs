using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionSets;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class GetDataShareRequestQuestionsSummaryResponse
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public string EsdaName { get; set; }

    public QuestionSetSummary QuestionSetSummary { get; set; }
}