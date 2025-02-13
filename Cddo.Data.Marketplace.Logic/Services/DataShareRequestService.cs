using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Responses.Supplier;
using Agrimetrics.DataShare.Api.Dto.Requests.Supplier;
using Agrimetrics.DataShare.Api.Dto.Requests.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Responses.AuditLogs;

namespace Cddo.Data.Marketplace.Logic.Services;

public class DataShareRequestService : IDataShareRequestService
{
    private readonly string _apiUrl;
    private readonly ILogger<DataShareRequestService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRoleService _userRoleService;

    public static class QueryParameters
    {
        public const string DataShareRequestId = "DataShareRequestId";
        public const string AcquirerBaseRoute = "AcquirerDataShareRequest";
        public const string SupplierBaseRoute = "SupplierDataShareRequest";
        public const string AuditLogBaseRoute = "AuditLog";
    }

    public DataShareRequestService(

        ILogger<DataShareRequestService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IUserRoleService userRoleService)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor, nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        ArgumentNullException.ThrowIfNull(userRoleService, nameof(userRoleService));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _apiUrl = configuration.GetSection("Api:DataShare").Value!;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _userRoleService = userRoleService;
    }

    public async Task<Guid> StartDataSharingRequest(
        Guid esdaId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data sharing request for ESDA {EsdaId}", esdaId);

        try
        {
            const int tempAcquirerSupplierDomainId = 777;
            const int tempAcquirerSupplierOrganisationId = 999;

            var input = new StartDataShareRequestRequest
            {
                SupplierDomainId = tempAcquirerSupplierDomainId,
                SupplierOrganisationId = tempAcquirerSupplierOrganisationId,
                EsdaId = esdaId
            };

            // Log the constructed request for debugging
            _logger.LogDebug($"Sending StartDataShareRequest with payload: {input}");

            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "StartDataShareRequest")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<StartDataShareRequestResponse>();

            // Log the response for debugging
            _logger.LogDebug("Received response for starting Data Sharing request for ESDA Id {EsdaId}: {Response}", esdaId, response);

            return response.DataShareRequestId;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, $"Error in StartDataSharingRequest for {esdaId}");
        }
    }

    public async Task<GetDataShareRequestQuestionsSummaryResponse> QuestionsSummary(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetDataShareRequestQuestionsSummary")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetDataShareRequestQuestionsSummaryResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in QuestionsSummary");
        }
    }


    public async Task<GetDataShareRequestQuestionInformationResponse> QuestionSummary(Guid dataShareRequestId, Guid questionId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetDataShareRequestQuestionInformation")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .SetQueryParam("QuestionId", questionId)
                .GetJsonAsync<GetDataShareRequestQuestionInformationResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in QuestionSummary");
        }
    }

    public async Task<SetDataShareRequestQuestionAnswerResponse> SubmitAnswerQuestion(SetDataShareRequestQuestionAnswerRequest answers, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "SetDataShareRequestQuestionAnswer")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(answers, cancellationToken: cancellationToken)
                .ReceiveJson<SetDataShareRequestQuestionAnswerResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in SubmitAnswerQuestion");
        }
    }

    public async Task<GetDataShareRequestAnswersSummaryResponse> GetAnswerSummary(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetDataShareRequestAnswersSummary")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetDataShareRequestAnswersSummaryResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetAnswerSummary");
        }
    }

    public async Task<SubmitDataShareRequestResponse> SubmitDataShareRequest(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var body = new SubmitDataShareRequestRequest { DataShareRequestId = dataShareRequestId };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "SubmitDataShareRequest")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(body, cancellationToken: cancellationToken)
                .ReceiveJson<SubmitDataShareRequestResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in SubmitDataShareRequest");
        }
    }

    public async Task<GetDataShareRequestSummariesResponse> GetAcquirerDataShareRequests(IEnumerable<DataShareRequestStatus> dataShareRequestStatuses, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new GetAcquirerDataShareRequestSummariesRequest
            {
                DataShareRequestStatuses = dataShareRequestStatuses.ToList()
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetAcquirerDataShareRequestSummaries")
                .WithOAuthBearerToken(idToken)
                .AppendQueryParam(input)
                .GetJsonAsync<GetDataShareRequestSummariesResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetAcquirerDataShareRequests");
        }
    }

    public async Task<GetDataShareRequestAdminSummariesResponse?> GetDataShareRequestAdminSummaries(
        GetDataShareRequestAdminSummariesRequest getDataShareRequestAdminSummariesRequest,
        CancellationToken cancellationToken)
    {
        if (_httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
        {
            var isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            var isAdministrator = await _userRoleService.IsUserRoleAdmin();
            var isSupplier = await _userRoleService.IsUserRoleSupplier();

            if (!isSystemAdmin)
            {
                var userProfile = await _userRoleService.GetUserProfileAsync();
                if (userProfile.Organisation!.OrganisationId != getDataShareRequestAdminSummariesRequest.SupplierOrganisationId) return null;
            }

            if (isSystemAdmin || isAdministrator || isSupplier)
            {
                try
                {
                    var idToken = await GetUserBearerTokenAsync();

                    var response = await _apiUrl
                        .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetDataShareRequestAdminSummaries")
                        .WithOAuthBearerToken(idToken)
                        .AppendQueryParam(getDataShareRequestAdminSummariesRequest)
                        .GetJsonAsync<GetDataShareRequestAdminSummariesResponse>(cancellationToken: cancellationToken);

                    return response;
                }
                catch (FlurlHttpException ex)
                {
                    throw await HandleFlurlExceptionAsync(ex, "Error in GetDataShareRequestSummaries");
                }
            }
        }
        return null;
    }

    public async Task<GetEsdaQuestionSetOutlineResponse> GetEsdaQuestionSetOutline(Guid esdaId, CancellationToken cancellationToken)
    {
        try
        {
            const int tempAcquirerSupplierDomainId = 777;
            const int tempAcquirerSupplierOrganisationId = 999;

            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetEsdaQuestionSetOutline")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam("EsdaId", esdaId)
                .SetQueryParam("SupplierDomainId", tempAcquirerSupplierDomainId)
                .SetQueryParam("SupplierOrganisationId", tempAcquirerSupplierOrganisationId)
                .GetJsonAsync<GetEsdaQuestionSetOutlineResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetEsdaQuestionSetOutline");
        }
    }

    public async Task<GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse> GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(
        Guid esdaId,
        CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam("EsdaId", esdaId)
                .GetJsonAsync<GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation");
        }
    }

    public async Task<CancelDataShareRequestResponse> CancelDataShareRequest(Guid dataShareRequestId, string reasonsForCancellation,
        CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new CancelDataShareRequestRequest
            {
                DataShareRequestId = dataShareRequestId,
                ReasonsForCancellation = reasonsForCancellation ?? ""
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "CancelDataShareRequest")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<CancelDataShareRequestResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in CancelDataShareRequest");
        }
    }

    public async Task<DeleteDataShareRequestResponse> DeleteDataShareRequest(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new DeleteDataShareRequestRequest
            {
                DataShareRequestId = dataShareRequestId
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AcquirerBaseRoute, "DeleteDataShareRequest")
                .WithOAuthBearerToken(idToken)
                .SendJsonAsync(HttpMethod.Delete, input, cancellationToken: cancellationToken)
                .ReceiveJson<DeleteDataShareRequestResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in DeleteDataShareRequest");
        }
    }

    #region Supplier Interface

    public async Task<GetSubmissionSummariesResponse> GetSupplierDataShareRequests(CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetSubmissionSummaries")
                .WithOAuthBearerToken(idToken)
                .GetJsonAsync<GetSubmissionSummariesResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetSupplierDataShareRequests");
        }
    }

    public async Task<GetSubmissionInformationResponse> GetSubmissionInformation(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetSubmissionInformation")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetSubmissionInformationResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetSubmissionInformation");
        }
    }

    public async Task<GetSubmissionReviewInformationResponse> GetSubmissionReviewInformation(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetSubmissionReviewInformation")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetSubmissionReviewInformationResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetSubmissionReviewInformation");
        }
    }

    public async Task<GetReturnedSubmissionInformationResponse> GetReturnedSubmissionInformation(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetReturnedSubmissionInformation")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetReturnedSubmissionInformationResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetReturnedSubmissionInformation");
        }
    }

    public async Task<GetSubmissionDetailsResponse> GetSubmissionDetails(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetSubmissionDetails")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetSubmissionDetailsResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetSubmissionDetails");
        }
    }

    public async Task<StartSubmissionReviewResponse> StartSubmissionReview(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new StartSubmissionReviewRequest
            {
                DataShareRequestId = dataShareRequestId
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "StartSubmissionReview")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<StartSubmissionReviewResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in StartSubmissionReview");
        }
    }

    public async Task<SetSubmissionNotesResponse> SetSubmissionNotes(Guid dataShareRequestId, string notes, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new SetSubmissionNotesRequest
            {
                DataShareRequestId = dataShareRequestId,
                Notes = notes ?? ""
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "SetSubmissionNotes")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<SetSubmissionNotesResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in SetSubmissionNotes");
        }
    }

    public async Task<GetCompletedSubmissionInformationResponse> GetCompletedReceivedRequest(Guid dataShareRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetCompletedSubmissionInformation")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, dataShareRequestId)
                .GetJsonAsync<GetCompletedSubmissionInformationResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetCompletedReceivedRequest");
        }
    }

    public async Task<AcceptSubmissionResponse> AcceptReceivedRequest(Guid dataShareRequestId, string acceptanceFeedbackForAcquirer,
        CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new AcceptSubmissionRequest
            {
                DataShareRequestId = dataShareRequestId,
                CommentsToAcquirer = acceptanceFeedbackForAcquirer ?? ""
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "AcceptSubmission")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<AcceptSubmissionResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in AcceptReceivedRequest");
        }
    }

    public async Task<RejectSubmissionResponse> RejectReceivedRequest(Guid dataShareRequestId, string rejectionFeedbackForAcquirer,
        CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new RejectSubmissionRequest
            {
                DataShareRequestId = dataShareRequestId,
                CommentsToAcquirer = rejectionFeedbackForAcquirer ?? ""
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "RejectSubmission")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<RejectSubmissionResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in RejectReceivedRequest");
        }
    }

    public async Task<ReturnSubmissionResponse> ReturnReceivedRequest(
        Guid dataShareRequestId,
        string returnFeedbackForAcquirer,
        CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var input = new ReturnSubmissionRequest
            {
                DataShareRequestId = dataShareRequestId,
                CommentsToAcquirer = returnFeedbackForAcquirer ?? ""
            };

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "ReturnSubmission")
                .WithOAuthBearerToken(idToken)
                .PostJsonAsync(input, cancellationToken: cancellationToken)
                .ReceiveJson<ReturnSubmissionResponse>();

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in ReturnReceivedRequest");
        }
    }

    public async Task<byte[]> DownloadCompletedRequest(Guid requestId, DataShareRequestFileFormat fileFormat,
        CancellationToken cancellationToken)
    {
        try
        {
            var idToken = await GetUserBearerTokenAsync();

            var data = await _apiUrl
                .AppendPathSegments(QueryParameters.SupplierBaseRoute, "GetSubmissionContentAsFile")
                .WithOAuthBearerToken(idToken)
                .SetQueryParam(QueryParameters.DataShareRequestId, requestId)
                .SetQueryParam("FileFormat", fileFormat)
                .GetBytesAsync(cancellationToken: cancellationToken);

            return data;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in DownloadCompletedRequest");
        }
    }

    public async Task<GetDataShareRequestAuditLogResponse> GetDataShareRequestReturnCommentsAuditLog(
        Guid dataShareRequestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getDataShareRequestAuditLogRequest = new GetDataShareRequestAuditLogRequest
            {
                DataShareRequestId = dataShareRequestId,
                ToStatuses = [DataShareRequestStatus.Returned]
            };

            var idToken = await GetUserBearerTokenAsync();

            var response = await _apiUrl
                .AppendPathSegments(QueryParameters.AuditLogBaseRoute, "GetDataShareRequestAuditLog")
                .WithOAuthBearerToken(idToken)
                .SetQueryParams(getDataShareRequestAuditLogRequest)
                .GetJsonAsync<GetDataShareRequestAuditLogResponse>(cancellationToken: cancellationToken);

            return response;
        }
        catch (FlurlHttpException ex)
        {
            throw await HandleFlurlExceptionAsync(ex, "Error in GetDataShareRequestReturnCommentsAuditLog");
        }
    }

    #endregion

    private async Task<string> GetUserBearerTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) throw new DataShareRequestException
        {
            DsrStatusCode = StatusCodes.Status500InternalServerError,
            DsrResponseText = "Unable to obtain User Id Token.  Unable to obtain HttpContext",
            DsrExceptionText = "Unable to obtain User Id Token"
        };

        var idToken = httpContext.Request.Cookies["CO-Datamarketplace"];
        if (idToken == null) throw new DataShareRequestException
        {
            DsrStatusCode = StatusCodes.Status403Forbidden,
            DsrResponseText = "Unable to obtain User Id Token.  You were likely not signed in.  You should not see this message when the UI is properly working because you shouldn't be able to Start a DSR when you're not signed in",
            DsrExceptionText = "Unable to obtain User Id Token"
        };

        return idToken;
    }

    private async Task<DataShareRequestException> HandleFlurlExceptionAsync(FlurlHttpException ex, string messageBody)
    {
        var dataShareRequestException = await BuildDataShareRequestExceptionAsync();

        _logger.LogError(ex, "{MessageBody}: {DataShareRequestException}", messageBody, dataShareRequestException);

        return dataShareRequestException;

        async Task<DataShareRequestException> BuildDataShareRequestExceptionAsync()
        {
            return new DataShareRequestException
            {
                DsrStatusCode = ex.StatusCode,
                DsrResponseText = await ex.GetResponseStringAsync(),
                DsrExceptionText = ex.Message
            };
        }
    }
}
