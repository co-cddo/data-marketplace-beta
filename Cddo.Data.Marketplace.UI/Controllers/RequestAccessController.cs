using Cddo.Data.Marketplace.Api.Dto.Responses.RequestAccess;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers
{
    [Route("[controller]")]
    public class RequestAccessController : Controller
    {
        private readonly IRequestAccessService _requestAccessService;
        private readonly IUserRoleService _userRoleService;
        private readonly ILogger<RequestAccessController> _logger;
        public RequestAccessController(
            IRequestAccessService requestAccessService,
            IUserRoleService userRoleService,
            ILogger<RequestAccessController> logger)
        {
            _requestAccessService = requestAccessService;
            _userRoleService = userRoleService;
            _logger = logger;
        }


        [Route("ManageOrganisation")]
        public IActionResult ManageOrganisation()
        {
            return View("~/Pages/Manage/RequestAccess/ManageOrganisationAccess.cshtml");
        }

        [Route("UpdateRequest")]
        public async Task<IActionResult> UpdateOrganisationRequest(OrganisationAccessResponse organisationAccessResponse)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("UpdateRequest validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            if (User.Identity!.IsAuthenticated)
            {
                bool? isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.GetValueOrDefault())
                {
                    var result = await _requestAccessService.GetOrganisationRequestByIdAsync(organisationAccessResponse.OrganisationRequestID);

                    result.OrganisationName = organisationAccessResponse.OrganisationName;
                    result.OrganisationType = organisationAccessResponse.OrganisationType;
                    result.DomainName = organisationAccessResponse.DomainName;

                    await _requestAccessService.UpdateOrganisationRequestAsync(result);
                    return RedirectToAction(nameof(GetOrganisationRequest), new { organisationRequestID = result.OrganisationRequestID });
                }
            }
            return RedirectToPage("/Error/403");
        }

        [Route("GetOrganisationsRequests")]
        public async Task<IActionResult> GetOrganisationsRequest()
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("GetOrganisationsRequests validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            if (User.Identity!.IsAuthenticated)
            {
                bool? isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.GetValueOrDefault())
                {
                    var result = await _requestAccessService.GetOrganisationAllRequestAsync();
                    return View("~/Pages/Manage/RequestAccess/OrganisationRequests.cshtml", result);
                }
            }
            return RedirectToPage("/Error/403");
        }

        [HttpGet(Name = "GetOrganisationRequest")]
        public async Task<IActionResult> GetOrganisationRequest(int organisationRequestID)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("GetOrganisationRequest validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            if (User.Identity!.IsAuthenticated)
            {
                bool? isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.GetValueOrDefault())
                {
                    var result = await _requestAccessService.GetOrganisationRequestByIdAsync(organisationRequestID);
                    return View("~/Pages/Manage/RequestAccess/ManageOrganisationAccess.cshtml", result);
                }
            }
            return RedirectToPage("/Error/403");
        }

        [Route("RejectAccessRequest")]
        public IActionResult RejectOrganisationAccess(int organisationRequestID)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Pages/Manage/RequestAccess/RejectAccessRequest.cshtml", new OrganisationAccessResponse() { OrganisationRequestID = organisationRequestID });
            }

            return View("~/Pages/Manage/RequestAccess/RejectAccessRequest.cshtml", new OrganisationAccessResponse() { OrganisationRequestID = organisationRequestID });
        }

        [Route("EditAccessRequest")]
        public async Task<IActionResult> EditOrganisationAccess(int organisationRequestID)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("EditAccessRequest validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            var result = await _requestAccessService.GetOrganisationRequestByIdAsync(organisationRequestID);
            return View("~/Pages/Manage/RequestAccess/EditOrganisation.cshtml", result);
        }

        [Route("UpdateAccessStatus")]
        public async Task<IActionResult> UpdateAccessStatus(int organisationRequestID, string status, string reason)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

                if (validationErrors.Count() > 0)
                {
                    _logger.LogError("UpdateAccessStatus validation errors: {ValidationErrors}", string.Join("; ", validationErrors));
                }
            }

            if (User.Identity!.IsAuthenticated)
            {
                bool? isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin.GetValueOrDefault())
                {
                    var result = await _requestAccessService.GetOrganisationRequestByIdAsync(organisationRequestID);

                    result.Status = status;
                    result.Reason = reason;

                    var updateStatus = await _requestAccessService.UpdateOrganisationRequestAsync(result);

                    if (status == "Approved")
                    {
                        return RedirectToPage("/Manage/ManageOrganisation", new { id = updateStatus });
                    }
                }
            }

            return RedirectToAction(nameof(GetOrganisationsRequest));
        }
    }
}
