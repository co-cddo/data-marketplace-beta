using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class GetCompletedSubmissionInformationResponse
{
    public Guid DataShareRequestId { get; set; }

    public CompletedSubmissionInformation CompletedSubmissionInformation { get; set; }
}