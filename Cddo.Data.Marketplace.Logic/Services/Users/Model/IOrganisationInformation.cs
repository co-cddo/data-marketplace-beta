namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public interface IOrganisationInformation
{
    int OrganisationId { get; }

    string OrganisationName { get; }

    IEnumerable<IDomainInformation> Domains { get; }
}