using System.Net;

namespace Cddo.Data.Marketplace.Logic.ServiceOperationResults;

public class ServiceOperationResultFactory : IServiceOperationResultFactory
{
    IServiceOperationDataResult<T> IServiceOperationResultFactory.CreateSuccessfulDataResult<T>(
        T data,
        HttpStatusCode? statusCode)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new ServiceOperationDataResult<T>(true, null, data, statusCode);
    }

    IServiceOperationDataResult<T> IServiceOperationResultFactory.CreateFailedDataResult<T>(
        string error,
        HttpStatusCode? statusCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new ServiceOperationDataResult<T>(false, error, default, statusCode);
    }

    IServiceOperationResult IServiceOperationResultFactory.CreateSuccessfulResult(
        HttpStatusCode? statusCode)
    {
        return new ServiceOperationResult(true, null, statusCode);
    }

    IServiceOperationResult IServiceOperationResultFactory.CreateFailedResult(
        string error,
        HttpStatusCode? statusCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new ServiceOperationResult(false, error, statusCode);
    }
}