using System.ComponentModel;
using System.Runtime.Serialization;

namespace Cddo.Data.Marketplace.UI.Model.Enum
{
    public enum UpdateFrequency

    {
        [EnumMember(Value = "Never")]
        [Description("Never")]
        Never = 0,

        [EnumMember(Value = "Daily")]
        [Description("Daily")]
        Daily = 1,

        [EnumMember(Value = "Monthly")]
        [Description("Monthly")]
        Monthly = 2,

        [EnumMember(Value = "Quarterly")]
        [Description("Quarterly")]
        Quarterly = 3,

        [EnumMember(Value = "Annually")]
        [Description("Annually")]
        Annually = 4,

        [EnumMember(Value = "DontKnow")]
        [Description("I don’t know")]
        DontKnow = 5,

        [EnumMember(Value = "Other")]
        [Description("Other - I'll enter a different time period")]
        Other = 6,
    }
}
