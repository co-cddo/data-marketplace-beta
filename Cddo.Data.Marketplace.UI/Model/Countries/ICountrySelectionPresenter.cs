namespace Cddo.Data.Marketplace.UI.Model.Countries;

internal interface ICountrySelectionPresenter
{
    IEnumerable<ICountrySelection> CountrySelectionsWithUnitedKingdom { get; }

    IEnumerable<ICountrySelection> CountrySelectionsWithoutUnitedKingdom { get; }
}