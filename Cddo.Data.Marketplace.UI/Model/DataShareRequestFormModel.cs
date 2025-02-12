namespace Cddo.Data.Marketplace.UI.Model;

public class DataShareRequestFormModel(Guid requestId, Guid questionId)
{
    public Guid RequestId { get; set; } = requestId;

    public Guid QuestionId { get; set; } = questionId;

    public List<DataShareRequestFormDataModel> FormData { get; set; } = [];
}

public class DataShareRequestFormDataModel
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
