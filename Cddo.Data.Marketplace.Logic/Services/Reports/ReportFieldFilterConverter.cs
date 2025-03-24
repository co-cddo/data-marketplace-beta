using Agm.Catalog.DotNet.Core.Utilities;
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;

namespace Cddo.Data.Marketplace.Logic.Services.Reports;

internal class ReportFieldFilterConverter(
    ICatalogAssetFieldInformationDataDescription catalogAssetFieldInformationDataDescription,
    IEnumMemberConverter enumMemberConverter) : IReportFieldFilterConverter
{
    ICatalogAssetFieldFilter IReportFieldFilterConverter.ConvertReportFieldFilter(
        CatalogReportFieldFilter catalogReportFieldFilter)
    {
        ArgumentNullException.ThrowIfNull(catalogReportFieldFilter);

        var fieldTypeInformation = catalogAssetFieldInformationDataDescription.GetCatalogReportFieldTypeInformation(
            catalogReportFieldFilter.Field);

        return new CatalogAssetFieldFilter
        {
            Field = catalogReportFieldFilter.Field,
            PropertyContainsCollection = fieldTypeInformation.IsCollection,
            MatchableValues = ProvisionFilterValues()
        };

        IEnumerable<string> ProvisionFilterValues()
        {
            Func<string, string> provisionFunc = fieldTypeInformation.FieldType switch
            {
                CatalogAssetFieldType.Enumeration => ProvisionEnumValue,
                _ => x => x
            };

            var sanitizedValues = catalogReportFieldFilter.Values.Where(x => !string.IsNullOrWhiteSpace(x));
            return sanitizedValues.Select(provisionFunc).Select(EscapeValue);

            string ProvisionEnumValue(string value)
            {
                // If the property is an enum then the given value will be the enum value name (e.g. "CrimeAndJusticeEnum"), and the
                // filter value needs to be the enum member value (e.g. "Crime and justice")

                //var enumValue = (Enum)Enum.Parse(fieldTypeInformation.ItemDataType, value, true);

                return value;
                //return enumMemberConverter.GetEnumMemberValue(enumValue);
            }

            string EscapeValue(string value) =>
                value.Replace(":", "\\:");
        }
    }
}