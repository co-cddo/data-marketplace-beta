using Microsoft.AspNetCore.Mvc;


namespace Cddo.Data.Marketplace.UI.Controllers
{
    public class CookieController : Controller
    {
        private const string analyticsCookieName = "cddo-analytics-cookies-acceptance";
        private const string siteSettingsCookieName = "cddo-sitesettings-cookies-acceptance";
        private const string tempCookiesAcceptanceDecisionName = "CookieAcceptanceDecision";

        private readonly ILogger<CookieController> _logger;

        public CookieController(ILogger<CookieController> logger)
        {
            _logger = logger;
        }

        public IActionResult ApplyCookiesAcceptanceDecisionsFromCookiesPage(
            bool? acceptAnalyticsCookiesDecision,
            bool? acceptSiteSettingsCookiesDecision)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for ApplyCookiesAcceptanceDecisionsFromCookiesPage.");
            }

            _logger.LogInformation("Applying cookies acceptance decisions from cookies page.");
            ApplyAnalyticsCookiesDecision(acceptAnalyticsCookiesDecision);
            ApplySiteSettingsCookiesDecision(acceptSiteSettingsCookiesDecision);

            return Redirect("/");
        }

        public IActionResult ApplyCookiesAcceptanceDecisionFromBanner(
            bool acceptAllCookiesDecision)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for ApplyCookiesAcceptanceDecisionFromBanner.");
            }

            _logger.LogInformation("Applying cookies acceptance decision from banner.");
            ApplyAnalyticsCookiesDecision(acceptAllCookiesDecision);
            ApplySiteSettingsCookiesDecision(acceptAllCookiesDecision);

            TempData[tempCookiesAcceptanceDecisionName] = acceptAllCookiesDecision;

            return ReloadCallingPage();
        }

        public IActionResult HideCookieBanner()
        {
            TempData.Remove(tempCookiesAcceptanceDecisionName);

            _logger.LogInformation("Hiding the cookie banner.");

            return ReloadCallingPage();
        }

        private void ApplyAnalyticsCookiesDecision(
            bool? acceptAnalyticsCookiesDecision)
        {
            _logger.LogInformation("Applying analytics cookies decision: {acceptAnalyticsCookiesDecision}", acceptAnalyticsCookiesDecision);
            ApplyCookiesDecision(analyticsCookieName, acceptAnalyticsCookiesDecision);
        }

        private void ApplySiteSettingsCookiesDecision(
            bool? acceptSiteSettingsCookiesDecision)
        {
            _logger.LogInformation("Applying site settings cookies decision: {acceptSiteSettingsCookiesDecision}", acceptSiteSettingsCookiesDecision);
            ApplyCookiesDecision(siteSettingsCookieName, acceptSiteSettingsCookiesDecision);
        }

        private void ApplyCookiesDecision(
            string cookieName,
            bool? acceptCookiesDecision)
        {
            _logger.LogInformation("Applying cookies decision for cookie {cookieName}. Accept: {acceptCookiesDecision}", cookieName, acceptCookiesDecision);

            Response.Cookies.Delete(cookieName);

            if (acceptCookiesDecision.HasValue)
            {
                Response.Cookies.Append(cookieName, acceptCookiesDecision.Value.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMonths(1),
                    Path = "/"
                });
            }
        }

        private IActionResult ReloadCallingPage()
        {
            var referrerUrl = Request.Headers.Referer.ToString();

            _logger.LogInformation("Reloading calling page. Referrer URL: {referrerUrl}", referrerUrl);

            return !string.IsNullOrEmpty(referrerUrl)
                ? Redirect(referrerUrl)
                : Redirect("/");
        }
    }
}
