using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.QuestionConfiguration.CompulsoryQuestions;

public class GetCompulsoryQuestionsRequest
{
    [Required]
    public int RequestingUserId { get; set; }
}