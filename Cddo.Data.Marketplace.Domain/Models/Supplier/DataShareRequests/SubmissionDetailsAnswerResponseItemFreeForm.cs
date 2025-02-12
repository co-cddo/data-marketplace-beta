namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionDetailsAnswerResponseItemFreeForm : SubmissionDetailsAnswerResponseItemBase
{
    public string AnswerValue { get; set; }

    public bool ValueEntryDeclined { get; set; }
}