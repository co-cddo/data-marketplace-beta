namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

internal class DomainInformation : IDomainInformation
{
    public required int DomainId { get; init; }
    public required string DomainName { get; init; }
    public required string? DataShareRequestMailboxAddress { get; init; }
}