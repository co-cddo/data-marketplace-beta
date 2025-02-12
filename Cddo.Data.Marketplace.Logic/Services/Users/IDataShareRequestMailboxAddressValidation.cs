namespace Cddo.Data.Marketplace.Logic.Services.Users;

public interface IDataShareRequestMailboxAddressValidation
{
    bool TryValidateDataShareRequestMailboxAddress(
        string dataShareRequestMailboxAddress,
        out string? validationError);
}