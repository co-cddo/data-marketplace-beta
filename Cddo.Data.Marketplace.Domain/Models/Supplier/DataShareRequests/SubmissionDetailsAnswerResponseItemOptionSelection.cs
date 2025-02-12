namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionDetailsAnswerResponseItemOptionSelection : SubmissionDetailsAnswerResponseItemBase
{
    public List<SubmissionDetailsAnswerResponseItemSelectedOption> SelectedOptions { get; set; } = [];
}