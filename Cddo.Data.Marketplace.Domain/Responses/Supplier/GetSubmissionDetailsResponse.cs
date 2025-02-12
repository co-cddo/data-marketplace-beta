using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class GetSubmissionDetailsResponse
{
    public Guid DataShareRequestId { get; set; }

    public SubmissionDetails SubmissionDetails { get; set; }
}