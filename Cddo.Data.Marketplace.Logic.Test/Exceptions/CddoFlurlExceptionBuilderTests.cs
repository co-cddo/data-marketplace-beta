using AutoFixture;
using Cddo.Data.Marketplace.Logic.Exceptions;
using FluentAssertions;
using Flurl.Http;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Logic.Test.Exceptions
{
    [TestFixture]
    public class CddoFlurlExceptionBuilderTests
    {
        private IFixture _fixture;
        private ICddoFlurlExceptionBuilder _exceptionBuilder;

        
        public CddoFlurlExceptionBuilderTests()
        {
            _fixture = new Fixture();
            _exceptionBuilder = new CddoFlurlExceptionBuilder();
        }

        [Test]
        public async Task BuildAsync_WhenFlurlHttpExceptionIsValid_ReturnsCddoFlurlException()
        {
            // Arrange
            var exceptionMessage = _fixture.Create<string>();
            var flurlCall = new FlurlCall();
            var innerException = new Exception();
            var exception = new FlurlHttpException(flurlCall, exceptionMessage, innerException) { };

            // Act
            var result = await _exceptionBuilder.BuildAsync(exception);
            var toString = result.ToString();
            var message = result.Message;
            // Assert
            result.Should().NotBeNull();
            result.ExceptionText.Should().Be(exceptionMessage);
        }

        [Test]
        public void BuildAsync_WhenFlurlHttpExceptionIsNull_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _exceptionBuilder.BuildAsync(null));
        }


    }
}
