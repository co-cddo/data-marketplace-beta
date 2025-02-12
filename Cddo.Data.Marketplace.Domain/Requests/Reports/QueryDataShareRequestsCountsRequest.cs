using Cddo.Data.Marketplace.Api.Dto.Models.Reports.DataShareRequests;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Reports
{
    public class QueryDataShareRequestsCountsRequest
    {
        public List<DataShareRequestCountQuery> DataShareRequestCountQueries { get; set; } = [];
    }
}
