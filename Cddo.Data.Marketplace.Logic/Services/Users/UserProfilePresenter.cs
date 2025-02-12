using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Logic.Services.Users.Configuration;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.Logic.Services.Users.Model.External;
using Cddo.Data.Marketplace.Logic.Services.Users.UserIdPresentation;
using Flurl;
using Flurl.Http;
using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.Audit;

namespace Cddo.Data.Marketplace.Logic.Services.Users;

public class UserProfilePresenter(
    IUserIdPresenter userIdPresenter,
    IAppInsightsLogger logger,
    IUsersServiceConfigurationPresenter usersServiceConfigurationPresenter) : IUserProfilePresenter
{
    async Task<IUserIdSet?> IUserProfilePresenter.GetInitiatingUserIdSetAsync()
    {
        var userProfile = await GetUserProfileOfInitiatingUserAsync();

        return userProfile != null
            ? BuildUserIdSetFromUserProfile(userProfile)
            : null;
    }

    async Task<IUserDetails?> IUserProfilePresenter.GetInitiatingUserDetailsAsync()
    {
        var userProfile = await GetUserProfileOfInitiatingUserAsync();

        return userProfile != null
            ? BuildUserDetailsFromProfile(userProfile)
            : null;
    }

    async Task<IUserDetails?> IUserProfilePresenter.GetUserDetailsByUserIdAsync(int userId)
    {
        var userProfile = await GetUserProfileByIdAsync(userId);

        return userProfile != null
            ? BuildUserDetailsFromProfile(userProfile)
            : null;
    }

    async Task<IOrganisationInformation?> IUserProfilePresenter.GetOrganisationInformationAsync(int organisationId) =>
        await DoGetOrganisationInformationAsync(organisationId);

    async Task<IDomainInformation?> IUserProfilePresenter.GetDomainInformationOfInitiatingUserAsync()
    {
        var userProfile = await GetUserProfileOfInitiatingUserAsync();
        if (userProfile == null) return null;

        var organisationInformation = await DoGetOrganisationInformationAsync(userProfile.Organisation.OrganisationId);

        return organisationInformation?.Domains.FirstOrDefault(domain => domain.DomainId == userProfile.Domain.DomainId);
    }

    private async Task<IOrganisationInformation?> DoGetOrganisationInformationAsync(int organisationId)
    {
        var organisationDetail = await GetOrganisationDetailByOrganisationIdAsync(organisationId);

        return organisationDetail != null
            ? BuildOrganisationInformation(organisationDetail)
            : null;
    }

    async Task<IDomainInformation?> IUserProfilePresenter.GetOrganisationDomainInformationAsync(
        int organisationId, int domainId)
    {
        var organisationInformation = await DoGetOrganisationInformationAsync(organisationId);

        return organisationInformation?.Domains.FirstOrDefault(domain => domain.DomainId == domainId);
    }

    private static IUserDetails BuildUserDetailsFromProfile(
        UserProfile userProfile)
    {
        return new UserDetails
        {
            UserIdSet = BuildUserIdSetFromUserProfile(userProfile),
            UserContactDetails = BuildUserContactDetailsFromUserProfile(userProfile),
            OrganisationInformation = BuildOrganisationInformationFromUserProfile(userProfile)
        };
    }

    private static IUserIdSet BuildUserIdSetFromUserProfile(
        UserProfile userProfile)
    {
        return new UserIdSet
        {
            UserId = userProfile.User.UserId,
            DomainId = userProfile.Domain.DomainId,
            OrganisationId = userProfile.Organisation.OrganisationId
        };
    }

    private static IUserContactDetails BuildUserContactDetailsFromUserProfile(
        UserProfile initiatingUserProfile)
    {
        return new UserContactDetails
        {
            EmailAddress = initiatingUserProfile.User.UserEmail,
            UserName = initiatingUserProfile.User.UserName
        };
    }

    private static IOrganisationInformation BuildOrganisationInformationFromUserProfile(
        UserProfile userProfile)
    {
        return new OrganisationInformation
        {
            OrganisationId = userProfile.Organisation.OrganisationId,
            OrganisationName = userProfile.Organisation.OrganisationName,
            Domains = [ BuildDomainInformationFromUserProfile(userProfile) ]
        };
    }

    private static IOrganisationInformation BuildOrganisationInformation(
        OrganisationDetail organisationDetail)
    {
        return new OrganisationInformation
        {
            OrganisationId = organisationDetail.OrganisationId!.Value,
            OrganisationName = organisationDetail.OrganisationName ?? string.Empty,
            Domains = organisationDetail.Domains?.Select(BuildDomainInformation) ?? []
        };
    }
    private static IDomainInformation BuildDomainInformationFromUserProfile(UserProfile userProfile)
    {
        return new DomainInformation
        {
            DomainId = userProfile.Domain.DomainId,
            DomainName = userProfile.Domain.DomainName,
            DataShareRequestMailboxAddress = null
        };
    }

    private static IDomainInformation BuildDomainInformation(DomainDetail domainDetail)
    {
        return new DomainInformation
        {
            DomainId = domainDetail.DomainId!.Value,
            DomainName = domainDetail.DomainName ?? string.Empty,
            DataShareRequestMailboxAddress = domainDetail.DataShareRequestMailboxAddress
        };
    }

    private async Task<UserProfile?> GetUserProfileOfInitiatingUserAsync()
    {
        try
        {
            var initiatingUserIdToken = await userIdPresenter.GetInitiatingUserIdToken();

            if (initiatingUserIdToken == null) return null;

            var getUserInfoByTokenEndPoint = usersServiceConfigurationPresenter.GetUserInfoByTokenEndPoint();

            return await new Url(getUserInfoByTokenEndPoint)
                .WithOAuthBearerToken(initiatingUserIdToken)
                .PostJsonAsync(null)
                .ReceiveJson<UserProfile>();
        }
        catch (FlurlHttpException ex)
        {
            var responseBody = await ex.GetResponseStringAsync();
            logger.LogError($"GetUserProfileOfInitiatingUserAsync: Failed to fetch user profile. Status: {ex.StatusCode}, Response: {responseBody}", ex);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError($"GetUserProfileOfInitiatingUserAsync: Unexpected error. Message: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<UserProfile?> GetUserProfileByIdAsync(int userId)
    {
        try
        {
            var initiatingUserIdToken = await userIdPresenter.GetInitiatingUserIdToken();
            if (initiatingUserIdToken == null) return null;

            var getUserInfoByUserIdEndPoint = usersServiceConfigurationPresenter.GetUserInfoByUserIdEndPoint();

            return await new Url(getUserInfoByUserIdEndPoint)
                .WithOAuthBearerToken(initiatingUserIdToken)
                .SetQueryParam("userid", userId)
                .GetJsonAsync<UserProfile>();
        }
        catch (FlurlHttpException ex)
        {
            var responseBody = await ex.GetResponseStringAsync();
            logger.LogError($"GetUserProfileByIdAsync: Failed to fetch user profile for userId: {userId}. Status: {ex.StatusCode}, Response: {responseBody}", ex);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError($"GetUserProfileByIdAsync: Unexpected error for userId: {userId}. Message: {ex.Message}", ex);
            return null;
        }
    }

    private async Task<OrganisationDetail?> GetOrganisationDetailByOrganisationIdAsync(int organisationId)
    {
        try
        {
            var initiatingUserIdToken = await userIdPresenter.GetInitiatingUserIdToken();
            if (initiatingUserIdToken == null) return null;

            var getUserOrganisationByOrganisationIdEndPoint =
                usersServiceConfigurationPresenter.GetUserOrganisationByOrganisationIdEndPoint();

            return await new Url(getUserOrganisationByOrganisationIdEndPoint)
                .WithOAuthBearerToken(initiatingUserIdToken)
                .AppendPathSegment(organisationId)
                .GetJsonAsync<OrganisationDetail>();
        }
        catch (FlurlHttpException ex)
        {
            var responseBody = await ex.GetResponseStringAsync();
            logger.LogError($"GetOrganisationDetailByOrganisationIdAsync: Failed to get user organisation. Status: {ex.StatusCode}, Response: {responseBody}", ex);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError($"GetOrganisationDetailByOrganisationIdAsync: Unexpected error: {ex.Message}", ex);
            return null;
        }
    }
}