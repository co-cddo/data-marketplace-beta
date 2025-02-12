namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatFreeFormText : QuestionPartResponseFormatFreeForm
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.Text;

    public int MaximumResponseLength { get; set; }
}