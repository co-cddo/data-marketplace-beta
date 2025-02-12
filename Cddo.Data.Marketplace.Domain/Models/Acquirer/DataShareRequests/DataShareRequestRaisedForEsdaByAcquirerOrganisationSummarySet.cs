namespace Agrimetrics.DataShare.Api.Dto.Models.Acquirer.DataShareRequests;

public class DataShareRequestRaisedForEsdaByAcquirerOrganisationSummarySet
{
    public Guid EsdaId { get; set; }

    public string EsdaName { get; set; }

    public List<DataShareRequestRaisedForEsdaByAcquirerOrganisationSummary> DataShareRequestRaisedForEsdaByAcquirerOrganisationSummaries { get; set; }
}