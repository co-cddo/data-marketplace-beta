using Cddo.Data.Marketplace.Api.Dto.Models;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;

public class UserRoleApprovalRequest
{
    public bool MetadataPublisher { get; set; }
    public bool DataRequestApprover { get; set; }
    public string? ReasonPublisherRequest { get; set; }
    public string? ReasonDataApproverRequest { get; set; }
    public int UserId { get; set; }
    public int DomainId { get; set; }
    public int OrganisationId { get; set; }
}
