using Flurl.Http;

namespace Cddo.Data.Marketplace.Logic.Exceptions;

public interface ICddoFlurlExceptionBuilder
{
    Task<CddoFlurlException> BuildAsync(FlurlHttpException ex);
}