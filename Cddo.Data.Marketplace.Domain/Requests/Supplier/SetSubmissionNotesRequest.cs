using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Supplier;

public class SetSubmissionNotesRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }

    
    public string Notes { get; set; }
}