namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionDetailsAnswerGroup
{
    public string MainQuestionHeader { get; set; }

    public int OrderWithinSubmission { get; set; }

    public SubmissionAnswerGroupEntry MainEntry { get; set; }

    public List<SubmissionAnswerGroupEntry> BackingEntries { get; set; }
}