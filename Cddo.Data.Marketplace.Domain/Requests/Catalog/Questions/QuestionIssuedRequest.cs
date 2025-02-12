namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
public class QuestionIssuedRequest : CatalogDataRequestBase
{
    public int metadataIssuedDay { get; set; }
    public int metadataIssuedMonth { get; set; }
    public int metadataIssuedYear { get; set; }
    public DateTime metadataIssuedDate { get; set; }
}
