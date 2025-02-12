namespace Cddo.Data.Marketplace.Logic.Services.Users.Model;

public interface IDomainInformation
{
    int DomainId { get; }

    string DomainName { get; }

    string? DataShareRequestMailboxAddress { get; }
}