namespace Cddo.Data.Marketplace.UI.Configuration
{
    public class MockJwtDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MockJwtDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var mockToken = context.Session.GetString("MockJwtToken");
                if (!string.IsNullOrEmpty(mockToken))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mockToken);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
