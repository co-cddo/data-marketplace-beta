using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.OptionSelectionItems;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatOptionSelectMultiValue : QuestionPartResponseFormatOptionSelect
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.SelectMulti;

    public List<QuestionPartOptionSelectionItemForMultiSelection>? MultiSelectionOptions { get; set; }
}