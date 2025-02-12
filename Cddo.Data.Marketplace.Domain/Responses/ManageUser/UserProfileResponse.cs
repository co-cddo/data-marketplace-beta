using Cddo.Data.Marketplace.Api.Dto.Models;

namespace Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser
{
    public class UserProfileResponse
    {
        public UserInfo? User { get; set; }
        public UserDomain? Domain { get; set; }
        public UserOrganisation? Organisation { get; set; }
        public List<Role>? Roles { get; set; }
        public bool EmailNotification { get; set; }
        public bool WelcomeNotification { get; set; }
        public DateTime LastLogin { get; set; }
        public string? Token { get; set; }
    }
}
