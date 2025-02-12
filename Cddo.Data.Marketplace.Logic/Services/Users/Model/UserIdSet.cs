namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public class UserIdSet : IUserIdSet
{
    public required int UserId { get; init; }

    public required int DomainId { get; init; }

    public required int OrganisationId { get; init; }
}