using System.Net;

namespace Cddo.Data.Marketplace.Logic.ServiceOperationResults;

internal class ServiceOperationResult(bool success, string? error, HttpStatusCode? statusCode = null)
    : IServiceOperationResult
{
    public bool Success { get; } = success;

    public string? Error { get; } = error;

    public HttpStatusCode? StatusCode { get; } = statusCode;
}