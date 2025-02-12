namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public class UserDetails : IUserDetails
{
    public required IUserIdSet UserIdSet { get; init; }

    public required IUserContactDetails UserContactDetails { get; init; }

    public required IOrganisationInformation OrganisationInformation { get; init; }
}