using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cddo.Data.Marketplace.UI.Pages.Error
{
    public class NoPermissionsModel : PageModel
    {
        public string RequiredPermission { get; set; }
        public string RequiredRolePermission { get; set; }
        public void OnGet(string requiredPermission)
        {
            switch (requiredPermission)
            {
                case "publisher":
                    RequiredPermission = "publish data descriptions";
                    RequiredRolePermission = "Metadata Publisher";
                    break;
                case "datarequest":
                    RequiredPermission = "approve data requests";
                    RequiredRolePermission = "Data Request Approver";
                    break;
                default:
                    RequiredPermission = "access this page";
                    RequiredRolePermission = "Organisation Administrator";
                    break;
            }
        }
    }
}
