namespace Cddo.Data.Marketplace.Api.Dto;
public class DomainDetail
{
    public int? DomainId { get; set; }
    public string? DomainName { get; set; }
    public OrganisationType? OrganisationType { get; set; }
    public string? OrganisationFormat { get; set; }
    public bool AllowList { get; set; }
    public string? DataShareRequestMailboxAddress { get; set; }
}
