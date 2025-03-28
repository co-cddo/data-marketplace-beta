namespace Cddo.Data.Marketplace.Api.Dto.Responses.RequestAccess
{
    public class OrganisationAccessResponse
    {
        public int? OrganisationRequestID { get; set; }
        public string OrganisationName { get; set; }
        public OrganisationType OrganisationType { get; set; }
        public string? OrganisationFormat { get; set; }
        public string DomainName { get; set; }
        public string? UserName { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
    }
}
