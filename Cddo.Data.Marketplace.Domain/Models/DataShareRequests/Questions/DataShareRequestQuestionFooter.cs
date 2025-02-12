namespace Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;

public class DataShareRequestQuestionFooter
{
    public string? FooterHeader { get; set; }

    public List<DataShareRequestQuestionFooterItem> FooterItems { get; set; }
}