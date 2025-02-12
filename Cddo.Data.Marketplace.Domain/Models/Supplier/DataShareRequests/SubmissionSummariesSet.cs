namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionSummariesSet
{
    public List<PendingSubmissionSummary> PendingSubmissionSummaries { get; set; }

    public List<CompletedSubmissionSummary> CompletedSubmissionSummaries { get; set; }
}