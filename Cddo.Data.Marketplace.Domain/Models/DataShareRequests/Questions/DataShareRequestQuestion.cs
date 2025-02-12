namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;

public class DataShareRequestQuestion
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public Guid QuestionId { get; set; }

    public bool IsOptional { get; set; }

    public List<DataShareRequestQuestionPart> QuestionParts { get; set; }

    public DataShareRequestQuestionFooter? QuestionFooter { get; set; }
}