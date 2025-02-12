using System.Text.Json.Serialization;
using Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;

[JsonPolymorphic]
[JsonDerivedType(typeof(DataShareRequestQuestionAnswerPartResponseBase), typeDiscriminator: "Base")]
[JsonDerivedType(typeof(DataShareRequestQuestionAnswerPartResponseFreeForm), typeDiscriminator: "FreeForm")]
[JsonDerivedType(typeof(DataShareRequestQuestionAnswerPartResponseSelectionOption), typeDiscriminator: "SelectionOption")]
public class DataShareRequestQuestionAnswerPartResponseBase
{
    public virtual QuestionPartResponseInputType InputType { get; set; }

    public int OrderWithinAnswerPart { get; set; }

    public bool MultipleResponsesAreAllowed { get; set; }
}