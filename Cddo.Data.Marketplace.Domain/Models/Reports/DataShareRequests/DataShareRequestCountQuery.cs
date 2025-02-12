using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Cddo.Data.Marketplace.Api.Dto.Models.Reports.DataShareRequests
{
    public class DataShareRequestCountQuery
    {
        public int Id { get; set; }

        public IEnumerable<DataShareRequestStatus> CurrentStatuses { get; set; } = [];

        public IEnumerable<DataShareRequestStatus> IntermediateStatuses { get; set; } = [];

        public bool UseOnlyTheMostRecentPeriodSpentInIntermediateStatuses { get; set; } = true;

        public TimeSpan? MinimumDuration { get; set; }

        public TimeSpan? MaximumDuration { get; set; }
       
        public DateTime? From { get; set; }
      
        public DateTime? To { get; set; }

        public int? PublisherOrganisationId { get; set; }

        public int? PublisherDomainId { get; set; }
    }
}
