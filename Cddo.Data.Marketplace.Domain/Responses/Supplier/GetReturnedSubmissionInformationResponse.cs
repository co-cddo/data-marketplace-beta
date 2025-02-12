using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

public class GetReturnedSubmissionInformationResponse
{
    public Guid DataShareRequestId { get; set; }

    public ReturnedSubmissionInformation ReturnedSubmissionInformation { get; set; }
}