namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions;
public class QuestionIssuedRequest : CatalogDataRequestBase
{
    public int metadataIssuedDay { get; set; }
    public int metadataIssuedMonth { get; set; }
    public int metadataIssuedYear { get; set; }
    public DateTime metadataIssuedDate { get; set; }
}
public class QuestionIssuedRequestModel
{
    public string? Identifier { get; set; }
    public string metadataIssuedDay { get; set; }
    public string metadataIssuedMonth { get; set; }
    public string metadataIssuedYear { get; set; }

}
