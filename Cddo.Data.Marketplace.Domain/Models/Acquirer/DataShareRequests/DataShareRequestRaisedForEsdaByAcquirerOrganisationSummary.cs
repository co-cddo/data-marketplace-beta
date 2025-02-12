using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Models.Acquirer.DataShareRequests;

public class DataShareRequestRaisedForEsdaByAcquirerOrganisationSummary
{
    public Guid Id { get; set; }

    public string RequestId { get; set; }

    public DataShareRequestStatus Status { get; set; }

    public DateTime DateStarted { get; set; }

    public DateTime? DateSubmitted { get; set; }

    public AcquirerContactDetails OriginatingAcquirerContactDetails { get; set; }
}