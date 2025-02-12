namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats
{
    public class QuestionPartResponseItemFreeFormDate : QuestionPartResponseItemModel
    {
        public required string DayInputComponentId { get; init; }
        
        public required string MonthInputComponentId { get; init; }
        
        public required string YearInputComponentId { get; init; }

        public required string DayInputComponentName { get; init; }

        public required string MonthInputComponentName { get; init; }

        public required string YearInputComponentName { get; init; }

        public required string EnteredDayPart { get; init; }

        public required string EnteredMonthPart { get; init; }

        public required string EnteredYearPart { get; init; }
    }
}
