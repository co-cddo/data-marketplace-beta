namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatFreeFormTime : QuestionPartResponseFormatFreeForm
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.Time;
}