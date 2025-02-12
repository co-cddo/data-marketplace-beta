using AutoFixture;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Logic.Test.ServiceOperationResults
{
    [TestFixture]
    public class ServiceOperationResultFactoryTests
    {
        private IFixture _fixture;
        private IServiceOperationResultFactory _resultFactory;

        
        public ServiceOperationResultFactoryTests()
        {
            _fixture = new Fixture();
            _resultFactory = new ServiceOperationResultFactory();
        }

        [Test]
        public void CreateSuccessfulDataResult_WhenDataIsValid_ReturnsSuccessfulDataResult()
        {
            // Arrange
            var data = _fixture.Create<string>();
            var statusCode = HttpStatusCode.OK;

            // Act
            var result = _resultFactory.CreateSuccessfulDataResult(data, statusCode);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Data, Is.EqualTo(data));
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public void CreateSuccessfulDataResult_WhenDataIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string data = null;
            var statusCode = HttpStatusCode.OK;

            // Act & Assert
            Assert.That(() => _resultFactory.CreateSuccessfulDataResult(data, statusCode), 
                        Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void CreateFailedDataResult_WhenErrorIsValid_ReturnsFailedDataResult()
        {
            // Arrange
            var error = _fixture.Create<string>();
            var statusCode = HttpStatusCode.BadRequest;

            // Act
            var result = _resultFactory.CreateFailedDataResult<string>(error, statusCode);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public void CreateFailedDataResult_WhenErrorIsNullOrWhiteSpace_ThrowsArgumentException()
        {
            // Arrange
            string error = null;
            var statusCode = HttpStatusCode.BadRequest;

            // Act & Assert
            Assert.That(() => _resultFactory.CreateFailedDataResult<string>(error, statusCode), 
                        Throws.InstanceOf<ArgumentException>());

            error = "";
            Assert.That(() => _resultFactory.CreateFailedDataResult<string>(error, statusCode), 
                        Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void CreateSuccessfulResult_WhenCalled_ReturnsSuccessfulResult()
        {
            // Arrange
            var statusCode = HttpStatusCode.OK;

            // Act
            var result = _resultFactory.CreateSuccessfulResult(statusCode);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.Null);
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public void CreateFailedResult_WhenErrorIsValid_ReturnsFailedResult()
        {
            // Arrange
            var error = _fixture.Create<string>();
            var statusCode = HttpStatusCode.InternalServerError;

            // Act
            var result = _resultFactory.CreateFailedResult(error, statusCode);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        }

        [Test]
        public void CreateFailedResult_WhenErrorIsNullOrWhiteSpace_ThrowsArgumentException()
        {
            // Arrange
            string error = null;
            var statusCode = HttpStatusCode.InternalServerError;

            // Act & Assert
            Assert.That(() => _resultFactory.CreateFailedResult(error, statusCode), 
                        Throws.InstanceOf<ArgumentException>());

            error = "";
            Assert.That(() => _resultFactory.CreateFailedResult(error, statusCode), 
                        Throws.InstanceOf<ArgumentException>());
        }

    }
}
