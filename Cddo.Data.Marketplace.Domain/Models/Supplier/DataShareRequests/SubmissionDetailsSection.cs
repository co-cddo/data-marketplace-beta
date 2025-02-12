namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionDetailsSection
{
    public int SectionNumber { get; set; }

    public string SectionHeader { get; set; }

    public List<SubmissionDetailsAnswerGroup> AnswerGroups { get; set; }
}