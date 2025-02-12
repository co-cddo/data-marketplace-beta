using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionAnswerGroupEntryPart
{
    public int OrderWithinGroupEntry { get; set; }

    public string QuestionPartText { get; set; }

    public QuestionPartResponseInputType ResponseInputType { get; set; }

    public QuestionPartResponseFormatType ResponseFormatType { get; set; }

    public bool MultipleResponsesAllowed { get; set; }

    public string? CollectionDescriptionIfMultipleResponsesAllowed { get; set; }

    public List<SubmissionDetailsAnswerResponse> Responses { get; set; } = [];
}