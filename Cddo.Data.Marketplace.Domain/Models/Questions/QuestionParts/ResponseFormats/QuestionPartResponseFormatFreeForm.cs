using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.FreeFormItems;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatFreeForm : QuestionPartResponseFormatBase
{
    public override QuestionPartResponseInputType InputType { get; set; } = QuestionPartResponseInputType.FreeForm;

    public QuestionPartFreeFormOptions? FreeFormOptions { get; set; }
}