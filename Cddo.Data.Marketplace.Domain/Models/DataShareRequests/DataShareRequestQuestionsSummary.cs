using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionSets;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests
{
    public class DataShareRequestQuestionsSummary
    {
        public Guid DataShareRequestId { get; set; }

        public string DataShareRequestRequestId { get; set; }

        public string EsdaName { get; set; }

        public QuestionSetSummary QuestionSetSummary { get; set; }

        public DataShareRequestStatus DataShareRequestStatus { get; set; }

        public bool QuestionsRemainThatRequireAResponse { get; set; }
    }
}
