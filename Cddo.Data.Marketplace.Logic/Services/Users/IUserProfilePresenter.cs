using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Logic.Services.Users
{
    public interface IUserProfilePresenter
    {
        Task<IUserIdSet?> GetInitiatingUserIdSetAsync();

        Task<IUserDetails?> GetInitiatingUserDetailsAsync();

        Task<IUserDetails?> GetUserDetailsByUserIdAsync(int userId);

        Task<IOrganisationInformation?> GetOrganisationInformationAsync(int organisationId);

        Task<IDomainInformation?> GetDomainInformationOfInitiatingUserAsync();

        Task<IDomainInformation?> GetOrganisationDomainInformationAsync(int organisationId, int domainId);
    }
}
