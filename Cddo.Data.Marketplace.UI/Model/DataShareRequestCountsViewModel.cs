using Cddo.Data.Marketplace.Api.Dto.Models.Reports.DataShareRequests;

namespace Cddo.Data.Marketplace.UI.Model
{
    public class DataShareRequestCountsViewModel
    {
        public int DraftCurrentCount { get; set; }
        public int SubmittedCurrentCount { get; set; }
        public int AcceptedCurrentCount { get; set; }
        public int RejectedCurrentCount { get; set; }
        public int CancelledCurrentCount { get; set; }
        public int ReturnedCurrentCount { get; set; }
        public int DraftIntermediateCount { get; set; }
        public int SubmittedIntermediateCount { get; set; }
        public int AcceptedIntermediateCount { get; set; }
        public int RejectedIntermediateCount { get; set; }
        public int CancelledIntermediateCount { get; set; }
        public int ReturnedIntermediateCount { get; set; }
        public DataShareRequestCountQuery DataShareRequestCountQuery { get; set; }

    }
}
