using Cddo.Data.Marketplace.Api.Dto.Requests.RequestAccess;
using Cddo.Data.Marketplace.UI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Authorize]
[Route("OrganisationAccess")]
public class OrganisationAccessController : Controller
{
    private readonly IRequestAccessService _requestAccessService;
    public OrganisationAccessController(
            IRequestAccessService requestAccessService)
    {

        _requestAccessService = requestAccessService;
    }

    public const string ViewAddOrganisation = "~/Pages/OrganisationAccess/AddOrganisation.cshtml";

    [Route("Organisation-Access-Request")]
    public IActionResult RequestOrganisationAccess()
    {
        return View(ViewAddOrganisation);
    }

    [Route("SubmitRequest")]
    public async Task<IActionResult> SubmitOrganisationRequest(CreateOrganisationRequest organisationAccessRequest)
    {
        if (!ModelState.IsValid)
        {
            return View(ViewAddOrganisation, organisationAccessRequest);
        }

        if (!IsValidEmail(organisationAccessRequest.CreatedBy))
        {
            ModelState.AddModelError("CreatedBy", "Invalid email format");
            return View(ViewAddOrganisation, organisationAccessRequest);
        }

        var response = await _requestAccessService.SubmitOrganisationRequestAsync(organisationAccessRequest);
        if (response == null)
        {
            ModelState.AddModelError(string.Empty, "Submission failed: An organisation or domain with the same name already exists");
            return View(ViewAddOrganisation, organisationAccessRequest);
        }

        return RedirectToAction(nameof(SubmitOrganisationConfirmation));
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var emailValidator = new EmailAddressAttribute();
        return emailValidator.IsValid(email);
    }

    [Route("SubmitRequestConfirmation")]
    public IActionResult SubmitOrganisationConfirmation()
    {
        return View("~/Pages/OrganisationAccess/OrganisationConfirmation.cshtml");
    }
}
