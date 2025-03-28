using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;
using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.Supplier;
using Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Builders;
using Cddo.Data.Marketplace.UI.Pages.DataRequest;
using Cddo.Data.Marketplace.UI.Pages.DataShare;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("[controller]")]
[Authorize]
public class DataRequestController(
    IDataShareRequestService dataShareRequestService,
    IQuestionDataBuilder questionDataBuilder, IAppInsightsLogger logger, IUserRoleService userRoleService) : Controller
{

    private const string DataShareRequestEvent = "DataShareRequest";
    private const string RequestEvent = "Request";
    private const string RequestTasksAction = "RequestTasks";
    private const string NotificationEvent = "Notification";
    private const string AcceptEvent = "Accept";

    #region Questions
    [HttpGet("Request/{esdaId:guid}/Questions")]
    public async Task<IActionResult> RequestTasksListQuestions(Guid esdaId, string esdaName, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for RequestTasksListQuestions.");
        }

        var data = await dataShareRequestService.GetEsdaQuestionSetOutline(esdaId, cancellationToken);
        ViewBag.EsdaName = esdaName;
        //Log Questions get
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var response = await userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(response);

            userEventProperties.Add("esdaId", esdaId.ToString());
            userEventProperties.Add("esdaName", esdaName);

            logger.LogEventMainBase(UserEvent.UserPageNavigation, "RequestTasksListQuestions", "CDDO", "", "", "", userEventProperties);
        }

        return View("~/Pages/DataShare/RequestTasksListQuestions.cshtml", data);
    }
    [HttpGet("Question/{requestId:Guid}/{questionId:Guid}")]
    public async Task<IActionResult> RequestQuestion(Guid requestId, Guid questionId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for RequestQuestion.");
        }

        try
        {
            var dataShareRequestQuestionInformationResponse = await dataShareRequestService.QuestionSummary(requestId, questionId, cancellationToken);

            var questionModel = questionDataBuilder.BuildQuestionModelFromDataShareRequestQuestion(
                dataShareRequestQuestionInformationResponse.DataShareRequestQuestion);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                userEventProperties.Add("questionId", questionId.ToString());
                userEventProperties.Add("requestId", requestId.ToString());

                logger.LogEventMainBase(UserEvent.UserPageNavigation, "Question", "CDDO", "", "", "", userEventProperties);
            }

            return View("~/Pages/DataShare/Question.cshtml", questionModel);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    #endregion

    #region Tasks
    [HttpGet("Tasks/{requestId:Guid}")]
    public async Task<IActionResult> RequestTasks(Guid requestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for RequestTasks.");
        }

        try
        {
            var getDataShareRequestQuestionsSummaryResponse = await dataShareRequestService.QuestionsSummary(requestId, cancellationToken);

            var getDataShareRequestAuditLogResponse = await dataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                requestId, cancellationToken);

            var userCanDeleteDataShareRequest = await DetermineWhetherInitiatingUserCanDeleteDataShareRequestAsync(
                getDataShareRequestQuestionsSummaryResponse);

            ViewBag.RequestRequestId = getDataShareRequestQuestionsSummaryResponse.DataShareRequestRequestId;

            var requestTasksModel = new RequestTasksModel
            {
                DataShareRequestId = getDataShareRequestQuestionsSummaryResponse.DataShareRequestId,
                DataShareRequestRequestId = getDataShareRequestQuestionsSummaryResponse.DataShareRequestRequestId,
                EsdaName = getDataShareRequestQuestionsSummaryResponse.EsdaName,
                QuestionSetSummary = getDataShareRequestQuestionsSummaryResponse.QuestionSetSummary,
                DataShareRequestAuditLog = getDataShareRequestAuditLogResponse.DataShareRequestAuditLog,
                UserCanDeleteDataShareRequest = userCanDeleteDataShareRequest
            };

            return View("~/Pages/DataShare/RequestTasks.cshtml", requestTasksModel);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }

        async Task<bool> DetermineWhetherInitiatingUserCanDeleteDataShareRequestAsync(
            GetDataShareRequestQuestionsSummaryResponse dataShareRequestQuestionsSummaryResponse)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Model state is invalid for DetermineWhetherInitiatingUserCanDeleteDataShareRequestAsync.");
            }

            try
            {
                var initiatingUserProfile = await userRoleService.GetUserProfileAsync();

                var dataShareRequestAcquirerUserDetails =
                    dataShareRequestQuestionsSummaryResponse.QuestionSetSummary.AcquirerUserDetails;

                var userIsTheOrganisationOfTheDataShareRequest =
                    initiatingUserProfile.Organisation?.OrganisationId == dataShareRequestAcquirerUserDetails.OrganisationId;

                if (userIsTheOrganisationOfTheDataShareRequest) return true;

                var rolesThatCanDeleteDataShareRequest = new List<string>
                {
                    "Data Explorer",
                    "System Administrator"
                };

                var userHasARoleThatCanDeleteDataShareRequest = await userRoleService.IsUserInRoleAsync(
                    rolesThatCanDeleteDataShareRequest);

                return userHasARoleThatCanDeleteDataShareRequest;
            }
            catch
            {
                return false;
            }
        }
    }

    [HttpGet("Tasks/{requestId:Guid}/Review-your-answers")]
    public async Task<IActionResult> RequestTasksReviewAnswers(Guid requestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for RequestTasksReviewAnswers.");
        }

        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/403");
        }

        try
        {
            var getAnswersSummaryResponse = await dataShareRequestService.GetAnswerSummary(requestId, cancellationToken);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                userEventProperties.Add("requestId", requestId.ToString());

                logger.LogEventMainBase(UserEvent.UserPageNavigation, "ReviewAnswers", "CDDO", "", "", "", userEventProperties);
            }

            var getDataShareRequestAuditLogResponse = await dataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                requestId, cancellationToken);

            var reviewAnswersModel = new ReviewAnswersModel
            {
                AnswersSummary = getAnswersSummaryResponse.AnswersSummary,
                DataShareRequestAuditLog = getDataShareRequestAuditLogResponse.DataShareRequestAuditLog
            };

            return View("~/Pages/DataShare/ReviewAnswers.cshtml", reviewAnswersModel);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpGet("Tasks/{requestId:Guid}/Read-answers")]
    public async Task<IActionResult> RequestTasksReadAnswers(Guid requestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/403");
        }

        try
        {
            var getAnswersSummaryResponse = await dataShareRequestService.GetAnswerSummary(requestId, cancellationToken);

            var userCanDeleteDataShareRequest = await DetermineWhetherInitiatingUserCanDeleteDataShareRequestAsync();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                userEventProperties.Add("requestId", requestId.ToString());

                logger.LogEventMainBase(UserEvent.UserPageNavigation, "ReviewAnswers", "CDDO", "", "", "", userEventProperties);
            }

            return View("~/Pages/DataShare/ReviewReadOnlyAnswers.cshtml", new ReviewReadOnlyAnswersModel
            {
                DataShareRequestId = getAnswersSummaryResponse.DataShareRequestId,
                AnswersSummary = getAnswersSummaryResponse.AnswersSummary,
                UserCanDeleteDataShareRequest = userCanDeleteDataShareRequest
            });
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }

        async Task<bool> DetermineWhetherInitiatingUserCanDeleteDataShareRequestAsync()
        {
            try
            {
                var rolesThatCanDeleteDataShareRequest = new List<string>
                {
                    "System Administrator"
                };

                var userHasARoleThatCanDeleteDataShareRequest = await userRoleService.IsUserInRoleAsync(
                    rolesThatCanDeleteDataShareRequest);

                return userHasARoleThatCanDeleteDataShareRequest;
            }
            catch
            {
                return false;
            }
        }
    }

    [Route("Tasks/{requestId:guid}/Delete-read-answers-request")]
    public IActionResult DeleteReadAnswersRequest(Guid requestId, string dataShareRequestRequestId, string esdaName)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DeleteReadAnswersRequest.");
        }

        return View("~/Pages/DataRequest/DeleteReadAnswersRequest.cshtml", new DeleteReadAnswersRequestModel
        {
            DataShareRequestId = requestId,
            DataShareRequestRequestId = dataShareRequestRequestId,
            EsdaName = esdaName
        });
    }

    [HttpPost("Tasks/{requestId:guid}/Delete-read-answers-request-confirm")]
    public async Task<IActionResult> ConfirmDeleteReadAnswersRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ConfirmDeleteReadAnswersRequest.");
        }

        try
        {
            await dataShareRequestService.DeleteDataShareRequest(requestId, Request.HttpContext.RequestAborted);

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestDeleted, DataShareRequestEvent, "CDDO", "RequestDeleteReadOnlySubmit", RequestEvent, requestId.ToString(), userEventProperties);
            }

            return RedirectToAction(nameof(ManageDataRequestController.GotoManageCreatedDataShare), "ManageDataRequest",
                new GetDataShareRequestSummariesRequest());
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Tasks/{requestId:guid}/Delete-read-answers-request-cancel")]
    public async Task<IActionResult> CancelDeleteReadAnswersRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for CancelDeleteReadAnswersRequest.");
        }

        return RedirectToAction("RequestTasksReadAnswers", new { requestId });
    }

    [AutoValidateAntiforgeryToken]
    [HttpPost("Tasks/{requestId:Guid}/Submit-data-share-request")]
    public async Task<IActionResult> RequestSubmitDataShareRequest(Guid requestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            logger.LogEventMainBase(DataSharingEvent.DataSharingSubmissionNotes, "SubmissionValidation", "CDDO", "SubmissionNotes", "Notes", requestId.ToString(), new Dictionary<string, string>
        {{ "ValidationErrors", string.Join(", ", validationErrors) },{ "RequestId", requestId.ToString()  }});
        }

        try
        {
            //Log Questions get
            var submitDataShareRequestResponse = await dataShareRequestService.SubmitDataShareRequest(requestId, cancellationToken);

            var requestRequestId = submitDataShareRequestResponse.DataShareRequestRequestId;
            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                userEventProperties.Add("requestId", requestId.ToString());
                userEventProperties.Add("requestRequestId", requestRequestId.ToString());
                logger.LogEventMainBase(UserEvent.UserDataShareRequestEnd, "DataShareRequestComplete", "CDDO", "", "", "", userEventProperties);

                var notificationEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                notificationEventProperties.Add("requestId", requestId.ToString());
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestNotification, string.Empty, "CDDO", NotificationEvent, "Submitted", submitDataShareRequestResponse.NotificationSuccess.ToString(), notificationEventProperties);
            }
            return RedirectToAction("DataShareRequestComplete", new { requestRequestId });

        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpGet("Tasks/{requestRequestId}/Request-submitted")]
    public IActionResult DataShareRequestComplete(string requestRequestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DataShareRequestComplete.");
        }

        try
        {
            ViewBag.RequestRequestId = requestRequestId;

            return View("~/Pages/DataShare/DataShareRequestComplete.cshtml");
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Tasks/{requestId:Guid}/{questionId:Guid}/set-and-continue")]
    public async Task<IActionResult> SetQuestionAnswerAndContinue(Guid requestId, Guid questionId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for SetQuestionAnswerAndContinue.");
        }

        //Log Questions get
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userResponse = await userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

            userEventProperties.Add("requestId", requestId.ToString());
            userEventProperties.Add("questionId", questionId.ToString());

            logger.LogEventMainBase(UserEvent.UserSetQuestionAnswer, "Question", "CDDO", "", "", "", userEventProperties);
        }
        return await DoSetQuestionAnswer(requestId, true, form, cancellationToken);
    }

    [HttpPost("Tasks/{requestId:Guid}/{questionId:Guid}/set-and-return")]
    public async Task<IActionResult> SetQuestionAnswerAndReturn(Guid requestId, Guid questionId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for SetQuestionAnswerAndReturn.");
        }

        return await DoSetQuestionAnswer(requestId, false, form, cancellationToken);
    }

    #endregion

    #region Datashare
    [HttpGet("Request/{esdaId:guid}/Previous/")]
    public async Task<IActionResult> DataShareRequestPrevious(Guid esdaId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DataShareRequestPrevious.");
        }

        var response = await dataShareRequestService.GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(
            esdaId, cancellationToken);

        var esdaName = response.DataShareRequestSummaries.EsdaName;

        //Log Questions get
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userResponse = await userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

            userEventProperties.Add("esdaId", esdaId.ToString());
            userEventProperties.Add("esdaName", esdaName);

            logger.LogEventMainBase(UserEvent.UserPageNavigation, "DataShareRequestPrevious", "CDDO", "", "", "", userEventProperties);
        }

        return View("~/Pages/DataShare/DataShareRequestPrevious.cshtml", new DataShareRequestPreviousModel
        {
            EsdaName = esdaName,
            EsdaId = esdaId,
            DataShareRequestSummaries = response.DataShareRequestSummaries
        });
    }

    [AutoValidateAntiforgeryToken]

    [HttpPost("Start")]
    public async Task<IActionResult> RequestStartSubmit(Guid esdaId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for RequestStartSubmit.");
        }

        try
        {
            var requestId = await dataShareRequestService.StartDataSharingRequest(esdaId, cancellationToken);
            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestCreated, DataShareRequestEvent, "CDDO", "RequestStartSubmit", RequestEvent, esdaId.ToString(), userEventProperties);
            }

            //logger.LogDataSharingEvent(EventTypes.DataSharingEvent.DataSharingRequestCreated, DataShareRequestEvent, "CDDO", RequestStartSubmit", esdaId.ToString(),)
            return RedirectToAction(RequestTasksAction, new { requestId, esdaId });
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/DeclareRequestSubmission")]
    public async Task<IActionResult> DeclareRequestSubmission(Guid requestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DeclareRequestSubmission.");
        }
        try
        {
            var submitDataShareRequestResponse = await dataShareRequestService.SubmitDataShareRequest(requestId, cancellationToken);

            var requestRequestId = submitDataShareRequestResponse.DataShareRequestRequestId;
            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                userEventProperties.Add("requestId", requestId.ToString());
                userEventProperties.Add("requestRequestId", requestRequestId.ToString());
                logger.LogEventMainBase(UserEvent.UserDataShareRequestEnd, "DataShareRequestComplete", "CDDO", "", "", "", userEventProperties);

                var notificationEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                notificationEventProperties.Add("requestId", requestId.ToString());
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestNotification, string.Empty, "CDDO", NotificationEvent, "Submitted", submitDataShareRequestResponse.NotificationSuccess.ToString(), notificationEventProperties);
            }
            return RedirectToAction("DataShareRequestComplete", new { requestRequestId });
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Dashboard")]
    public async Task<IActionResult> RequestDashboard()
    {
        if (User.Identity!.IsAuthenticated)
        {
            return View("~/Pages/DataRequest/RequestDashboard.cshtml");
        }

        return RedirectToPage("/Index");
    }

    [Route("Created")]
    public async Task<IActionResult> CreatedRequests()
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for CreatedRequests.");
        }

        try
        {

            var requests = await dataShareRequestService.GetAcquirerDataShareRequests(new List<DataShareRequestStatus>(), Request.HttpContext.RequestAborted);

            return View("~/Pages/DataRequest/CreatedRequests.cshtml", requests);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Created/{requestId:guid}/Cancel-request")]
    public IActionResult CancelCreatedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for CancelCreatedRequest.");
        }

        return View("~/Pages/DataRequest/CancelCreatedRequest.cshtml", new CancelCreatedRequestModel { DataShareRequestId = requestId });
    }

    [HttpPost("Created/{requestId:guid}/Cancel-request-confirm")]
    public async Task<IActionResult> ConfirmCancelCreatedRequest(Guid requestId, IFormCollection form)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ConfirmCancelCreatedRequest.");
        }

        try
        {
            var reasonsForCancellation = form.TryGetValue("cancellation-reasons", out var reasonsForCancellationValues)
                ? reasonsForCancellationValues.ToString()
                : "";

            var cancelDataShareRequestResponse = await dataShareRequestService.CancelDataShareRequest(requestId, reasonsForCancellation, Request.HttpContext.RequestAborted);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestCancelled, DataShareRequestEvent, "CDDO", "RequestStartSubmit", RequestEvent, requestId.ToString(), userEventProperties);

                var notificationEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                notificationEventProperties.Add("requestId", requestId.ToString());
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestNotification, string.Empty, "CDDO", NotificationEvent, "Cancelled", cancelDataShareRequestResponse.NotificationSuccess.ToString(), notificationEventProperties);
            }

            return RedirectToAction("CreatedRequests");
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Created/{requestId:guid}/Delete-request")]
    public IActionResult DeleteCreatedRequest(Guid requestId, string dataShareRequestRequestId, string esdaName)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DeleteCreatedRequest.");
        }

        return View("~/Pages/DataRequest/DeleteCreatedRequest.cshtml", new DeleteCreatedRequestModel
        {
            DataShareRequestId = requestId,
            DataShareRequestRequestId = dataShareRequestRequestId,
            EsdaName = esdaName
        });
    }

    [HttpPost("Created/{requestId:guid}/Delete-request-confirm")]
    public async Task<IActionResult> ConfirmDeleteCreatedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ConfirmDeleteCreatedRequest.");
        }

        try
        {
            await dataShareRequestService.DeleteDataShareRequest(requestId, Request.HttpContext.RequestAborted);

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestDeleted, DataShareRequestEvent, "CDDO", "RequestDeleteSubmit", RequestEvent, requestId.ToString(), userEventProperties);
            }

            return RedirectToAction("CreatedRequests");
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Created/{requestId:guid}/Delete-request-cancel")]
    public async Task<IActionResult> CancelDeleteCreatedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for CancelDeleteCreatedRequest.");
        }

        return RedirectToAction(RequestTasksAction, new { requestId });
    }



    #endregion

    #region privates

    private async Task<IActionResult> DoSetQuestionAnswer(
    Guid requestId, bool showNextQuestionIfValidResponsesGiven, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DoSetQuestionAnswer.");
        }

        try
        {
            var dataShareRequestQuestionAnswer = questionDataBuilder.BuildQuestionAnswerFromFormData(form);
            var setDataShareRequestQuestionAnswerResponse = await SubmitAnswerQuestionAsync(dataShareRequestQuestionAnswer, cancellationToken);

            if (!setDataShareRequestQuestionAnswerResponse.Result.AnswerIsValid)
            {
                return await HandleInvalidAnswerAsync(requestId, setDataShareRequestQuestionAnswerResponse.Result);
            }

            if (showNextQuestionIfValidResponsesGiven == false)
            {
                return RedirectToAction(RequestTasksAction, new { requestId });
            }

            return await HandleValidAnswerAsync(requestId, setDataShareRequestQuestionAnswerResponse.Result);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    private async Task<SetDataShareRequestQuestionAnswerResponse> SubmitAnswerQuestionAsync(
        DataShareRequestQuestionAnswer dataShareRequestQuestionAnswer, CancellationToken cancellationToken)
    {
        return await dataShareRequestService.SubmitAnswerQuestion(
            new SetDataShareRequestQuestionAnswerRequest { DataShareRequestQuestionAnswer = dataShareRequestQuestionAnswer },
            cancellationToken);
    }

    private async Task<IActionResult> HandleInvalidAnswerAsync(Guid requestId, SetDataShareRequestQuestionAnswerResult result)
    {
        var questionModel = questionDataBuilder.BuildQuestionModelFromDataShareRequestQuestion(result.QuestionInformation);
        await LogEventMainBaseAsync(requestId, questionModel.QuestionId, UserEvent.UserSetQuestionAnswerInvalid);

        return View("~/Pages/DataShare/Question.cshtml", questionModel);
    }

    private async Task<IActionResult> HandleValidAnswerAsync(Guid requestId, SetDataShareRequestQuestionAnswerResult result)
    {
        if (result.NextQuestionId.HasValue)
        {
            await LogEventMainBaseAsync(requestId, result.NextQuestionId.Value, UserEvent.UserSetQuestionAnswerInvalid);
            return RedirectToAction("RequestQuestion", new { requestId, questionId = result.NextQuestionId.Value });
        }

        await LogEventMainBaseAsync(requestId, null, UserEvent.UserSetQuestionAnswerCompleted);

        return result.DataShareRequestQuestionsRemainThatRequireAResponse
            ? RedirectToAction(RequestTasksAction, new { requestId })
            : RedirectToAction("RequestTasksReviewAnswers", new { requestId });
    }

    private async Task LogEventMainBaseAsync(Guid requestId, Guid? questionId, UserEvent userEvent)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userResponse = await userRoleService.GetUserProfileAsync();
            var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

            userEventProperties.Add("requestId", requestId.ToString());
            if (questionId.HasValue)
            {
                userEventProperties.Add("questionId", questionId.Value.ToString());
            }

            logger.LogEventMainBase(userEvent, "Question", "CDDO", "", "", "", userEventProperties);
        }
    }
    #endregion

    #region Supplier Interface
    [Route("Received")]
    public async Task<IActionResult> ReceivedRequests()
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ReceivedRequests.");
        }

        try
        {
            var roles = new List<string> { "System Administrator", "Organisation Administrator", "Data Request Approver" };
            bool? isAGMAdministrator = await userRoleService.IsUserInRoleAsync(roles);
            if (isAGMAdministrator.HasValue && isAGMAdministrator.Value)
            {
                var getSubmissionSummariesResponse = await dataShareRequestService.GetSupplierDataShareRequests(Request.HttpContext.RequestAborted);

                return View("~/Pages/DataRequest/ReceivedRequests.cshtml", getSubmissionSummariesResponse.SubmissionSummariesSet);
            }
            return RedirectToPage("/Error/NoPermissions", new { requiredPermission = "datarequest" });
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Received/{requestId:guid}/New")]
    public async Task<IActionResult> ReviewNewReceivedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ReviewNewReceivedRequest.");
        }

        try
        {
            var getSubmissionInformationResponse = await dataShareRequestService.GetSubmissionInformation(requestId, Request.HttpContext.RequestAborted);

            return View("~/Pages/DataRequest/ReviewNewReceivedRequest.cshtml", getSubmissionInformationResponse.SubmissionInformation);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Received/{requestId:guid}/InProgress")]
    public async Task<IActionResult> ReviewInProgressReceivedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ReviewInProgressReceivedRequest.");
        }

        try
        {
            var getSubmissionReviewInformationResponse = await dataShareRequestService.GetSubmissionReviewInformation(requestId, Request.HttpContext.RequestAborted);

            var getDataShareRequestAuditLogResponse = await dataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                requestId);

            var reviewInProgressReceivedRequestModel = new ReviewInProgressReceivedRequestModel
            {
                SubmissionDetails = getSubmissionReviewInformationResponse.SubmissionReviewInformation.SubmissionDetails,
                SupplierNotes = getSubmissionReviewInformationResponse.SubmissionReviewInformation.SupplierNotes,
                DataShareRequestAuditLog = getDataShareRequestAuditLogResponse.DataShareRequestAuditLog
            };

            return View("~/Pages/DataRequest/ReviewInProgressReceivedRequest.cshtml", reviewInProgressReceivedRequestModel);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Received/{requestId:guid}/Returned")]
    public async Task<IActionResult> ViewReturnedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ViewReturnedRequest.");
        }

        try
        {
            var getReturnedSubmissionInformationResponse = await dataShareRequestService.GetReturnedSubmissionInformation(requestId, Request.HttpContext.RequestAborted);

            return View("~/Pages/DataRequest/ViewReturnedRequest.cshtml", getReturnedSubmissionInformationResponse.ReturnedSubmissionInformation);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Received/{requestId:guid}/CompletedDetails")]
    public async Task<IActionResult> ViewCompletedSubmissionDetails(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ViewCompletedSubmissionDetails.");
        }

        try
        {
            var getSubmissionDetailsResponse = await dataShareRequestService.GetSubmissionDetails(requestId, Request.HttpContext.RequestAborted);

            var getDataShareRequestAuditLogResponse = await dataShareRequestService.GetDataShareRequestReturnCommentsAuditLog(
                requestId);

            var completedSubmissionDetailsModel = new CompletedSubmissionDetailsModel
            {
                SubmissionDetails = getSubmissionDetailsResponse.SubmissionDetails,
                DataShareRequestAuditLog = getDataShareRequestAuditLogResponse.DataShareRequestAuditLog
            };

            return View("~/Pages/DataRequest/CompletedSubmissionDetails.cshtml", completedSubmissionDetailsModel);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/Start")]
    public async Task<IActionResult> StartSubmissionReview(Guid requestId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for StartSubmissionReview.");
        }

        try
        {
            await dataShareRequestService.StartSubmissionReview(requestId, cancellationToken);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingSubmissionReview, "ReviewInProgressReceivedRequest", "CDDO", "SubmissionReview", "Review", requestId.ToString(), userEventProperties);
            }

            return RedirectToAction(nameof(ReviewInProgressReceivedRequest), new { requestId = requestId });
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/NotesAndContinue")]
    public async Task<IActionResult> SetSubmissionNotesAndContinue(Guid requestId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for SetSubmissionNotesAndContinue.");
        }

        return await SetSubmissionNotes(requestId, true, form, cancellationToken);
    }

    [HttpPost("Received/{requestId:guid}/NotesAndReturn")]
    public async Task<IActionResult> SetSubmissionNotesAndReturn(Guid requestId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for SetSubmissionNotesAndReturn.");
        }

        return await SetSubmissionNotes(requestId, false, form, cancellationToken);
    }

    private async Task<IActionResult> SetSubmissionNotes(Guid requestId, bool showDecisionPage, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for SetSubmissionNotes.");
        }

        try
        {
            var supplierNotes = (form.TryGetValue("supplier-notes", out var supplierNotesValues))
                ? supplierNotesValues.ToString()
                : "";

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingSubmissionNotes, "SubmissionDecision", "CDDO", "SubmissionNotes", "Notes", requestId.ToString(), userEventProperties);
            }

            await dataShareRequestService.SetSubmissionNotes(requestId, supplierNotes, cancellationToken);

            if (!showDecisionPage)
            {
                return await ReceivedRequests();
            }

            return RedirectToAction(nameof(ShowSubmissionDecision), new { requestId = requestId });
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("Received/{requestId:guid}/Submission-Decision")]
    public async Task<IActionResult> ShowSubmissionDecision(
        Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ShowSubmissionDecision.");
        }

        try
        {
            var getSubmissionInformationResponse = await dataShareRequestService.GetSubmissionInformation(requestId, Request.HttpContext.RequestAborted);

            return View("~/Pages/DataRequest/SubmissionDecision.cshtml", getSubmissionInformationResponse.SubmissionInformation);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/Accept")]
    public async Task<IActionResult> AcceptReceivedRequest(Guid requestId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for AcceptReceivedRequest.");
        }

        try
        {
            var acceptanceFeedbackForAcquirer = (form.TryGetValue("acceptance-feedback-for-acquirer", out var acceptanceFeedbackForAcquirerValues))
                ? acceptanceFeedbackForAcquirerValues.ToString()
                : "";

            var requestAcceptanceDeclarationModel = new RequestAcceptanceDeclarationModel
            {
                DataShareRequestId = requestId,
                FeedbackToAcquirer = acceptanceFeedbackForAcquirer
            };

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestReceived, "RequestAcceptanceDeclaration", "CDDO", RequestEvent, AcceptEvent, requestId.ToString(), userEventProperties);
            }

            return View("~/Pages/DataRequest/RequestAcceptanceDeclaration.cshtml", requestAcceptanceDeclarationModel);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/DeclareAcceptance")]
    public async Task<IActionResult> DeclareRequestAcceptance(Guid requestId, string acceptanceFeedbackForAcquirer, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DeclareRequestAcceptance.");
        }

        ViewData.ModelState.Clear();

        try
        {
            var acceptSubmissionResponse = await dataShareRequestService.AcceptReceivedRequest(requestId, acceptanceFeedbackForAcquirer, cancellationToken);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestAccepted, "RequestAccepted", "CDDO", RequestEvent, AcceptEvent, requestId.ToString(), userEventProperties);

                var notificationEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                notificationEventProperties.Add("requestId", requestId.ToString());
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestNotification, string.Empty, "CDDO", NotificationEvent, "Approved", acceptSubmissionResponse.NotificationSuccess.ToString(), notificationEventProperties);
            }

            return View("~/Pages/DataRequest/RequestAccepted.cshtml", acceptSubmissionResponse.AcceptedDecisionSummary);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/Reject")]
    public async Task<IActionResult> RejectReceivedRequest(Guid requestId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for RejectReceivedRequest.");
        }

        ViewData.ModelState.Clear();

        try
        {
            var rejectionFeedbackForAcquirer = (form.TryGetValue("rejection-feedback-for-acquirer", out var rejectionFeedbackForAcquirerValues))
                ? rejectionFeedbackForAcquirerValues.ToString()
                : "";

            var rejectSubmissionResponse = await dataShareRequestService.RejectReceivedRequest(requestId, rejectionFeedbackForAcquirer, cancellationToken);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestRejected, "RequestRejected", "CDDO", RequestEvent, AcceptEvent, requestId.ToString(), userEventProperties);

                var notificationEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                notificationEventProperties.Add("requestId", requestId.ToString());
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestNotification, string.Empty, "CDDO", NotificationEvent, "Rejected", rejectSubmissionResponse.NotificationSuccess.ToString(), notificationEventProperties);
            }
            return View("~/Pages/DataRequest/RequestRejected.cshtml", rejectSubmissionResponse.RejectedDecisionSummary);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [HttpPost("Received/{requestId:guid}/Return")]
    public async Task<IActionResult> ReturnReceivedRequest(Guid requestId, IFormCollection form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ReturnReceivedRequest.");
        }

        ViewData.ModelState.Clear();

        try
        {
            var returnFeedbackForAcquirer = (form.TryGetValue("return-feedback-for-acquirer", out var returnFeedbackForAcquirerValues))
                ? returnFeedbackForAcquirerValues.ToString()
                : "";

            if (string.IsNullOrWhiteSpace(returnFeedbackForAcquirer))
            {
                ViewData.ModelState.AddModelError("decision-return", "Comments are required");

                return View("~/Pages/DataRequest/SubmissionDecision.cshtml", new SubmissionInformation
                {
                    DataShareRequestId = requestId,
                    RequestStatus = FindDataShareRequestStatus()
                });
            }

            var returnSubmissionResponse = await dataShareRequestService.ReturnReceivedRequest(requestId, returnFeedbackForAcquirer, cancellationToken);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestReturned, "RequestReturned", "CDDO", RequestEvent, AcceptEvent, requestId.ToString(), userEventProperties);

                var notificationEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);
                notificationEventProperties.Add("requestId", requestId.ToString());
                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestNotification, string.Empty, "CDDO", NotificationEvent, "ReturnedWithComments", returnSubmissionResponse.NotificationSuccess.ToString(), notificationEventProperties);
            }

            return View("~/Pages/DataRequest/RequestReturned.cshtml", returnSubmissionResponse.ReturnedDecisionSummary);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }

        DataShareRequestStatus FindDataShareRequestStatus()
        {
            const DataShareRequestStatus defaultDataShareRequestStatus = DataShareRequestStatus.InReview;

            var requestStatusTextFound = form.TryGetValue("request-status", out var requestStatusValues);
            if (!requestStatusTextFound) return defaultDataShareRequestStatus;

            return Enum.TryParse<DataShareRequestStatus>(requestStatusValues.ToString(), out var dataShareRequestStatus)
                ? dataShareRequestStatus
                : defaultDataShareRequestStatus;
        }
    }

    [Route("Received/Completed/{requestId:guid}")]
    public async Task<IActionResult> ViewCompletedReceivedRequest(Guid requestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for ViewCompletedReceivedRequest.");
        }

        try
        {
            ViewBag.CreatedRequest = false;

            var completedSubmissionInformationResponse = await dataShareRequestService.GetCompletedReceivedRequest(requestId, Request.HttpContext.RequestAborted);

            return View("~/Pages/DataRequest/ViewCompletedRequest.cshtml", completedSubmissionInformationResponse.CompletedSubmissionInformation);
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }

    [Route("DownloadCompletedRequest")]
    public async Task<IActionResult> DownloadCompletedRequest(Guid requestId, string requestRequestId)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Model state is invalid for DownloadCompletedRequest.");
        }
        try
        {
            // Currently only support downloading in PDF
            var content = await dataShareRequestService.DownloadCompletedRequest(requestId, DataShareRequestFileFormat.Pdf, Request.HttpContext.RequestAborted);

            //Log Questions get
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userResponse = await userRoleService.GetUserProfileAsync();
                var userEventProperties = AuditUtility.ConvertUserProfileToJSONDictionary(userResponse);

                logger.LogEventMainBase(DataSharingEvent.DataSharingRequestAccepted, "RequestReturned", "CDDO", RequestEvent, AcceptEvent, requestId.ToString(), userEventProperties);
            }

            return File(content, "application/pdf", $"{requestRequestId}.pdf");
        }
        catch (DataShareRequestException ex)
        {
            var errorViewResult = GetViewResultDataShareRequestException(ex);
            if (errorViewResult != null) return errorViewResult;

            throw;
        }
    }
    #endregion


    private ViewResult? GetViewResultDataShareRequestException(DataShareRequestException ex)
    {
        return ex.DsrStatusCode switch
        {
            400 => View("~/Pages/Error/400.cshtml"),
            401 => View("~/Pages/Error/401.cshtml"),
            403 => View("~/Pages/Error/403.cshtml"),
            404 => View("~/Pages/Error/404.cshtml"),
            405 => View("~/Pages/Error/405.cshtml"),
            500 => View("~/Pages/Error/500.cshtml"),
            _ => null
        };
    }
}
