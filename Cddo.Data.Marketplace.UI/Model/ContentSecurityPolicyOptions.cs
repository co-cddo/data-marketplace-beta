namespace Cddo.Data.Marketplace.UI.Model
{
    public class ContentSecurityPolicyOptions
    {
        public string[] DefaultSrc { get; set; }
        public string[] ScriptSrc { get; set; }
        public string[] ConnectSrc { get; set; }
        public string[] ImgSrc { get; set; }
        public string[] StyleSrc { get; set; }
        public string[] FontSrc { get; set; }
        public string[] ManifestSrc { get; set; }
    }

}
