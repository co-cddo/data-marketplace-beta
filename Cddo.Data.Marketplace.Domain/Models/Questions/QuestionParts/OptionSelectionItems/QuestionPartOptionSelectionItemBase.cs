namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.OptionSelectionItems;

public class QuestionPartOptionSelectionItemBase
{
    public Guid Id { get; set; }

    public string? ValueText { get; set; }
    public string? HintText { get; set; }
    public int OptionOrderWithinSelection { get; set; }

    public QuestionPart? SupplementaryQuestion { get; set; }
}