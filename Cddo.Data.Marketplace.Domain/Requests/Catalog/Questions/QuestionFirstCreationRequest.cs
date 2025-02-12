using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;

public class QuestionFirstCreationRequest : QuestionTitleRequest
{
    public string? Publisher { get; set; }
    public SecurityClassificationEnum? SecurityClassification { get; set; }
}