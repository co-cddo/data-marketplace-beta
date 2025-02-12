using System.ComponentModel.DataAnnotations;

namespace Agrimetrics.DataShare.Api.Dto.Requests.Supplier;

public class GetSubmissionAsFileRequest
{
    [Required]
    public Guid DataShareRequestId { get; set; }

    [Required]
    public DataShareRequestFileFormat FileFormat { get; set; }
}