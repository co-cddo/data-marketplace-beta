using Agrimetrics.DataShare.Api.Dto.Models.Acquirer.DataShareRequests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.DataShare
{
    public class DataShareRequestPreviousModel : PageModel
    {
        public required Guid EsdaId { get; init; }

        public required string EsdaName { get; init; }

        public required DataShareRequestRaisedForEsdaByAcquirerOrganisationSummarySet DataShareRequestSummaries { get; init; }


    }
}
