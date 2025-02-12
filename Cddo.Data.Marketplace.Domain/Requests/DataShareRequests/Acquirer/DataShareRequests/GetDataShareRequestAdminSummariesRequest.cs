using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class GetDataShareRequestAdminSummariesRequest
{
    public int? AcquirerOrganisationId { get; set; }

    public int? SupplierOrganisationId { get; set; }

    public List<DataShareRequestStatus>? DataShareRequestStatuses { get; set; }
}