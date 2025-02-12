using System.Text.Json.Serialization;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.Answers;

[JsonPolymorphic]
[JsonDerivedType(typeof(QuestionPartAnswerResponseItemBase), typeDiscriminator: "Base")]
[JsonDerivedType(typeof(QuestionPartAnswerResponseItemFreeForm), typeDiscriminator: "FreeForm")]
[JsonDerivedType(typeof(QuestionPartAnswerResponseItemSelectionOption), typeDiscriminator: "SelectionOption")]
public class QuestionPartAnswerResponseItemBase
{
    public virtual QuestionPartResponseInputType InputType { get; set; }
}