using Agrimetrics.DataShare.Api.Dto.Models.Acquirer.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse
{
    public Guid EsdaId { get; set; }

    public DataShareRequestRaisedForEsdaByAcquirerOrganisationSummarySet DataShareRequestSummaries { get; set; }
}