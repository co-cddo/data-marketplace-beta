namespace Cddo.Data.Marketplace.Api.Dto.Models;

public class UserProfile
{
    public UserInfo? User { get; set; }
    public UserDomain? Domain { get; set; }
    public UserOrganisation? Organisation { get; set; }
    public List<Role>? Roles { get; set; }
    public bool EmailNotification { get; set; }
    public bool WelcomeNotification { get; set; }
    public DateTime LastLogin { get; set; }
}

public class UserInfo
{
    public int UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
}

public class UserDomain
{
    public int DomainId { get; set; }
    public string? DomainName { get; set; }
    public bool IsEnabled { get; set; }
}

public class UserOrganisation
{
    public int OrganisationId { get; set; }
    public string? OrganisationName { get; set; }
    public bool IsEnabled { get; set; }
}

public class Role
{
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? Description { get; set; }
}
