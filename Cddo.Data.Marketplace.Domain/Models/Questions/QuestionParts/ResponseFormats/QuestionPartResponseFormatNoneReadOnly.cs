namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatNoneReadOnly : QuestionPartResponseFormatNone
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.ReadOnly;
}