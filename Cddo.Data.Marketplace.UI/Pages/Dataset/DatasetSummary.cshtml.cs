using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Dataset
{
    public class DatasetSummaryModel : PageModel
    {
        public Guid DatasetId { get; set; }

        public void OnGet(Guid datasetId)
        {
            this.DatasetId = datasetId;
        }
    }
}
