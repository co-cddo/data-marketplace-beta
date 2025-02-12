namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats
{
    public class QuestionPartResponseItemFreeFormText : QuestionPartResponseItemModel
    {
        public required string TextInputComponentId { get; init; }

        public required string TextInputComponentName { get; init; }

        public required bool IsShortAnswer { get; init; }

        public required string EnteredValue { get; init; }

        public required int MaximumResponseLength { get; init; }
    }
}
