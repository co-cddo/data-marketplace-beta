using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;

public class QuestionDistributionRequest : CatalogDataRequestBase
{
    public List<Distribution>? Distribution { get; set; }
}