namespace Agrimetrics.DataShare.Api.Dto.Models.Questions.QuestionParts.ResponseFormats;

public enum QuestionPartResponseFormatType
{
    Text,
    Numeric,
    Date,
    Time,
    DateTime,

    SelectSingle,
    SelectMulti,

    ReadOnly,

    Country
}