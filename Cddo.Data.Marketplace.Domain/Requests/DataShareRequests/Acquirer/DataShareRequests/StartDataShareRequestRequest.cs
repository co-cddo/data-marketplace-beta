using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class StartDataShareRequestRequest
{
    [Required]
    public Guid EsdaId { get; set; }

    [Required]
    public int SupplierDomainId { get; set; }

    [Required]
    public int SupplierOrganisationId { get; set; }
}