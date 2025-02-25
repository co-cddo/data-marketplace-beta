namespace Cddo.Data.Marketplace.UI.Model
{
    public class ContentSecurityPolicyOptions
    {
        public string[] DefaultSrc { get; set; } = Array.Empty<string>();
        public string[] ScriptSrc { get; set; } = Array.Empty<string>();
        public string[] ConnectSrc { get; set; } = Array.Empty<string>();
        public string[] ImgSrc { get; set; } = Array.Empty<string>();
        public string[] StyleSrc { get; set; } = Array.Empty<string>();
        public string[] FontSrc { get; set; } = Array.Empty<string>();
        public string[] ManifestSrc { get; set; } = Array.Empty<string>();
    }
}
