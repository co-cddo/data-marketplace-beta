namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatFreeFormDateTime : QuestionPartResponseFormatFreeForm
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.DateTime;
}