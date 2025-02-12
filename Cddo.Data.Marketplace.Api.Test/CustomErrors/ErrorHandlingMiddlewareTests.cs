using Cddo.Data.Marketplace.Api.CustomErrors;
using Cddo.Data.Marketplace.Audit;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.Api.Test.CustomErrors
{
    [TestFixture]
    public class ErrorHandlingMiddlewareTests
    {
        private Mock<RequestDelegate> _mockNext;
        private Mock<IAppInsightsLogger> _mockAppInsightsLogger;
        private ErrorHandlingMiddleware _middleware;
        private DefaultHttpContext _httpContext;


        [SetUp]
        public void SetUp()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockAppInsightsLogger = new Mock<IAppInsightsLogger>();
            _middleware = new ErrorHandlingMiddleware(_mockNext.Object, _mockAppInsightsLogger.Object);
            _httpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task InvokeAsync_LogsError_WhenJsonExceptionIsThrown()
        {
            // Arrange: Set up the path that triggers the middleware error handling
            _httpContext.Request.Path = "/datamarketplaceapi/";

            // Arrange: Mock the next middleware to throw a JsonException
            _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(new JsonException("Invalid JSON"));

            // Act: Invoke the middleware
            await _middleware.InvokeAsync(_httpContext);

            // Assert: Ensure that AppInsightsLogger.LogEventMain is called with the expected parameters
            _mockAppInsightsLogger.Verify(
                logger => logger.LogEventMainBase(
                    EventTypes.ErrorEvent.ApplicationError,                // The event type is ErrorEvent
                    "Middleware",
                    "CDDO",
                    "Error",
                    "IngestionApiError",
                    "",                                    // Subject is empty in the test
                    It.IsAny<Dictionary<string, string>>()
                ),
                Times.Once
            );

            // Further assert that the ErrorDetails are included in the properties
            _mockAppInsightsLogger.Verify(
                logger => logger.LogEventMainBase(
                    EventTypes.ErrorEvent.ApplicationError,
                    "Middleware",
                    "CDDO",
                    "Error",
                    "IngestionApiError",
                    "",
                    It.Is<Dictionary<string, string>>(props => props.ContainsKey("ErrorDetails") && props["ErrorDetails"] == "Invalid JSON")
                ),
                Times.Once
            );
        }

        [Test]
        public async Task InvokeAsync_DoesNotLogError_WhenPathIsNotMatched()
        {
            // Arrange: Set up the path that does not trigger the middleware error handling
            _httpContext.Request.Path = "/some-other-path";

            // Act: Invoke the middleware, which should just call the next delegate without throwing
            await _middleware.InvokeAsync(_httpContext);

            // Assert: Ensure that AppInsightsLogger.LogEventMain is not called
            _mockAppInsightsLogger.Verify(
                logger => logger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
                Times.Never
            );
        }

        [Test]
        public async Task InvokeAsync_DoesNotLogError_WhenNoExceptionIsThrown()
        {
            // Arrange: Set up the path that triggers the middleware error handling
            _httpContext.Request.Path = "/datamarketplaceapi/some-path";

            // Arrange: Mock the next middleware to not throw any exception
            _mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            // Act: Invoke the middleware, which should not log anything since no exception is thrown
            await _middleware.InvokeAsync(_httpContext);

            // Assert: Ensure that AppInsightsLogger.LogEventMain is not called
            _mockAppInsightsLogger.Verify(
                logger => logger.LogEventMainBase(EventTypes.ErrorEvent.ApplicationError, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
                Times.Never
            );
        }
    }
}
