namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public class OrganisationInformation : IOrganisationInformation
{
    public required int OrganisationId { get; init; }
    public required string OrganisationName { get; init; }
    public required IEnumerable<IDomainInformation> Domains { get; init; }
}