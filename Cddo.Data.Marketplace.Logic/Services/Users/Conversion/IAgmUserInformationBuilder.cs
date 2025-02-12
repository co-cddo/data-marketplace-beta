using Agm.Catalog.DotNet.Dto.Models.UserData;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Logic.Services.Users.Conversion;

public interface IAgmUserInformationBuilder
{
    IAgmUserDetails BuildAgmUserDetails(
        IUserDetails userDetails);

    IAgmUserIdSet BuildAgmUserIdSet(
        IUserIdSet userIdSet);

    IAgmUserContactDetails BuildAgmUserContactDetails(
        IUserContactDetails userContactDetails);

    IAgmOrganisationInformation BuildAgmOrganisationInformation(
        IOrganisationInformation organisationInformation);
}