using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Reports
{
    public class MetadataReportsStats
    {
        public string OrganisationName { get; set; }
        public Guid TemplateId { get; set; }
        public int TotalPublished { get; set; }
        public int TotalEdited { get; set; }
        public int TotalDraft { get; set; }
        public int TotalArchived { get; set; }
        public int Totaldeleted { get; set; }
        public int TotalUploadloaded { get; set; }
        public int TotalWebformIngested { get; set; }
    }
}
