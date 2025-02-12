using System.Net;

namespace Cddo.Data.Marketplace.Logic.ServiceOperationResults;

public interface IServiceOperationResult
{
    public bool Success { get; }

    public string? Error { get; }

    public HttpStatusCode? StatusCode { get; }
}