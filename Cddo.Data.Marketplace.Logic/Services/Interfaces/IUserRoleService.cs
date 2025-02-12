using Cddo.Data.Marketplace.Api.Dto.Models;

namespace Cddo.Data.Marketplace.Logic.Services.Interfaces;
public interface IUserRoleService
{
    Task<UserProfile> GetUserProfileAsync();
    Task<UserProfile> GetUserByIdAsync(string id);
    Task<bool> IsUserInRoleAsync(List<string> roles);
    Task<UserProfile> AddUserToRoleAsync(string roleId, string userId);
    Task<UserProfile> RemoveUserFromRoleAsync(string roleId, string userId);
    Task<bool> IsUserDomainEnabledAsync();
    Task<bool> IsUserRoleAdmin();
    Task<List<Role>> GetAllRolesAsync();
    Task<bool> IsUserRoleSystemAdmin();
    Task<bool> IsUserRoleSupplier();
    Task<bool> IsUserRolePublisher();
}
