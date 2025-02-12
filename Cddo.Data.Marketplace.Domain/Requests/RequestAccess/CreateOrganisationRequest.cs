using System.ComponentModel.DataAnnotations;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.RequestAccess
{
    public class CreateOrganisationRequest
    {
        public int? OrganisationRequestID { get; set; }
        public int? OrganisationID { get; set; }
        [Required(ErrorMessage = "User name is required")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "User email address is required")]
        public string CreatedBy { get; set; }
        [Required(ErrorMessage = "Organisation name is required")]
        public string OrganisationName { get; set; }
        [Required(ErrorMessage = "Organisation type is required")]
        public OrganisationType? OrganisationType { get; set; }
        public string OrganisationFormat { get; set; } = string.Empty;
        [Required(ErrorMessage = "Domain name is required")]
        [RegularExpression(@"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid domain name")]
        public string DomainName { get; set; }
    }
}