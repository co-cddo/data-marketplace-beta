using System.Net;

namespace Cddo.Data.Marketplace.Logic.ServiceOperationResults;

public interface IServiceOperationResultFactory
{
    IServiceOperationDataResult<T> CreateSuccessfulDataResult<T>(
        T data,
        HttpStatusCode? statusCode = null);

    IServiceOperationDataResult<T> CreateFailedDataResult<T>(
        string error,
        HttpStatusCode? statusCode = null);

    IServiceOperationResult CreateSuccessfulResult(
        HttpStatusCode? statusCode = null);

    IServiceOperationResult CreateFailedResult(
        string error,
        HttpStatusCode? statusCode = null);
}