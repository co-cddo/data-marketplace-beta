using System.Text.Json.Serialization;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

[JsonPolymorphic]
[JsonDerivedType(typeof(SubmissionDetailsAnswerResponseItemBase), typeDiscriminator: "Base")]
[JsonDerivedType(typeof(SubmissionDetailsAnswerResponseItemFreeForm), typeDiscriminator: "FreeForm")]
[JsonDerivedType(typeof(SubmissionDetailsAnswerResponseItemOptionSelection), typeDiscriminator: "OptionSelection")]
public class SubmissionDetailsAnswerResponseItemBase
{

}