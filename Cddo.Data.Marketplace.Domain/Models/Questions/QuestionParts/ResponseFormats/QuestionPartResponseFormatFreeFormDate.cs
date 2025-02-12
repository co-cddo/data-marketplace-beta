namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatFreeFormDate : QuestionPartResponseFormatFreeForm
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.Date;
}