using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Requests.Supplier;
using Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Responses.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Responses.Supplier;

namespace Cddo.Data.Marketplace.Logic.Services.Interfaces;
public interface IDataShareRequestService
{
    Task<Guid> StartDataSharingRequest(Guid esdaId, CancellationToken cancellationToken);

    Task<GetDataShareRequestQuestionsSummaryResponse> QuestionsSummary(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<GetDataShareRequestQuestionInformationResponse> QuestionSummary(Guid dataShareRequestId, Guid questionId, CancellationToken cancellationToken);

    Task<SetDataShareRequestQuestionAnswerResponse> SubmitAnswerQuestion(SetDataShareRequestQuestionAnswerRequest answers, CancellationToken cancellationToken);

    Task<GetDataShareRequestAnswersSummaryResponse> GetAnswerSummary(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<SubmitDataShareRequestResponse> SubmitDataShareRequest(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<GetDataShareRequestSummariesResponse> GetAcquirerDataShareRequests(IEnumerable<DataShareRequestStatus> dataShareRequestStatuses, CancellationToken cancellationToken);

    Task<GetDataShareRequestAdminSummariesResponse?> GetDataShareRequestAdminSummaries(GetDataShareRequestAdminSummariesRequest getDataShareRequestAdminSummariesRequest, CancellationToken cancellationToken);

    Task<GetEsdaQuestionSetOutlineResponse> GetEsdaQuestionSetOutline(Guid esdaId, CancellationToken cancellationToken);

    Task<GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse> GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(Guid esdaId, CancellationToken cancellationToken);

    Task<CancelDataShareRequestResponse> CancelDataShareRequest(Guid dataShareRequestId, string reasonsForCancellation, CancellationToken cancellationToken);

    Task<DeleteDataShareRequestResponse> DeleteDataShareRequest(Guid dataShareRequestId, CancellationToken cancellationToken);

    #region Supplier Interface
    Task<GetSubmissionSummariesResponse> GetSupplierDataShareRequests(CancellationToken cancellationToken);

    Task<GetSubmissionInformationResponse> GetSubmissionInformation(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<GetSubmissionReviewInformationResponse> GetSubmissionReviewInformation(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<GetReturnedSubmissionInformationResponse> GetReturnedSubmissionInformation(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<GetSubmissionDetailsResponse> GetSubmissionDetails(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<StartSubmissionReviewResponse> StartSubmissionReview(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<SetSubmissionNotesResponse> SetSubmissionNotes(Guid dataShareRequestId, string notes, CancellationToken cancellationToken);

    Task<GetCompletedSubmissionInformationResponse> GetCompletedReceivedRequest(Guid dataShareRequestId, CancellationToken cancellationToken);

    Task<AcceptSubmissionResponse> AcceptReceivedRequest(Guid dataShareRequestId, string acceptanceFeedbackForAcquirer, CancellationToken cancellationToken);

    Task<RejectSubmissionResponse> RejectReceivedRequest(Guid dataShareRequestId, string rejectionFeedbackForAcquirer, CancellationToken cancellationToken);
    
    Task<ReturnSubmissionResponse> ReturnReceivedRequest(Guid dataShareRequestId, string returnFeedbackForAcquirer, CancellationToken cancellationToken);

    Task<byte[]> DownloadCompletedRequest(Guid requestId, DataShareRequestFileFormat fileFormat, CancellationToken cancellationToken);

    Task<GetDataShareRequestAuditLogResponse> GetDataShareRequestReturnCommentsAuditLog(Guid dataShareRequestId, CancellationToken cancellationToken = default);
    #endregion
}