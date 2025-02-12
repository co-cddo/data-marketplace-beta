using Cddo.Data.Marketplace.Api.Dto.Requests;
using Cddo.Data.Marketplace.Logic.Services;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers
{
    public class ManageDepartmentController : Controller
    {
        private readonly IManageDepartmentsService _manageDepartmentService;
        private readonly IUserRoleService _userRoleService;
        private readonly string errorPageLink = "/Error/403";


        public ManageDepartmentController(
            IManageDepartmentsService manageDepartmentService,
            IUserRoleService userRoleService)
        {
            ArgumentNullException.ThrowIfNull(manageDepartmentService);

            _manageDepartmentService = manageDepartmentService;
            _userRoleService = userRoleService;
        }



        [HttpGet(Name = "GetDepartments")]
        public async Task<IActionResult> GetDepartments(ManageDepartmentRequest manageDepartmentRequest)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin)
                {
                    var result = await _manageDepartmentService.GetManageDepartmentsAsync(manageDepartmentRequest, default).ConfigureAwait(false);
                    return View("~/Pages/Manage/Departments.cshtml", result);
                }
            }
            return RedirectToPage(errorPageLink);
        }

        [HttpGet(Name = "GetDepartmentById")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin)
                {
                    var result = await _manageDepartmentService.GetDepartmentByIdAsync(id, default).ConfigureAwait(false);
                    return View("~/Pages/Manage/ManageDepartment.cshtml", result);
                }
            }
            return RedirectToPage(errorPageLink);
        }

        [HttpGet(Name = "GetAddDepartments")]
        public async Task<IActionResult> GetAddDepartments()
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin)
                {
                    return View("~/Pages/Manage/AddDepartment.cshtml");
                }
            }
            return RedirectToPage(errorPageLink);
        }

        [HttpPost(Name = "PostAddDepartment")]
        public async Task<IActionResult> PostAddDepartments(string departmentName)
        {
            if (User.Identity!.IsAuthenticated)
            {
                bool isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
                if (isSystemAdmin)
                {
                    var result = await _manageDepartmentService.PostAddDepartmentAsync(departmentName, default).ConfigureAwait(false);
                    if (result)
                    {
                        return RedirectToAction("GetDepartments", new ManageDepartmentRequest());
                    }
                }
            }
            return RedirectToPage(errorPageLink);
        }

    }
}
