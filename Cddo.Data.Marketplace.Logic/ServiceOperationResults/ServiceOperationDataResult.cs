using System.Net;

namespace Cddo.Data.Marketplace.Logic.ServiceOperationResults;

public class ServiceOperationDataResult<T>(bool success, string? error, T? data, HttpStatusCode? statusCode = null)
    : IServiceOperationDataResult<T>
{
    public bool Success { get; } = success;

    public string? Error { get; } = error;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    public T? Data { get; } = data;
}