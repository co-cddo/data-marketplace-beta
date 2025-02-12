using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.Web.Pages.Organisations
{
    public class OrganisationsModel : PageModel
    {
        public IUserRoleService UserRoleService { get; }

        public OrganisationsModel(IUserRoleService userRoleService)
        {
            UserRoleService = userRoleService;
        }
    }
}
