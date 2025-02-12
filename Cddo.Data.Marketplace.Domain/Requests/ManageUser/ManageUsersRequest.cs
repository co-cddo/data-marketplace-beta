namespace Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
public class ManageUsersRequest
{
    public string SearchTerm { get; set; }
    public int CurrentPage { get; set; }
    public bool? Visible { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int? SelectedOrganisationId { get; set; }
    public int? SelectedDomainId { get; set; }
    public string SortBy { get; set; } = "email";
    public string SortOrder { get; set; } = "ASC";
}
