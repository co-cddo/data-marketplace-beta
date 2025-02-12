using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.QuestionConfiguration.CompulsoryQuestions;

public class SetCompulsoryQuestionRequest
{
    [Required]
    public int RequestingUserId { get; set; }

    [Required]
    public Guid QuestionId { get; set; }
}