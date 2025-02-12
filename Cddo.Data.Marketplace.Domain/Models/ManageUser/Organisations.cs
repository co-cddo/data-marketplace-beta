using System.Text.Json.Serialization;

namespace Cddo.Data.Marketplace.Api.Dto.ManageUser
{
    public class Organisations
    {
        public List<OrganisationDetail>? Orgs { get; set; }
        public int? CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int? TotalPages
        {
            get { return (int)Math.Ceiling((double)TotalCount / PageSize); }
        }
    }
    public class OrganisationDetail
    {
        public int? OrganisationId { get; set; }
        public string? OrganisationName { get; set; }
        public OrganisationType? OrganisationType { get; set; }
        public List<DomainDetail>? Domains { get; set; }
        public int DomainCount { get; set; }
        public Department? OrgDepartment { get; set; }
        public DateTime? Modified { get; set; }
        public int? ModifiedBy { get; set; }
        public OrganisationRequest? OrganisationRequest { get; set; }
        public bool? Allowed { get; set; }

    }
    public class Department
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("departmentName")]
        public string? DepartmentName { get; set; }
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        [JsonPropertyName("created")]
        public DateTime? Created { get; set; }
        [JsonPropertyName("createdByName")]
        public string? CreatedByName { get; set; }
        [JsonPropertyName("updated")]
        public DateTime? Updated { get; set; }
        [JsonPropertyName("updatedBy")]
        public int? UpdatedBy { get; set; }
    }

    public class OrganisationRequest
    {
        public int? OrganisationRequestID { get; set; }
        public int? OrganisationID { get; set; }
        public string? OrganisationName { get; set; }
        public OrganisationType? OrganisationType { get; set; }
        public string? OrganisationFormat { get; set; }
        public string? DomainName { get; set; }
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
