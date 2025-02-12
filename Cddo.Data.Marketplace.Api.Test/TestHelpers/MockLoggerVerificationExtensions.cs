using Microsoft.Extensions.Logging;
using Moq;

namespace Cddo.Data.Marketplace.Api.Test.TestHelpers;

public static class MockLoggerVerificationExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel logLevel, string logMessage, Times? times = null)
    {
        mockLogger.Verify(x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == logMessage && @type.Name == "FormattedLogValues"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }

    public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel logLevel, string logMessage, Exception exception, Times? times = null)
    {
        mockLogger.Verify(x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == logMessage && @type.Name == "FormattedLogValues"),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times ?? Times.Once());
    }
}