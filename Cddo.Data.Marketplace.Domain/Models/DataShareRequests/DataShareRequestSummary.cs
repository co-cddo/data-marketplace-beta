namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

public class DataShareRequestSummary
{
    public Guid Id { get; set; }

    public string RequestId { get; set; }

    public string EsdaName { get; set; }

    public DataShareRequestStatus DataShareRequestStatus { get; set; }
}