using Cddo.Data.Marketplace.Api.Dto.ManageUser;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
public class ManageOrganisationsResponse
{
    public List<OrganisationDetail> Orgs { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
