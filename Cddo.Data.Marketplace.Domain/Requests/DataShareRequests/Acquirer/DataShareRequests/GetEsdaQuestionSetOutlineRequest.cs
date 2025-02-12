using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class GetEsdaQuestionSetOutlineRequest
{
    [Required]
    public Guid EsdaId { get; set; }

    public int SupplierDomainId { get; set; }

    public int SupplierOrganisationId { get; set; }
}