using Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class ManageDataRequestController : Controller
    {
        private readonly IDataShareRequestService _dataShareRequestService;
        private readonly IUserRoleService _userRoleService;
        private readonly ILogger<ManageDataRequestController> _logger;

        public ManageDataRequestController(
            IDataShareRequestService dataShareRequestService,
            IUserRoleService userRoleService,
            ILogger<ManageDataRequestController> logger)
        {
            _dataShareRequestService = dataShareRequestService;
            _userRoleService = userRoleService;
            _logger = logger;
        }

        [Route("Manage/ReceivedDataShare")]
        public async Task<IActionResult> GotoManageReceivedDataShare(GetDataShareRequestAdminSummariesRequest getDataShareRequestAdminSummariesRequest)
        {
            if (!await _userRoleService.IsUserRoleAdmin())
            {
                var user = await _userRoleService.GetUserProfileAsync();
                ViewBag.OrganisationName = user.Organisation!.OrganisationName;
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ReceivedDataShare model state is invalid");
            }

            var getDataShareRequestAdminSummariesResponse = await _dataShareRequestService.GetDataShareRequestAdminSummaries(getDataShareRequestAdminSummariesRequest, Request.HttpContext.RequestAborted);
            ViewBag.DataShareRequestStatuses = getDataShareRequestAdminSummariesRequest.DataShareRequestStatuses;
            ViewBag.SupplierOrganisationId = getDataShareRequestAdminSummariesRequest.SupplierOrganisationId;
            return View("~/Pages/Manage/DataShare/ReceivedDataShareRequests.cshtml", getDataShareRequestAdminSummariesResponse);
        }

        [Route("Manage/CreatedDataShare")]
        public async Task<IActionResult> GotoManageCreatedDataShare(GetDataShareRequestAdminSummariesRequest getDataShareRequestAdminSummariesRequest)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("CreatedDataShare model state is invalid");
            }

            var getDataShareRequestAdminSummariesResponse = await _dataShareRequestService.GetDataShareRequestAdminSummaries(getDataShareRequestAdminSummariesRequest, Request.HttpContext.RequestAborted);

            ViewBag.DataShareRequestStatuses = getDataShareRequestAdminSummariesRequest.DataShareRequestStatuses;
            ViewBag.AcquirerOrganisationId = getDataShareRequestAdminSummariesRequest.AcquirerOrganisationId;

            return View("~/Pages/Manage/DataShare/CreatedDataShareRequests.cshtml", getDataShareRequestAdminSummariesResponse);
        }
    }
}
