namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts;

public class QuestionPartMultipleAnswerItemControl
{
    public bool MultipleAnswerItemsAreAllowed { get; set; }

    public string? ItemDescription { get; set; }

    public string? CollectionDescription { get; set; }
}