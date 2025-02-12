namespace Cddo.Data.Marketplace.UI.Model.Countries;

public class CountrySelection : ICountrySelection
{
    public required string Id { get; init; }

    public required string CountryName { get; init; }
}