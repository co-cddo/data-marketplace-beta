using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class GetSubmissionInformationResponse
{
    public Guid DataShareRequestId { get; set; }

    public SubmissionInformation SubmissionInformation { get; set; }
}