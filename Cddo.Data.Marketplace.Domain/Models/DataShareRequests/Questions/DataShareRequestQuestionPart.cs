using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;

public class DataShareRequestQuestionPart
{
    public QuestionPart QuestionPartQuestion { get; set; }

    public QuestionPartAnswer? QuestionPartAnswer { get; set; }
}