using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class GetDataShareRequestSummariesResponse
{
    public DataShareRequestSummarySet DataShareRequestSummaries { get; set; }
}