namespace Cddo.Data.Marketplace.Logic.Services.Users.Configuration
{
    public interface IUsersServiceConfigurationPresenter
    {
        string GetUserInfoByTokenEndPoint();

        string GetUserInfoByUserIdEndPoint();

        string GetUserOrganisationByOrganisationIdEndPoint();
    }
}
