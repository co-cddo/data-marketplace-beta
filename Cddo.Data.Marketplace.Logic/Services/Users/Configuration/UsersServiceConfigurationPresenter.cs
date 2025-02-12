using Cddo.Data.Marketplace.Logic.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cddo.Data.Marketplace.Logic.Services.Users.Configuration;

internal class UsersServiceConfigurationPresenter(
    IConfiguration configuration,
    ILogger<UsersServiceConfigurationPresenter> logger,
    IConfigurationKeys configurationKeys) : IUsersServiceConfigurationPresenter
{
    string IUsersServiceConfigurationPresenter.GetUserInfoByTokenEndPoint()
        => DoGetUserInfoByTokenEndPoint();

    string IUsersServiceConfigurationPresenter.GetUserInfoByUserIdEndPoint()
        => DoGetUserInfoByUserIdEndPoint();

    public string GetUserOrganisationByOrganisationIdEndPoint()
        => DoGetUserOrganisationByOrganisationIdEndPoint();

    private string DoGetUserInfoByTokenEndPoint()
    {
        var userInfoAddress = GetUserInfoAddress();

        return $"{userInfoAddress}/User/userinfo";
    }

    private string DoGetUserInfoByUserIdEndPoint()
    {
        var userInfoAddress = GetUserInfoAddress();

        return $"{userInfoAddress}/User/UserById";
    }

    private string DoGetUserOrganisationByOrganisationIdEndPoint()
    {
        var userInfoAddress = GetUserInfoAddress();

        return $"{userInfoAddress}/Organisations";
    }

    private string? GetUserInfoAddress()
    {
        var usersApiKey = configurationKeys.UsersApiAddressKey;
        var address = configuration.GetSection(usersApiKey).Value?.TrimEnd('/');

        if (string.IsNullOrEmpty(address))
        {
            logger.LogError("Users API Address is not configured");
        }

        return address;
    }

}