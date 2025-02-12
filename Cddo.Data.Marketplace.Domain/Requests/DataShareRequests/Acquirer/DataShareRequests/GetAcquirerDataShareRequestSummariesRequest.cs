using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class GetAcquirerDataShareRequestSummariesRequest
{
    public int? SupplierDomainId { get; set; }

    public int? SupplierOrganisationId { get; set; }

    public Guid? EsdaId { get; set; }

    public List<DataShareRequestStatus>? DataShareRequestStatuses { get; set; }
}