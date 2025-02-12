namespace Cddo.Data.Marketplace.UI.Configuration;

public static class WebApplicationRegistrations
{
    public static void UseSecurityHeaders(this WebApplication app)
    {
        var policyCollection = new HeaderPolicyCollection()
                    .AddFrameOptionsDeny()
                    .AddXssProtectionBlock()
                    .AddContentTypeOptionsNoSniff()
                    .AddStrictTransportSecurityMaxAgeIncludeSubDomains(
                        maxAgeInSeconds: 60 * 60 * 24 * 365
                    )
                    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
                    .RemoveServerHeader()
                    .AddCrossOriginOpenerPolicy(builder =>
                    {
                        builder.SameOrigin();
                    })
                    .AddCrossOriginEmbedderPolicy(builder =>
                    {
                        builder.RequireCorp();
                    })
                    .AddCrossOriginResourcePolicy(builder =>
                    {
                        builder.SameOrigin();
                    })
                    .AddContentSecurityPolicy(cspBuilder =>
                    {
                        cspBuilder.AddUpgradeInsecureRequests();
                        cspBuilder.AddBlockAllMixedContent();
                        cspBuilder.AddFontSrc().Self();
                        cspBuilder.AddObjectSrc().None();
                        cspBuilder.AddImgSrc().Self().OverHttps();
                        cspBuilder.AddScriptSrc().UnsafeInline();
                        cspBuilder.AddScriptSrcElem().UnsafeInline().Self();
                        cspBuilder.AddStyleSrc().Self().WithNonce();
                        cspBuilder.AddMediaSrc().Self().OverHttps();
                        cspBuilder.AddFrameAncestors().None();
                        cspBuilder.AddBaseUri().Self();
                        cspBuilder.AddFrameSrc().Self();
                        cspBuilder.AddManifestSrc().Self();
                        cspBuilder.AddMediaSrc().Self();
                        cspBuilder.AddConnectSrc().Self();
                        cspBuilder.AddDefaultSrc().None();
                    });

        app.UseSecurityHeaders(policyCollection);
    }
}
