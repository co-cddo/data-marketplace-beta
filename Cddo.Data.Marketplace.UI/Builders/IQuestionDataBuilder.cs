using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;
using Cddo.Data.Marketplace.UI.Pages.DataShare;

namespace Cddo.Data.Marketplace.UI.Builders
{
    public interface IQuestionDataBuilder
    {
        QuestionModel BuildQuestionModelFromDataShareRequestQuestion(
            DataShareRequestQuestion dataShareRequestQuestion);

        DataShareRequestQuestionAnswer BuildQuestionAnswerFromFormData(
            IFormCollection form);
    }
}
