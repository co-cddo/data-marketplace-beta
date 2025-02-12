using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats;

public abstract class QuestionPartResponseItemSelectionOptionModel : PageModel
{
    public required string SelectionOptionInputComponentId { get; init; }

    public required string SelectionOptionInputComponentName { get; init; }

    public required Guid SelectionOptionId { get; init; }

    public required int OptionOrderWithinSet { get; init; }

    public required string ValueText { get; init; }

    public required string? HintText { get; init; }

    public required bool IsSelected { get; set; }

    public required string SupplementaryQuestionInputComponentId { get; init; }

    public required string SupplementaryQuestionInputComponentName { get; init; }

    public required string SupplementaryQuestionPartIdComponentId { get; init; }

    public required string SupplementaryQuestionPartIdComponentName { get; init; }

    public required Guid? SupplementaryQuestionPartId { get; init; }

    public required string? SupplementaryQuestionText { get; init; }

    public required string? SupplementaryQuestionEnteredValue { get; init; }

    public required int? SupplementaryQuestionMaximumResponseLength { get; init; }
}