using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionDetails
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public DataShareRequestStatus RequestStatus { get; set; }

    public string EsdaName { get; set; }

    public string AcquirerOrganisationName { get; set; }

    public List<SubmissionDetailsSection> Sections { get; set; }

    public SubmissionReturnDetailsSet SubmissionReturnDetailsSet { get; set; }
}