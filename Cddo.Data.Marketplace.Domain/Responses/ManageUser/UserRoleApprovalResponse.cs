namespace Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;

public class UserRoleApprovalDetailResponse
{
    public int ApprovalID { get; set; }
    public int UserID { get; set; }
    public string? Username { get; set; }
    public int DomainID { get; set; }
    public string? DomainName { get; set; }
    public int OrganisationID { get; set; }
    public string? OrganisationName { get; set; }
    public int RoleID { get; set; }
    public string? RoleName { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public int? ApprovedByUserID { get; set; }
    public string? ApprovedByUsername { get; set; }
    public string? ApprovedByDomainName { get; set; }
    public string? ApprovedByOrganisationName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? RejectionComment { get; set; }
    public string? RequestReason { get; set; }
}

public enum ApprovalStatus
{
    NotRequested,
    Approved,
    Pending,
    Rejected,
    Revoked
}
