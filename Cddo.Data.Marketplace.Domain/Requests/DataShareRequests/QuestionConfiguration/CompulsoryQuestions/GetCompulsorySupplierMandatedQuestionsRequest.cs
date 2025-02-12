using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.QuestionConfiguration.CompulsoryQuestions;

public class GetCompulsorySupplierMandatedQuestionsRequest
{
    [Required]
    public int RequestingUserId { get; set; }

    [Required]
    public int SupplierOrganisationId { get; set; }
}