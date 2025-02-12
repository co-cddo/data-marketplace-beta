namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatNone : QuestionPartResponseFormatBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.None;
}