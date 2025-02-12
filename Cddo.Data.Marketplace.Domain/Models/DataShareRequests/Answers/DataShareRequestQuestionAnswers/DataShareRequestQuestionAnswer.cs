namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers
{
    public class DataShareRequestQuestionAnswer
    {
        public Guid DataShareRequestId { get; set; }

        public Guid QuestionId { get; set; }

        public List<DataShareRequestQuestionAnswerPart> AnswerParts { get; set; }
    }
}