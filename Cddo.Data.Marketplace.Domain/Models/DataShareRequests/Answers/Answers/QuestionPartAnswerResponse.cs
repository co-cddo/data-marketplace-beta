using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;

public class QuestionPartAnswerResponse
{
    public virtual QuestionPartResponseInputType InputType { get; set; }

    public int OrderWithinAnswerPart { get; set; }

    public QuestionPartAnswerResponseItemBase? ResponseItem { get; set; }

    public List<string> ValidationErrors { get; set; } = [];
}