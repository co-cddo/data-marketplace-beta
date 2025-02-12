using System.ComponentModel;

namespace Agrimetrics.DataShare.Api.Dto.Models.Questions;

public enum QuestionStatus
{
    [Description("No response needed")]
    NoResponseNeeded,

    [Description("Not started")]
    NotStarted,

    [Description("Cannot start yet")]
    CannotStartYet,

    [Description("Completed")]
    Completed,

    [Description("Not applicable")]
    NotApplicable
}