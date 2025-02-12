using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class StartSubmissionReviewResponse
{
    public Guid DataShareRequestId { get; set; }

    public SubmissionReviewInformation SubmissionReviewInformation { get; set; }
}