using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser
{
    public class SetUserApprovalRequest
    {
        public int ApprovalID { get; set; }
        public int UserID { get; set; }
        public int DomainID { get; set; }
        public int OrganisationID { get; set; }
        public int RoleID { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public int? ApprovedByUserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? RejectionComment { get; set; }
        public string? RequestReason { get; set; }
    }
}
