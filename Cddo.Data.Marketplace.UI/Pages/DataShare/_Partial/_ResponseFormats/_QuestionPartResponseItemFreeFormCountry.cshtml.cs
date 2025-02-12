using Cddo.Data.Marketplace.UI.Model.Countries;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare._Partial._ResponseFormats
{
    public class QuestionPartResponseItemFreeFormCountry : QuestionPartResponseItemModel
    {
        public required string CountryInputComponentId { get; init; }

        public required string CountryInputComponentName { get; init; }

        public required string EnteredValue { get; init; }

        public required List<ICountrySelection> SelectableCountries { get; init; }
    }
}
