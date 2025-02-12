using System.Net;

namespace Cddo.Data.Marketplace.Logic.ServiceOperationResults;

public interface IServiceOperationDataResult<out T>
{
    public bool Success { get; }

    public string? Error { get; }

    public HttpStatusCode? StatusCode { get; }

    T? Data { get; }
}