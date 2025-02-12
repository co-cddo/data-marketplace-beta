namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public interface IUserDetails
{
    IUserIdSet UserIdSet { get; }

    IUserContactDetails UserContactDetails { get; }

    IOrganisationInformation OrganisationInformation { get; }
}