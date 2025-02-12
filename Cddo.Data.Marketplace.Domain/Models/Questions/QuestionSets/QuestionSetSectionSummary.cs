namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionSets;

public class QuestionSetSectionSummary
{
    public Guid Id { get; set; }

    public int SectionNumber { get; set; }

    public string SectionHeader { get; set; }

    public bool SectionIsComplete { get; set; }

    public List<QuestionSummary> QuestionSummaries { get; set; }
}