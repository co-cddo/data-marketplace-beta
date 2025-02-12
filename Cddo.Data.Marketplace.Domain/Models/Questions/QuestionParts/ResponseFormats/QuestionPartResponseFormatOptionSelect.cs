namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatOptionSelect : QuestionPartResponseFormatBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.OptionSelection;


}