using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;

namespace Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;

public class SubmissionInformation
{
    public Guid DataShareRequestId { get; set; }

    public string DataShareRequestRequestId { get; set; }

    public string EsdaName { get; set; }

    public string AcquirerOrganisationName { get; set; }

    public List<string> DataTypes { get; set; }

    public string ProjectAims { get; set; }

    public DateTime? WhenNeededBy { get; set; }

    public DateTime SubmittedOn { get; set; }

    public string AcquirerEmailAddress { get; set; }

    public DataShareRequestStatus RequestStatus { get; set; }

    public List<string> AnswerHighlights { get; set; }
}