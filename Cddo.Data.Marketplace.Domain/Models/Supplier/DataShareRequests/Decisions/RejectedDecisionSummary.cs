using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests.Decisions;

public class RejectedDecisionSummary
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public DataShareRequestStatus RequestStatus { get; set; }

    public string AcquirerOrganisationName { get; set; }
}