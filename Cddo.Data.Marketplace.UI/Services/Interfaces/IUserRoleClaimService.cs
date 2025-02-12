using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces
{
    public interface IUserRoleClaimService
    {
        Task<UserProfileResponse?> GetUserRoleDetailsAsync(CancellationToken cancellationToken = default);
        Task<IActionResult?> SetUserEmailNotificationAsync(bool notificationDecision, int userId, CancellationToken cancellationToken = default);
        Task<List<UserRoleApprovalDetailResponse>?> GetUserRoleApprovalListAsync(int userId, CancellationToken cancellationToken = default);
        Task<IActionResult?> SetUserRoleApprovalAsync(List<SetUserApprovalRequest> setUserApprovalRequest, CancellationToken cancellationToken = default);
    }
}
