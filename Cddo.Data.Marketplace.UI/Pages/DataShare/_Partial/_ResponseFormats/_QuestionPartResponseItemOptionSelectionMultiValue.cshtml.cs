using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.OptionSelectionItems;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats
{
    public class QuestionPartResponseItemOptionSelectionMultiValue : QuestionPartResponseItemModel
    {
        public required List<SelectionOptionInMultiValueSetModel> SelectionOptions { get; init; }
    }
}
