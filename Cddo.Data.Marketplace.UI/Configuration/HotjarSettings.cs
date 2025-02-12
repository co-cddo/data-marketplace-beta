using System.Security.Policy;

namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class HotjarSettings
    {
        public string GACookieDomain { get; set; }
        public string HotjarId { get; set; }
        public bool HotjarEnabled { get; set; }
        public bool GAEnabled { get; set; }
        public string GAEnvironment { get; set; }

        public string SiteUrl { get; set; }
    }
}