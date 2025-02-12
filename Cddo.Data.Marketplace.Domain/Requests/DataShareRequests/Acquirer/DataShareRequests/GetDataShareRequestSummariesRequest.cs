using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class GetDataShareRequestSummariesRequest
{
    public int? AcquirerUserId { get; set; }

    public int? AcquirerDomainId { get; set; }

    public int? AcquirerOrganisationId { get; set; }

    public int? SupplierDomainId { get; set; }

    public int? SupplierOrganisationId { get; set; }

    public Guid? EsdaId { get; set; }

    public List<DataShareRequestStatus>? DataShareRequestStatuses { get; set; }
}