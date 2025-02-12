using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;

public class QuestionThemeRequest : CatalogDataRequestBase
{
    public List<ThemeEnum>? Theme { get; set; }
}
