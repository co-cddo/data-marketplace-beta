using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts;

public class QuestionPart
{
    public Guid Id { get; set; }

    public int QuestionPartOrderWithinQuestion { get; set; }

    public QuestionPartPrompts Prompts { get; set; }

    public QuestionPartMultipleAnswerItemControl MultipleAnswerItemControl { get; set; }

    public QuestionPartResponseFormatBase ResponseFormat { get; set; }
}