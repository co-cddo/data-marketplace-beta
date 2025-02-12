using Agm.Catalog.DotNet.Core.Validation.EmailAddress;

namespace Cddo.Data.Marketplace.Logic.Services.Users;

internal class DataShareRequestMailboxAddressValidation(
    ICddoEmailAddressValidation cddoEmailAddressValidation) : IDataShareRequestMailboxAddressValidation
{
    bool IDataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
        string dataShareRequestMailboxAddress,
        out string? validationError)
    {
        if (MailboxAddressIsEmpty(dataShareRequestMailboxAddress))
        {
            validationError = "Enter a valid email address";
            return false;
        }

        if (MailboxAddressFormatIsInvalid(dataShareRequestMailboxAddress))
        {
            validationError = "Enter a valid email address";
            return false;
        }

        validationError = null;
        return true;
    }

    private static bool MailboxAddressIsEmpty(
        string dataShareRequestMailboxAddress)
    {
        return string.IsNullOrWhiteSpace(dataShareRequestMailboxAddress);
    }

    private bool MailboxAddressFormatIsInvalid(
        string dataShareRequestMailboxAddress)
    {
        return !cddoEmailAddressValidation.IsEmailAddressValid(dataShareRequestMailboxAddress);
    }
}