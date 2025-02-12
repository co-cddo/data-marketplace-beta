using Agrimetrics.DataShare.Api.Dto.Models.QuestionConfiguration.CompulsoryQuestions;

namespace Agrimetrics.DataShare.Api.Dto.Responses.QuestionConfiguration.CompulsoryQuestions;

public class GetCompulsorySupplierMandatedQuestionsResponse
{
    public int RequestingUserId { get; set; }

    public int SupplierOrganisationId { get; set; }

    public CompulsorySupplierMandatedQuestionSet CompulsorySupplierMandatedQuestionSet { get; set; }
}