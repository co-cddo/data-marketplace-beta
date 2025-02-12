using Cddo.Data.Marketplace.UI.Model.Enum;

namespace Cddo.Data.Marketplace.UI.Model
{
    public class UserRoleApproval
    {
        public int ApprovalID { get; set; }
        public int UserID { get; set; }
        public int DomainID { get; set; }
        public int OrganisationID { get; set; }
        public int RoleID { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public int ApprovedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? RejectionComment { get; set; }
        public string? RequestReason { get; set; }
    }

    public class UserRoleApprovalDetail
    {
        public int ApprovalID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public int DomainID { get; set; }
        public string DomainName { get; set; }
        public int OrganisationID { get; set; }
        public string OrganisationName { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }  // Name from Roles table
        public ApprovalStatus ApprovalStatus { get; set; }
        public int? ApprovedByUserID { get; set; }
        public string ApprovedByUsername { get; set; }  // Username of the approver
        public string ApprovedByDomainName { get; set; }  // Domain name of the approver
        public string ApprovedByOrganisationName { get; set; }  // Organisation name of the approver
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? RejectionComment { get; set; }
        public string? RequestReason { get; set; }
    }
}
