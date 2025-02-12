using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Cddo.Data.Marketplace.Api.Dto.Models.DataShareRequests;

public class DataShareRequestAdminSummary
{
    public required Guid Id { get; init; }

    public required string RequestId { get; init; }

    public required string EsdaName { get; init; }

    public required DateTime WhenCreatedUtc { get; init; }

    public DateTime? WhenSubmittedUtc { get; init; }

    public required string CreatedByUserEmailAddress { get; init; }

    public DateTime? WhenNeededByUtc { get; init; }

    public required DataShareRequestStatus DataShareRequestStatus { get; init; }
}