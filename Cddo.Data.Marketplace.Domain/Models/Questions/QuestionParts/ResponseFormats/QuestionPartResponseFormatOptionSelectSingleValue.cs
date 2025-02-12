using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.OptionSelectionItems;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public class QuestionPartResponseFormatOptionSelectSingleValue : QuestionPartResponseFormatOptionSelect
{
    public override QuestionPartResponseFormatType FormatType { get; set; } = QuestionPartResponseFormatType.SelectSingle;

    public List<QuestionPartOptionSelectionItemForSingleSelection> SingleSelectionOptions { get; set; }
}