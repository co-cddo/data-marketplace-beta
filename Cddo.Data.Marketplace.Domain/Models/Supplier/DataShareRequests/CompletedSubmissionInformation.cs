using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests.Decisions;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class CompletedSubmissionInformation
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public DataShareRequestStatus RequestStatus { get; set; }

    public SubmissionDecision Decision { get; set; }

    public string AcquirerUserEmail { get; set; }

    public string AcquirerOrganisationName { get; set; }

    public string EsdaName { get; set; }

    public DateTime SubmittedOn { get; set; }

    public DateTime CompletedOn { get; set; }

    public DateTime? WhenNeededBy { get; set; }

    public string SupplierNotes { get; set; }

    public string FeedbackProvided { get; set; }
}