using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Cddo.Data.Marketplace.Api.Dto;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.Logic.Services.Users;

namespace Cddo.Data.Marketplace.UI.Pages.Manage
{
    public class ManageDomainModel(
        IManageOrganisationsService manageOrganisationsService,
        IDataShareRequestMailboxAddressValidation dataShareRequestMailboxAddressValidation)
        : PageModel
    {
        public OrganisationDetail OrganisationDetail { get; set; } = null!;
        public DomainDetail DomainDetail { get; set; } = null!;

        private static readonly string AccessDeniedPage = "/Error/403";

        public async Task<IActionResult> OnGetAsync(int? organisationId, int? domainId)
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage(AccessDeniedPage);

            if (!organisationId.HasValue) return RedirectToPage("/Error/400");
            if (!domainId.HasValue) return RedirectToPage("/Error/400");

            try
            {
                await LoadDomainDetails(organisationId.Value, domainId.Value);

                return Page();
            }
            catch
            {
                return RedirectToPage("/Error/400");
            }
        }

        public async Task<IActionResult> OnPostEnableDataShareRequests(
            int organisationId,
            int domainId,
            string enableDataShareRequestMailboxAddress)
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage(AccessDeniedPage);

            return await SetDataShareRequestMailboxAddressAsync(
                organisationId, domainId, enableDataShareRequestMailboxAddress, true, nameof(enableDataShareRequestMailboxAddress));
        }

        public async Task<IActionResult> OnPostDisableDataShareRequests(
            int organisationId,
            int domainId)
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage(AccessDeniedPage);

            return await SetDataShareRequestMailboxAddressAsync(
                organisationId, domainId, null, false);
        }

        public async Task<IActionResult> OnPostUpdateDataShareRequestMailboxAddress(
            int organisationId,
            int domainId,
            string updatedDataShareRequestMailboxAddress)
        {
            if (!User.Identity!.IsAuthenticated) return RedirectToPage(AccessDeniedPage);

            return await SetDataShareRequestMailboxAddressAsync(
                organisationId, domainId, updatedDataShareRequestMailboxAddress, true, nameof(updatedDataShareRequestMailboxAddress));
        }

        private async Task<IActionResult> SetDataShareRequestMailboxAddressAsync(
            int organisationId,
            int domainId,
            string? dataShareRequestMailboxAddress,
            bool validateAddress,
            string? inputControlName = null)
        {
            ViewData.ModelState.Clear();

            if (validateAddress && !dataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
        dataShareRequestMailboxAddress!, out var validationError))
            {
                ViewData.ModelState.AddModelError(inputControlName!, validationError!);
                await LoadDomainDetails(organisationId, domainId);
                return Page();
            }

            await manageOrganisationsService.UpdateDataShareRequestMailboxAddress(domainId, dataShareRequestMailboxAddress);

            await LoadDomainDetails(organisationId, domainId);
            return Page();
        }

        public async Task<IActionResult> OnPostReturnToOrganisationDetails(int organisationId)
        {
            return await Task.Run(() => ReturnToOrganisationDetailsAsync(organisationId));
        }

        private IActionResult ReturnToOrganisationDetailsAsync(int organisationId)
        {
            var routeValues = new
            {
                id = organisationId
            };

            return RedirectToPage("/Manage/ManageOrganisation", routeValues);
        }

        private async Task LoadDomainDetails(int organisationId, int domainId)
        {
            var organisationDetail = await manageOrganisationsService.GetOrganisationAsync(organisationId)
                                     ?? throw new InvalidOperationException($"Unable to load domain details for unknown organisation Id.  Organisation Id: {organisationId}");

            var domainDetail = organisationDetail.Domains?.SingleOrDefault(x => x.DomainId == domainId)
                               ?? throw new InvalidOperationException($"Unable to load domain details for unknown domain Id.  Organisation Id: {organisationId}, Domain Id: {domainId}");

            OrganisationDetail = organisationDetail;
            DomainDetail = domainDetail;
        }
    }
}
