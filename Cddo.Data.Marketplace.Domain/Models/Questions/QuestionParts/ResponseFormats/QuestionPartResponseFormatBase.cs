using System.Text.Json.Serialization;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

[JsonPolymorphic]
[JsonDerivedType(typeof(QuestionPartResponseFormatBase), typeDiscriminator: "Base")]
[JsonDerivedType(typeof(QuestionPartResponseFormatNoneReadOnly), typeDiscriminator: "NoneReadOnly")]
[JsonDerivedType(typeof(QuestionPartResponseFormatFreeFormText), typeDiscriminator: "FreeFormText")]
[JsonDerivedType(typeof(QuestionPartResponseFormatFreeFormNumeric), typeDiscriminator: "FreeFormNumeric")]
[JsonDerivedType(typeof(QuestionPartResponseFormatFreeFormDate), typeDiscriminator: "FreeFormDate")]
[JsonDerivedType(typeof(QuestionPartResponseFormatFreeFormTime), typeDiscriminator: "FreeFormTime")]
[JsonDerivedType(typeof(QuestionPartResponseFormatFreeFormDateTime), typeDiscriminator: "FreeFormDateTime")]
[JsonDerivedType(typeof(QuestionPartResponseFormatFreeFormCountry), typeDiscriminator: "FreeFormCountry")]
[JsonDerivedType(typeof(QuestionPartResponseFormatOptionSelectSingleValue), typeDiscriminator: "OptionSelectSingleValue")]
[JsonDerivedType(typeof(QuestionPartResponseFormatOptionSelectMultiValue), typeDiscriminator: "OptionSelectMultiValue")]
public class QuestionPartResponseFormatBase
{
    public virtual QuestionPartResponseInputType InputType { get; set; }

    public virtual QuestionPartResponseFormatType FormatType { get; set; }
}