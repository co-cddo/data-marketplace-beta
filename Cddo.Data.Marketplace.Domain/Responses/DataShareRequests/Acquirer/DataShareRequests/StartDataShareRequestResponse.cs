namespace Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;

public class StartDataShareRequestResponse
{
    public Guid EsdaId { get; set; }

    public int SupplierDomainId { get; set; }

    public int SupplierOrganisationId { get; set; }

    public Guid DataShareRequestId { get; set; }
}