using Agrimetrics.DataShare.Api.Dto.Models.Acquirer;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionSets;

public class QuestionSetSummary
{
    public Guid Id { get; set; }

    public bool AnswersSectionComplete { get; set; }

    public List<QuestionSetSectionSummary> SectionSummaries { get; set; }
    
    public DataShareRequestStatus DataShareRequestStatus { get; set; }

    public bool QuestionsRemainThatRequireAResponse { get; set; }

    public string? SupplierOrganisationName { get; set; }

    public string? SubmissionResponseFromSupplier { get; set; }

    public string? CancellationReasonsFromAcquirer { get; set; }

    public required AcquirerUserDetails AcquirerUserDetails { get; set; }
}