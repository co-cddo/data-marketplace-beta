using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests.Decisions;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class CompletedSubmissionSummary
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public string AcquirerOrganisationName { get; set; }

    public string EsdaName { get; set; }

    public DateTime SubmittedOn { get; set; }

    public DateTime CompletedOn { get; set; }

    public SubmissionDecision Decision { get; set; }
}