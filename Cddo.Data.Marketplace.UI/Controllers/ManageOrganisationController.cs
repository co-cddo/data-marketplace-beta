using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("[controller]")]
[Authorize]
public class ManageOrganisationController : Controller
{
    private readonly IManageOrganisationService _manageUserService;
    private readonly IUserRoleService _userRoleService;

    public ManageOrganisationController(
        IManageOrganisationService manageUserService,
        IUserRoleService userRoleService)
    {
        _manageUserService = manageUserService;
        _userRoleService = userRoleService;
    }

    private sealed class SortOptions
    {
        public required SortBy SortBy { get; init; }
        public required SortDirection SortDirection { get; init; }
    }

    [HttpGet(Name = "GetManageOrganisations")]
    public async Task<IActionResult> GetManageOrganisations(ManageOrganisationsRequest manageOrganisationRequest, string? sortOption, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool? isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            if (isSystemAdmin.GetValueOrDefault())
            {
                var sortOptions = DetermineSortOptions(sortOption)
               ?? new SortOptions
               {
                   SortBy = SortBy.Modified,
                   SortDirection = SortDirection.Descending
               };

                manageOrganisationRequest.SortBy = sortOptions.SortBy;
                manageOrganisationRequest.SortDirection = sortOptions.SortDirection;

                var result = await _manageUserService
                    .GetManageOrganisationsAsync(manageOrganisationRequest, cancellationToken)
                    .ConfigureAwait(false);

                ViewBag.SearchTerm = manageOrganisationRequest.SearchTerm;
                ViewBag.OrganisationType = manageOrganisationRequest.OrganisationType;
                ViewBag.SortBy = sortOptions.SortBy;
                ViewBag.SortDirection = sortOptions.SortDirection;
                ViewBag.AllowListTrue = manageOrganisationRequest.AllowListTrue;
                ViewBag.AllowListFalse = manageOrganisationRequest.AllowListFalse;

                return View("~/Pages/Manage/Organisations.cshtml", result);
            }
        }

        return RedirectToPage("/Error/403");
    }

    [HttpGet("ManageOrganisation", Name = "ManageOrganisation")]
    public async Task<IActionResult> ManageOrganisation(int organisationId, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToPage("/Error/400");
        }

        if (User.Identity!.IsAuthenticated)
        {
            bool? isSystemAdmin = await _userRoleService.IsUserRoleSystemAdmin();
            if (isSystemAdmin.GetValueOrDefault())
            {
                return RedirectToPage("/Manage/ManageOrganisation", new { id = organisationId });
            }
        }

        return RedirectToPage("/Error/403");
    }

    private static SortOptions DetermineSortOptions(string? sortOption)
    {
        if (!string.IsNullOrWhiteSpace(sortOption))
        {
            return ExtractSortOptions(sortOption);
        }

        return new SortOptions
        {
            SortBy = SortBy.Modified,
            SortDirection = SortDirection.Descending
        };
    }

    private static SortOptions ExtractSortOptions(string sortOptionInput)
    {
        var options = sortOptionInput.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (options.Length != 2)
        {
            return new SortOptions
            {
                SortBy = SortBy.Modified,
                SortDirection = SortDirection.Descending
            };
        }

        var sortBy = Enum.TryParse<SortBy>(options[0], true, out var fieldValue)
            ? fieldValue
            : SortBy.Modified;

        var sortDirection = Enum.TryParse<SortDirection>(options[1], true, out var directionValue)
            ? directionValue
            : SortDirection.Descending;

        return new SortOptions
        {
            SortBy = sortBy,
            SortDirection = sortDirection
        };
    }
}
