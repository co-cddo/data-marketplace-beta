using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Api.Test
{
    public class CatalogDataStoreHealthCheckTests
    {
        private Mock<IOptions<HealthCheckSettings>> _mockOptions;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private CatalogDataStoreHealthCheck _healthCheck;
        private const string HealthCheckUrl = "https://fakeurl.com/health";

        [SetUp]
        public void SetUp()
        {
            // Arrange the mock for IOptions
            _mockOptions = new Mock<IOptions<HealthCheckSettings>>();
            _mockOptions.Setup(opt => opt.Value).Returns(new HealthCheckSettings
            {
                HealthCheckUrl = HealthCheckUrl
            });

            // Arrange the mock for HttpMessageHandler
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // Set up the HttpClient with the mocked HttpMessageHandler
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            // Create the health check instance
            _healthCheck = new CatalogDataStoreHealthCheck(_mockOptions.Object, _httpClient);
        }

        [Test]
        public async Task CheckHealthAsync_ReturnsHealthy_WhenServiceIsHealthy()
        {
            // Arrange: Mock the SendAsync to return a successful status code (200 OK)
            _mockHttpMessageHandler
                .SetupSendAsync(HttpStatusCode.OK);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

            // Assert: Ensure the result is healthy
            result.Status.Should().Be(HealthStatus.Healthy);
        }

        [Test]
        public async Task CheckHealthAsync_ReturnsUnhealthy_WhenServiceReturnsError()
        {
            // Arrange: Mock the SendAsync to return an error status code (500 InternalServerError)
            _mockHttpMessageHandler
                .SetupSendAsync(HttpStatusCode.InternalServerError);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

            // Assert: Ensure the result is unhealthy
            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        [Test]
        public async Task CheckHealthAsync_ReturnsUnhealthy_WhenServiceIsUnreachable()
        {
            // Arrange: Mock the SendAsync to throw an exception (simulate unreachable service)
            _mockHttpMessageHandler
                .SetupSendAsyncException(new HttpRequestException("Service unreachable"));

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

            // Assert: Ensure the result is unhealthy
            result.Status.Should().Be(HealthStatus.Unhealthy);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up resources if needed
        }
    }

    public static class HttpMessageHandlerExtensions
    {
        // Helper method to mock successful HTTP request responses
        public static void SetupSendAsync(this Mock<HttpMessageHandler> handler, HttpStatusCode statusCode)
        {
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(statusCode));
        }

        // Helper method to mock failed HTTP request (exception)
        public static void SetupSendAsyncException(this Mock<HttpMessageHandler> handler, Exception exception)
        {
            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(exception);
        }
    }
}
