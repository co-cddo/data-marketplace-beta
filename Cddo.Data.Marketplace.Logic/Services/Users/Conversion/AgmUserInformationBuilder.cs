using Agm.Catalog.DotNet.Dto.Models.UserData;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Logic.Services.Users.Conversion;

internal class AgmUserInformationBuilder : IAgmUserInformationBuilder
{
    IAgmUserDetails IAgmUserInformationBuilder.BuildAgmUserDetails(
        IUserDetails userDetails)
    {
        return new AgmUserDetails
        {
            IdSet = DoBuildAgmUserIdSet(userDetails.UserIdSet),
            ContactDetails = DoBuildAgmUserContactDetails(userDetails.UserContactDetails),
            OrganisationInformation = DoBuildAgmOrganisationInformation(userDetails.OrganisationInformation)
        };
    }

    IAgmUserIdSet IAgmUserInformationBuilder.BuildAgmUserIdSet(IUserIdSet userIdSet) => 
        DoBuildAgmUserIdSet(userIdSet);

    IAgmUserContactDetails IAgmUserInformationBuilder.BuildAgmUserContactDetails(IUserContactDetails userContactDetails) =>
        DoBuildAgmUserContactDetails(userContactDetails);

    IAgmOrganisationInformation IAgmUserInformationBuilder.BuildAgmOrganisationInformation(IOrganisationInformation organisationInformation) =>
        DoBuildAgmOrganisationInformation(organisationInformation);

    private static IAgmUserIdSet DoBuildAgmUserIdSet(
        IUserIdSet userIdSet)
    {
        return new AgmUserIdSet
        {
            OrganisationId = userIdSet.OrganisationId,
            DomainId = userIdSet.DomainId,
            UserId = userIdSet.UserId
        };
    }

    private static IAgmUserContactDetails DoBuildAgmUserContactDetails(IUserContactDetails userContactDetails) =>
        new AgmUserContactDetails
        {
            UserName = userContactDetails.UserName,
            EmailAddress = userContactDetails.EmailAddress
        };

    private static IAgmOrganisationInformation DoBuildAgmOrganisationInformation(
        IOrganisationInformation organisationInformation)
    {
        return new AgmOrganisationInformation
        {
            OrganisationId = organisationInformation.OrganisationId,
            OrganisationName = organisationInformation.OrganisationName,
            DomainInformation = organisationInformation.Domains.Select(DoBuildAgmDomainInformation).ToList()
        };
    }

    private static IAgmDomainInformation DoBuildAgmDomainInformation(
        IDomainInformation domainInformation)
    {
        return new AgmDomainInformation
        {
            DomainName = domainInformation.DomainName,
            DomainId = domainInformation.DomainId,
            DataShareRequestMailboxAddress = domainInformation.DataShareRequestMailboxAddress
        };
    }
}