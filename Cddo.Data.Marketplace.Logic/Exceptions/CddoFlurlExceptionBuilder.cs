using Flurl.Http;

namespace Cddo.Data.Marketplace.Logic.Exceptions;

public class CddoFlurlExceptionBuilder : ICddoFlurlExceptionBuilder
{
    async Task<CddoFlurlException> ICddoFlurlExceptionBuilder.BuildAsync(
        FlurlHttpException ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        return new CddoFlurlException
        {
            StatusCode = ex.StatusCode,
            FlurlResponseText = await ex.GetResponseStringAsync(),
            ExceptionText = ex.Message
        };
    }
}