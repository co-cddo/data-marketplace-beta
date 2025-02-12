namespace Agrimetrics.DataShare.Api.Dto.Responses.QuestionConfiguration.CompulsoryQuestions;

public class SetCompulsorySupplierMandatedQuestionResponse
{
    public int RequestingUserId { get; set; }

    public int SupplierOrganisationId { get; set; }

    public Guid QuestionId { get; set; }
}