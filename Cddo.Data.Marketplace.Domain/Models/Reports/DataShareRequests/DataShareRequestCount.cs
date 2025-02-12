namespace Cddo.Data.Marketplace.Api.Dto.Models.Reports.DataShareRequests
{
    public class DataShareRequestCount
    {
        public required DataShareRequestCountQuery DataShareRequestCountQuery { get; set; }
        public required int NumberOfDataShareRequests { get; set; }
    }
}
