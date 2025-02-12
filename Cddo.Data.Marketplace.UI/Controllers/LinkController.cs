using Cddo.Data.Marketplace.Audit;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.UI.Controllers;

[Route("")]
public class LinkController : Controller
{
    public LinkController()
    {
    }

    [Route("accessibility-statement")]
    public IActionResult Accessibility()
    {
        ViewData["Title"] = "Accessibility";
        return View("~/Pages/About/Accessibility.cshtml");
    }

    [Route("cookies")]
    public IActionResult Cookies()
    {
        ViewData["Title"] = "Cookies";
        return View("~/Pages/About/Cookies.cshtml");
    }

    [Route("cookie-details")]
    public IActionResult CookieDetailsPage()
    {
        return View("~/Pages/About/CookieDetails.cshtml");
    }

    [Route("privacy")]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy";
        return View("~/Pages/About/Privacy.cshtml");
    }
}
