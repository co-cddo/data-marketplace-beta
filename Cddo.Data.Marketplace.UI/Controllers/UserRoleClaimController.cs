using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.UI.Model.Enum;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers
{
    [Authorize]
    [Route("UserRoleClaim")]
    public class UserRoleClaimController : Controller
    {
        private readonly IUserRoleClaimService _userRoleClaimService;
        private readonly ILogger<UserRoleClaimController> _logger;

        public UserRoleClaimController(
            IUserRoleClaimService userRoleClaimService, ILogger<UserRoleClaimController> logger)
        {
            _userRoleClaimService = userRoleClaimService;
            _logger = logger;
        }

        [Route("UserProfile")]
        public async Task<IActionResult> GetUserRoleDetails(CancellationToken cancellationToken = default)
        {
            var result = await _userRoleClaimService.GetUserRoleDetailsAsync(cancellationToken).ConfigureAwait(false);
            ViewBag.ErrorMessage = TempData["ErrorMessage"];

            return View("~/Pages/Auth/UserClaims.cshtml", result);
        }

        [HttpPost("UserEmailNotification")]
        public async Task<IActionResult> SetUserEmailNotification(bool notificationDecision, int userId, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("UserEmailNotification validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            await _userRoleClaimService.SetUserEmailNotificationAsync(notificationDecision, userId, cancellationToken).ConfigureAwait(false);

            return RedirectToAction(nameof(GetUserRoleDetails));
        }

        [HttpPost("UserApprovalRequest")]
        public async Task<IActionResult> SetUserApprovalRequest(UserRoleApprovalRequest userRoleApprovalRequest, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("UserApprovalRequest validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            var validationMessage = ValidateRequest(userRoleApprovalRequest);

            if (!string.IsNullOrEmpty(userRoleApprovalRequest.ReasonPublisherRequest))
            {
                TempData["reasonPublisherRequest"] = userRoleApprovalRequest.ReasonPublisherRequest;
            }
            else
            {
                TempData["reasonPublisherRequest"] = userRoleApprovalRequest.ReasonDataApproverRequest;
            }
            if (userRoleApprovalRequest.MetadataPublisher)
            {
                TempData["metadataPublisher"] = true;
            }
            if (userRoleApprovalRequest.DataRequestApprover)
            {
                TempData["dataRequestApprover"] = true;
            }

            if (!string.IsNullOrEmpty(validationMessage))
            {
                
                TempData["ErrorMessage"] = validationMessage;
                return RedirectToAction(nameof(GetUserRoleDetails));
            }

            var setUserRoleApprovalRequest = SetUserApprovalPermissionRequest(userRoleApprovalRequest);

            if (!setUserRoleApprovalRequest.Any())
            {
                
                TempData["ErrorMessage"] = "No valid role request specified.";
                return RedirectToAction(nameof(GetUserRoleDetails));
            }
            
            await _userRoleClaimService.SetUserRoleApprovalAsync(setUserRoleApprovalRequest, cancellationToken).ConfigureAwait(false);
            return RedirectToAction(nameof(GetUserRoleDetails));
        }

        private static string? ValidateRequest(UserRoleApprovalRequest userRoleApprovalRequest)
        {
            if (userRoleApprovalRequest.MetadataPublisher && string.IsNullOrEmpty(userRoleApprovalRequest.ReasonPublisherRequest))
            {
                return "Missing required reason for requesting Metadata Publisher role.";
            }

            if (userRoleApprovalRequest.DataRequestApprover && string.IsNullOrEmpty(userRoleApprovalRequest.ReasonDataApproverRequest))
            {
                return "Missing required reason for requesting Data Request Approval role.";
            }

            if (string.IsNullOrEmpty(userRoleApprovalRequest.ReasonPublisherRequest) && userRoleApprovalRequest.MetadataPublisher)
            {
                return "You must check the Metadata Publisher checkbox if you provide a reason.";
            }

            if (string.IsNullOrEmpty(userRoleApprovalRequest.ReasonDataApproverRequest) && userRoleApprovalRequest.DataRequestApprover)
            {
                return "You must check the Data Request Approval checkbox if you provide a reason.";
            }

            return null;
        }

        private static List<SetUserApprovalRequest>? SetUserApprovalPermissionRequest(UserRoleApprovalRequest userRoleApprovalRequest)
        {
            var requests = new List<SetUserApprovalRequest>();

            if (userRoleApprovalRequest.MetadataPublisher)
            {
                requests.Add(new SetUserApprovalRequest
                {
                    UserID = userRoleApprovalRequest.UserId,
                    DomainID = userRoleApprovalRequest.DomainId,
                    OrganisationID = userRoleApprovalRequest.OrganisationId,
                    RoleID = 4,
                    ApprovalStatus = (Api.Dto.Responses.ManageUser.ApprovalStatus)ApprovalStatus.Pending,
                    RequestReason = userRoleApprovalRequest.ReasonPublisherRequest
                });
            }

            if (userRoleApprovalRequest.DataRequestApprover)
            {
                requests.Add(new SetUserApprovalRequest
                {
                    UserID = userRoleApprovalRequest.UserId,
                    DomainID = userRoleApprovalRequest.DomainId,
                    OrganisationID = userRoleApprovalRequest.OrganisationId,
                    RoleID = 7,
                    ApprovalStatus = (Api.Dto.Responses.ManageUser.ApprovalStatus)ApprovalStatus.Pending,
                    RequestReason = userRoleApprovalRequest.ReasonDataApproverRequest
                });
            }

            return requests;
        }      
    }
}
