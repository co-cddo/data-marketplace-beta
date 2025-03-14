using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;

namespace Cddo.Data.Marketplace.UI.Model;

public class DeleteDistribtionModel
{
    public string Title { get; set; }
    public string? Identifier { get; set; }
    public int DistributionId { get; set; }
    string? isCheckList { get; set; }
    string? isCheckAnswers { get; set; }
    string? isEditMode { get; set; }
}
