using System.Text.Json.Serialization;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestAnswerSummaries;

[JsonPolymorphic]
[JsonDerivedType(typeof(DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemBase), typeDiscriminator: "Base")]
[JsonDerivedType(typeof(DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemFreeForm), typeDiscriminator: "FreeForm")]
[JsonDerivedType(typeof(DataShareRequestAnswersSummaryQuestionPartAnswerResponseOptionSelection), typeDiscriminator: "OptionSelection")]
public class DataShareRequestAnswersSummaryQuestionPartAnswerResponseItemBase
{
    public virtual QuestionPartResponseInputType InputType { get; set; }
}