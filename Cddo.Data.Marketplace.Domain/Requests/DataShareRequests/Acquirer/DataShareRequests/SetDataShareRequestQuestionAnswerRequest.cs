using System.ComponentModel.DataAnnotations;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;

public class SetDataShareRequestQuestionAnswerRequest
{
    [Required]
    public DataShareRequestQuestionAnswer DataShareRequestQuestionAnswer { get; set; }
}