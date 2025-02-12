using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.ManagementData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V1_0;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets;
using Agm.Catalog.DotNet.Dto.Responses.DataAssets.Models;
using Agm.Catalog.DotNet.Logic.Services.DataAssets;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Exceptions;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;
using Agm.Catalog.DotNet.Logic.Services.EmbeddedResourceProvision;
using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Api.Controllers;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cddo.Data.Marketplace.Api.Test.Controllers
{
    [TestFixture]
    public class DataAssetControllerTests
    {
        protected readonly IFixture _fixture;

        private Mock<ILogger<DataAssetController>> _mockLogger = new();
        private Mock<IDataAssetService> _mockDataAssetService = new();
        private Mock<IDataAssetResponseFactory> _mockDataAssetResponseFactory = new();
        private Mock<IAppInsightsLogger> _mockAppInsightsLogger = new();
        private Mock<IUserProfilePresenter> _mockUserRoleService = new();
        private Mock<IUserProfilePresenter> _mockUserProfilePresenter = new();
        private Mock<IServiceOperationResultFactory> _mockServiceOperationResultFactory = new();

        private readonly DataAssetController _controller;

        public DataAssetControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _mockLogger = new Mock<ILogger<DataAssetController>>();
            _mockDataAssetService = new Mock<IDataAssetService>();
            _mockDataAssetResponseFactory = new Mock<IDataAssetResponseFactory>();
            _mockAppInsightsLogger = new Mock<IAppInsightsLogger>();
            _mockUserRoleService = new Mock<IUserProfilePresenter>();
            _mockUserProfilePresenter = new Mock<IUserProfilePresenter>();

            _controller = new DataAssetController(
                _mockLogger.Object,
                _mockDataAssetService.Object,
                _mockDataAssetResponseFactory.Object,
                _mockAppInsightsLogger.Object,
                _mockUserRoleService.Object,
                _mockUserProfilePresenter.Object);

            _mockServiceOperationResultFactory = new Mock<IServiceOperationResultFactory>();

        }

        public void clearInvocations()
        {
            _mockLogger.Invocations.Clear();
            _mockDataAssetService.Invocations.Clear();
            _mockDataAssetResponseFactory.Invocations.Clear();
            _mockAppInsightsLogger.Invocations.Clear();
            _mockUserRoleService.Invocations.Clear();
            _mockUserProfilePresenter.Invocations.Clear();
        }

        [Test]
        public async Task AddProfiledDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<AddProfiledDataAssetRequest>();

            var data = _fixture.Create<IAddDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.AddProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(), 
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ReturnsAsync(mockResult.Object);

            var expectedResponse = _fixture.Create<AddProfiledDataAssetResponse>();

            _mockDataAssetResponseFactory
                .Setup(x => x.CreateAddProfiledDataAssetResponse(It.IsAny<Guid>()))
                .Returns(expectedResponse);

            // Act
            var result = await _controller.AddProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expectedResponse);

            _mockDataAssetService.Verify(x => x.AddProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(), It.IsAny<ManagementMetadataBase>(), 
                                                                            It.IsAny<DataAssetActionSourceEnum>()), Times.Once);

            _mockDataAssetResponseFactory.Verify(x => x.CreateAddProfiledDataAssetResponse(It.IsAny<Guid>()), Times.Once);
        }
        [Test]
        public async Task AddProfiledDataAsset_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<AddProfiledDataAssetRequest>();

            var data = _fixture.Create<IAddDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IAddDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.AddProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.AddProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            _mockDataAssetService.Verify(x => x.AddProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(), It.IsAny<ManagementMetadataBase>(),
                                                                            It.IsAny<DataAssetActionSourceEnum>()), Times.Once);
        }
        [Test]
        public async Task AddProfiledDataAsset_ShouldReturnBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<AddProfiledDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.AddProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.AddProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Test exception");

            _mockDataAssetService.Verify(x => x.AddProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()), Times.Once);
        }
        [Test]
        public async Task PatchProfiledDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<PatchProfiledDataAssetRequest>(); 
            var data = _fixture.Create<IPatchDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ReturnsAsync(mockResult.Object);

            var expectedResponse = _fixture.Create<PatchProfiledDataAssetResponse>();
            _mockDataAssetResponseFactory
                .Setup(x => x.CreatePatchProfiledDataAssetResponse(It.IsAny<Guid>()))
                .Returns(expectedResponse);

            // Act
            var result = await _controller.PatchProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expectedResponse);

            _mockDataAssetService.Verify(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()), Times.Once);
            _mockDataAssetResponseFactory.Verify(x => x.CreatePatchProfiledDataAssetResponse(It.IsAny<Guid>()), Times.Once);
        }
        [Test]
        public async Task PatchProfiledDataAsset_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<PatchProfiledDataAssetRequest>();
            var data = _fixture.Create<IPatchDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.PatchProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            _mockDataAssetService.Verify(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                            It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()), Times.Once);
        }

        [Test]
        public async Task PatchProfiledDataAsset_ShouldReturnForbidden_WhenUnauthorizedAccessExceptionIsThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<PatchProfiledDataAssetRequest>();
            var data = _fixture.Create<IPatchDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ThrowsAsync(new UnAuthorizedAccessToDataAssetException());

            // Act
            var result = await _controller.PatchProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<ForbidResult>();

            _mockDataAssetService.Verify(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(),
                                                        It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()), Times.Once);
        }

        [Test]
        public async Task PatchProfiledDataAsset_ShouldReturnBadRequest_WhenGeneralExceptionIsThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<PatchProfiledDataAssetRequest>();
            var data = _fixture.Create<IPatchDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IPatchDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(), It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.PatchProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Test exception");

            _mockDataAssetService.Verify(x => x.PatchProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<DataAssetType>(), It.IsAny<string>(), It.IsAny<ManagementMetadataBase>(), It.IsAny<DataAssetActionSourceEnum>()), Times.Once);
        }
        [Test]
        public async Task GetProfiledDataAssets_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetsRequest>();

            var data = _fixture.Create<IGetProfiledDataAssetsResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.GetProfiledDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<List<DataAssetType>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var expectedResponse = _fixture.Create<GetProfiledDataAssetsResponse>();

            _mockDataAssetResponseFactory
                .Setup(x => x.CreateGetProfiledDataAssetsResponse(It.IsAny<int>(), It.IsAny<IEnumerable<ProfiledDataAsset>>()))
                .Returns(expectedResponse);

            // Act
            var result = await _controller.GetProfiledDataAssets(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expectedResponse);

            _mockDataAssetService.Verify(x => x.GetProfiledDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<List<DataAssetType>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);

            _mockDataAssetResponseFactory.Verify(x => x.CreateGetProfiledDataAssetsResponse(It.IsAny<int>(), It.IsAny<IEnumerable<ProfiledDataAsset>>()), Times.Once);
        }
        [Test]
        public async Task GetProfiledDataAssets_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetsRequest>();

            var data = _fixture.Create<IGetProfiledDataAssetsResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.GetProfiledDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<List<DataAssetType>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var expectedResponse = _fixture.Create<GetProfiledDataAssetsResponse>();

            _mockDataAssetResponseFactory
                .Setup(x => x.CreateGetProfiledDataAssetsResponse(It.IsAny<int>(), It.IsAny<IEnumerable<ProfiledDataAsset>>()))
                .Returns(expectedResponse);

            // Act
            var result = await _controller.GetProfiledDataAssets(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockDataAssetService.Verify(x => x.GetProfiledDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<List<DataAssetType>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);

        }
        [Test]
        public async Task GetProfiledDataAssets_ShouldReturnBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetProfiledDataAssets(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            _mockDataAssetService.Verify(x => x.GetProfiledDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<List<DataAssetType>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);

        }
        [Test]
        public async Task GetProfiledDataAssetIds_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetIdsRequest>();
            var data = _fixture.Create<IGetProfiledDataAssetIdsResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetIdsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(service => service.GetProfiledDataAssetIdsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = _fixture.Create<GetProfiledDataAssetIdsResponse>();

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetProfiledDataAssetIdsResponse(It.IsAny<int>(), It.IsAny<string[]>()))
                .Returns(response);

            // Act
            var result = await _controller.GetProfiledDataAssetIds(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }

        [Test]
        public async Task GetProfiledDataAssetIds_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetIdsRequest>();
            var data = _fixture.Create<IGetProfiledDataAssetIdsResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetIdsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(service => service.GetProfiledDataAssetIdsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetProfiledDataAssetIds(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetProfiledDataAssetIds_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetIdsRequest>();
            _mockDataAssetService
                .Setup(service => service.GetProfiledDataAssetIdsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Some unexpected error"));

            // Act
            var result = await _controller.GetProfiledDataAssetIds(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Some unexpected error"));
        }

        [Test]
        public async Task GetProfiledDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetRequest>();

            var data = _fixture.Create<IGetProfiledDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            var expectedResponse = _fixture.Create<GetProfiledDataAssetResponse>();

            _mockDataAssetResponseFactory
                .Setup(x => x.CreateGetProfiledDataAssetResponse(It.IsAny<ProfiledDataAsset>()))
                .Returns(expectedResponse);

            // Act
            var result = await _controller.GetProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expectedResponse);

            _mockDataAssetService.Verify(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);

            _mockDataAssetResponseFactory.Verify(x => x.CreateGetProfiledDataAssetResponse(It.IsAny<ProfiledDataAsset>()), Times.Once);
        }
        [Test]
        public async Task GetProfiledDataAsset_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetRequest>();

            var data = _fixture.Create<IGetProfiledDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetProfiledDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            _mockDataAssetService.Verify(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
        }
        [Test]
        public async Task GetProfiledDataAsset_ShouldReturnBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetProfiledDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetProfiledDataAsset(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            _mockDataAssetService.Verify(x => x.GetProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Once);
        }
        [Test]
        public async Task DeleteProfiledDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<DeleteProfiledDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IDeleteDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.DeleteProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            var response = _fixture.Create<DeleteProfiledDataAssetResponse>();

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateDeleteProfiledDataAssetResponse(It.IsAny<Guid>()))
                .Returns(response);

            // Act
            var result = await _controller.DeleteProfiledDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }

        [Test]
        public async Task DeleteProfiledDataAsset_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<DeleteProfiledDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IDeleteDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IDeleteDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.DeleteProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object); ;

            // Act
            var result = await _controller.DeleteProfiledDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task DeleteProfiledDataAsset_ShouldReturnForbidden_WhenUnauthorizedAccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<DeleteProfiledDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);
            var exception = new UnAuthorizedAccessToDataAssetException();

            _mockDataAssetService
                .Setup(service => service.DeleteProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.DeleteProfiledDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [Test]
        public async Task DeleteProfiledDataAsset_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var request = _fixture.Create<DeleteProfiledDataAssetRequest>();
            var exception = new Exception("Some unexpected error");
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(service => service.DeleteProfiledDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.DeleteProfiledDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Some unexpected error"));
        }

        [Test]
        public void DeleteProfiledDataAsset_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            // Arrange
            DeleteProfiledDataAssetRequest? request = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _controller.DeleteProfiledDataAsset(request));
        }
        [Test]
        public async Task GetCddoDataAssets_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetCddoDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IGetCddoDataAssetsResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IEnumerable<DataAssetType>>() , It.IsAny<List<DataAssetStatus>>()
                , It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DataAssetsSortField>(), It.IsAny<DataAssetsSortDirection>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetCddoDataAssetsResponse()
            {
                CddoDataAssets = new List<CddoDataAsset>(),
                TotalNumberOfMatchingCddoDataAssets = 1
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetCddoDataAssetsResponse(It.IsAny<int>(), It.IsAny<CddoDataAsset[]>()))
                .Returns(response);

            // Act
            var result = await _controller.GetCddoDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }

        [Test]
        public async Task GetCddoDataAssets_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetCddoDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IGetCddoDataAssetsResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<List<DataAssetStatus>>()
                , It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DataAssetsSortField>(), It.IsAny<DataAssetsSortDirection>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetCddoDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetCddoDataAssets_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var request = _fixture.Create<GetCddoDataAssetsRequest>();
            var exception = new Exception("Some unexpected error");
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IEnumerable<DataAssetType>>(), It.IsAny<List<DataAssetStatus>>()
                , It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DataAssetsSortField>(), It.IsAny<DataAssetsSortDirection>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetCddoDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Some unexpected error"));
        }

        [Test]
        public void GetCddoDataAssets_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            // Arrange
            GetCddoDataAssetsRequest? request = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _controller.GetCddoDataAssets(request));
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetCddoDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IGetCddoDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetCddoDataAssetResponse
            {
                CddoDataAsset = new CddoDataAsset()
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetCddoDataAssetResponse(It.IsAny<CddoDataAsset>()))
                .Returns(response);

            // Act
            var result = await _controller.GetCddoDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetCddoDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IGetCddoDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetCddoDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetCddoDataAsset_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var exception = new Exception("Some unexpected error");
            clearInvocations();

            var request = _fixture.Create<GetCddoDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var data = _fixture.Create<IGetCddoDataAssetResult>();
            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Data).Returns(data);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.GetCddoDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Some unexpected error"));
        }

        [Test]
        public void GetCddoDataAsset_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            // Arrange
            GetCddoDataAssetRequest? request = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _controller.GetCddoDataAsset(request));
        }
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssets_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetSearchSuggestionsForPublishedDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var searchSuggestions = _fixture.Create<List<string>>(); // Simulating search suggestions
            var mockResult = new Mock<IServiceOperationDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(new GetSearchSuggestionsForPublishedDataAssetsResult
            {
                SearchSuggestionsForPublishedDataAssets = searchSuggestions
            });

            _mockDataAssetService
                .Setup(service => service.GetSearchSuggestionsForPublishedDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetSearchSuggestionsForPublishedDataAssetsResponse
            {
                SearchSuggestionsForPublishedDataAssets = searchSuggestions
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetSearchSuggestionsForPublishedDataAssetsResponse(It.IsAny<List<string>>()))
                .Returns(response);

            // Act
            var result = await _controller.GetSearchSuggestionsForPublishedDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssets_ShouldReturnBadRequest_WhenServiceFails()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetSearchSuggestionsForPublishedDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IGetSearchSuggestionsForPublishedDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Error message");

            _mockDataAssetService
                .Setup(service => service.GetSearchSuggestionsForPublishedDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetSearchSuggestionsForPublishedDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error message"));
        }
        //[Test]
        //public async Task GetSearchSuggestionsForPublishedDataAssets_ShouldReturnBadRequest_WhenRequestIsNull()
        //{
        //    // Arrange
        //    clearInvocations();

        //    // Act
        //    var result = await _controller.GetSearchSuggestionsForPublishedDataAssets(null);

        //    // Assert
        //    Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        //    var badRequestResult = (BadRequestObjectResult)result;
        //    Assert.That(badRequestResult?.Value, Is.EqualTo("Value cannot be null. (Parameter 'getSearchSuggestionsForPublishedDataAssetsRequest')"));
        //}
        [Test]
        public async Task GetSearchSuggestionsForPublishedDataAssets_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetSearchSuggestionsForPublishedDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            _mockDataAssetService.Setup(x => x.GetSearchSuggestionsForPublishedDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>())).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetSearchSuggestionsForPublishedDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssets_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetSearchSuggestionsForOrganisationDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var searchSuggestions = _fixture.Create<List<string>>(); // Simulating search suggestions
            var mockResult = new Mock<IServiceOperationDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(new GetSearchSuggestionsForOrganisationDataAssetsResult
            {
                SearchSuggestionsForOrganisationDataAssets = searchSuggestions
            });

            _mockDataAssetService
                .Setup(service => service.GetSearchSuggestionsForOrganisationDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetSearchSuggestionsForOrganisationDataAssetsResponse
            {
                SearchSuggestionsForOrganisationDataAssets = searchSuggestions
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetSearchSuggestionsForOrganisationDataAssetsResponse(It.IsAny<List<string>>()))
                .Returns(response);

            // Act
            var result = await _controller.GetSearchSuggestionsForOrganisationDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssets_ShouldReturnBadRequest_WhenServiceFails()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetSearchSuggestionsForOrganisationDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IGetSearchSuggestionsForOrganisationDataAssetsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Error message");

            _mockDataAssetService
                .Setup(service => service.GetSearchSuggestionsForOrganisationDataAssetsAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetSearchSuggestionsForOrganisationDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error message"));
        }
        [Test]
        public async Task GetSearchSuggestionsForOrganisationDataAssets_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetSearchSuggestionsForOrganisationDataAssetsRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetSearchSuggestionsForOrganisationDataAssets(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task ValidateCddoDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var dataAssetId = Guid.NewGuid();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>>();
            mockResult.Setup(r => r.Success).Returns(true);

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetValidationPropertyErrorsAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);



            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetCddoDataAssetValidationErrorsResponse(It.IsAny<IEnumerable<IDataAssetValidationPropertyResult>>()));

            // Act
            var result = await _controller.ValidateCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }
        [Test]
        public async Task ValidateCddoDataAsset_ShouldReturnBadRequest_WhenValidationServiceFails()
        {
            // Arrange
            clearInvocations();

            var dataAssetId = Guid.NewGuid();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IEnumerable<IDataAssetValidationPropertyResult>>>();

            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Validation failed");

            _mockDataAssetService
                .Setup(service => service.GetCddoDataAssetValidationPropertyErrorsAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.ValidateCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Validation failed"));
        }

        [Test]
        public async Task ValidateCddoDataAsset_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var dataAssetId = Guid.NewGuid();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.ValidateCddoDataAsset(dataAssetId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error with your request Unexpected error"));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAsset_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<CheckForPotentialDuplicatesToDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            var potentialDuplicateDataAssetInformation = _fixture.Create<IEnumerable<PotentialDuplicateDataAssetInformation>>();
            mockResult.Setup(r => r.Data.PotentialDuplicateDataAssetInformation).Returns(potentialDuplicateDataAssetInformation);


            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateCheckForPotentialDuplicatesToDataAssetResponse(It.IsAny<IEnumerable<PotentialDuplicateDataAssetInformation>>()));

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAsset_ShouldReturnBadRequest_WhenServiceFails()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<CheckForPotentialDuplicatesToDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Error checking for duplicates");

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error checking for duplicates"));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAsset_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<CheckForPotentialDuplicatesToDataAssetRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAsset(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task ValidateProfiledDataAssetsSpreadsheetContent_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var file = new Mock<IFormFile>(); 
            var dataAssetProfileId = "someProfileId";
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var validationResult = _fixture.Create<IValidateProfiledDataAssetsSpreadsheetContentResult>();
            var validationSummary = _fixture.Create<IProfiledDataAssetsSpreadsheetValidationSummary>();
            var mockResult = new Mock<IServiceOperationDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(r => r.Data.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(validationResult);
            mockResult.Setup(x => x.Data.ProfiledDataAssetsSpreadsheetValidationSummary).Returns(validationSummary);

            _mockDataAssetService
                .Setup(service => service.ValidateProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>(), It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = new ValidateProfiledDataAssetsSpreadsheetContentResponse
            {
                Success = validationResult.Success
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateValidateProfiledDataAssetsSpreadsheetContentResponse(It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<IProfiledDataAssetsSpreadsheetValidationSummary>()))
                .Returns(response);

            // Act
            var result = await _controller.ValidateProfiledDataAssetsSpreadsheetContent(file.Object, dataAssetProfileId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task ValidateProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenValidationServiceFails()
        {
            // Arrange
            clearInvocations();

            var file = new Mock<IFormFile>(); 
            var dataAssetProfileId = "someProfileId";
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IValidateProfiledDataAssetsSpreadsheetContentResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Validation failed");

            _mockDataAssetService
                .Setup(service => service.ValidateProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>(), It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.ValidateProfiledDataAssetsSpreadsheetContent(file.Object, dataAssetProfileId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Validation failed"));
        }
        [Test]
        public async Task ValidateProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenDataAssetProfileIdIsNullOrWhiteSpace()
        {
            // Arrange
            clearInvocations();

            var file = new Mock<IFormFile>();  
            var dataAssetProfileId = "string with spaces"; 

            // Act
            var result = await _controller.ValidateProfiledDataAssetsSpreadsheetContent(file.Object, dataAssetProfileId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var spreadsheetName = "TestSpreadsheet.xlsx";
            var validatedDataAssets = _fixture.Create<List<ValidatedProfiledDataAsset>>();

            var mockResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAssetSet>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(new ValidatedProfiledDataAssetSet
            {
                SpreadsheetName = spreadsheetName,
                ValidatedProfiledDataAssets = validatedDataAssets
            });

            _mockDataAssetService
                .Setup(service => service.GetValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetValidatedProfiledDataAssetsSpreadsheetContentResponse();

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetValidatedProfiledDataAssetsSpreadsheetContentResponse(It.IsAny<string>(), It.IsAny<IEnumerable<ValidatedProfiledDataAsset>>()))
                .Returns(response);

            // Act
            var result = await _controller.GetValidatedProfiledDataAssetsSpreadsheetContent();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenServiceFails()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAssetSet>>();

            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Failed to retrieve validated content");

            _mockDataAssetService
                .Setup(service => service.GetValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetValidatedProfiledDataAssetsSpreadsheetContent();

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Failed to retrieve validated content"));
        }

        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetValidatedProfiledDataAssetsSpreadsheetContent();

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetItemContent_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetItemContentRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var validatedProfiledDataAsset = _fixture.Create<ValidatedProfiledDataAsset>();
            var mockResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAsset>>();
            mockResult.Setup(r => r.Success).Returns(true);

            _mockDataAssetService
                .Setup(service => service.GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetValidatedProfiledDataAssetsSpreadsheetItemContentResponse
            {
                ValidatedProfiledDataAsset = validatedProfiledDataAsset
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetValidatedProfiledDataAssetsSpreadsheetItemContentResponse(It.IsAny<ValidatedProfiledDataAsset>()))
                .Returns(response);

            // Act
            var result = await _controller.GetValidatedProfiledDataAssetsSpreadsheetItemContent(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetItemContent_ShouldReturnBadRequest_WhenServiceFails()
        {
            // Arrange
            clearInvocations();

            var request = _fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetItemContentRequest>();
            var userDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ValidatedProfiledDataAsset>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Failed to retrieve validated item content");

            _mockDataAssetService
                .Setup(service => service.GetValidatedProfiledDataAssetsSpreadsheetItemContentAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetValidatedProfiledDataAssetsSpreadsheetItemContent(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Failed to retrieve validated item content"));
        }
        [Test]
        public async Task GetValidatedProfiledDataAssetsSpreadsheetItemContent_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();
            var request = _fixture.Create<GetValidatedProfiledDataAssetsSpreadsheetItemContentRequest>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetValidatedProfiledDataAssetsSpreadsheetItemContent(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();
            var request = _fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var publishedValidatedProfiledDataAssetsSpreadsheetContentItems = _fixture.Create<List<PublishedValidatedProfiledDataAssetsSpreadsheetContentItem>>();

            var mockResult = new Mock<IServiceOperationDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(x => x.Data).Returns(new PublishValidatedProfiledDataAssetsSpreadsheetContentResult
            {
                Success = true,
                PublishedValidatedProfiledDataAssetsSpreadsheetContentItems = publishedValidatedProfiledDataAssetsSpreadsheetContentItems,
                Errors = _fixture.Create<List<string>>()

            });

            _mockDataAssetService
                .Setup(service => service.PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>(), It.IsAny<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>()))
                .ReturnsAsync(mockResult.Object);

            var response = new PublishValidatedProfiledDataAssetsSpreadsheetContentResponse
            {
                Success = true,
                PublishedValidatedProfiledDataAssetsSpreadsheetContentItems = publishedValidatedProfiledDataAssetsSpreadsheetContentItems,
                Errors = _fixture.Create<List<string>>()
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreatePublishValidatedProfiledDataAssetsSpreadsheetContentResponse(It.IsAny<bool>(), It.IsAny<List<string>>(), It.IsAny<List<PublishedValidatedProfiledDataAssetsSpreadsheetContentItem>>()))
                .Returns(response);

            // Act
            var result = await _controller.PublishValidatedProfiledDataAssetsSpreadsheetContent(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenServiceFails()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();
            var request = _fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(userDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IPublishValidatedProfiledDataAssetsSpreadsheetContentResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(x => x.Error).Returns("Failed to publish validated profiled data assets content");

            _mockDataAssetService
                .Setup(service => service.PublishValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>(), It.IsAny<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.PublishValidatedProfiledDataAssetsSpreadsheetContent(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Failed to publish validated profiled data assets content"));
        }
        [Test]
        public async Task PublishValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var userDetails = _fixture.Create<IUserDetails>();
            var request = _fixture.Create<PublishValidatedProfiledDataAssetsSpreadsheetContentRequest>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.PublishValidatedProfiledDataAssetsSpreadsheetContent(request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task ClearValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var initiatingUserDetails = _fixture.Create<IUserDetails>();
            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationResult>();
            mockResult.Setup(r => r.Success).Returns(true);

            _mockDataAssetService
                .Setup(service => service.ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.ClearValidatedProfiledDataAssetsSpreadsheetContent();

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }
        [Test]
        public async Task ClearValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var initiatingUserDetails = _fixture.Create<IUserDetails>();
            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationResult>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(r => r.Error).Returns("Some error occurred");

            _mockDataAssetService
                .Setup(service => service.ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.ClearValidatedProfiledDataAssetsSpreadsheetContent();

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Some error occurred"));
        }
        [Test]
        public async Task ClearValidatedProfiledDataAssetsSpreadsheetContent_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var initiatingUserDetails = _fixture.Create<IUserDetails>();
            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            _mockDataAssetService
                .Setup(service => service.ClearValidatedProfiledDataAssetsSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.ClearValidatedProfiledDataAssetsSpreadsheetContent();

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContent_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetContentRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();
            var potentialDuplicates = _fixture.Create<List<PotentialDuplicatesToSpreadsheetItemInformation>>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(r => r.Data).Returns(new CheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult
            {
                PotentialDuplicatesToSpreadsheetContent = potentialDuplicates
            });

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ReturnsAsync(mockResult.Object);

            var response = new CheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse
            {
                PotentialDuplicatesToSpreadsheetContent = potentialDuplicates
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateCheckForPotentialDuplicatesInValidatedSpreadsheetContentResponse(It.IsAny<List<PotentialDuplicatesToSpreadsheetItemInformation>>()))
                .Returns(response);

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAssetSpreadsheetContent(checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContent_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetContentRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetContentResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(r => r.Error).Returns("Error while checking for duplicates");

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAssetSpreadsheetContent(checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error while checking for duplicates"));
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetContent_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetContentRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetSpreadsheetContentAsync(It.IsAny<IUserDetails>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAssetSpreadsheetContent(checkForPotentialDuplicatesInValidatedSpreadsheetContentRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItem_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetItemRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();
            var potentialDuplicates = _fixture.Create<PotentialDuplicatesToSpreadsheetItemInformation>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(r => r.Data).Returns(new CheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult
            {
                PotentialDuplicatesToSpreadsheetItem = potentialDuplicates
            });

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = new CheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse
            {
                PotentialDuplicatesToSpreadsheetItem = potentialDuplicates
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateCheckForPotentialDuplicatesInValidatedSpreadsheetItemResponse(It.IsAny<PotentialDuplicatesToSpreadsheetItemInformation>()))
                .Returns(response);

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAssetSpreadsheetItem(checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItem_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetItemRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<ICheckForPotentialDuplicatesToDataAssetSpreadsheetItemResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(r => r.Error).Returns("Error while checking for duplicates");

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAssetSpreadsheetItem(checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error while checking for duplicates"));
        }

        [Test]
        public async Task CheckForPotentialDuplicatesToDataAssetSpreadsheetItem_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest = _fixture.Create<CheckForPotentialDuplicatesInValidatedSpreadsheetItemRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            _mockDataAssetService
                .Setup(service => service.CheckForPotentialDuplicatesToDataAssetSpreadsheetItemAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.CheckForPotentialDuplicatesToDataAssetSpreadsheetItem(checkForPotentialDuplicatesInValidatedSpreadsheetItemRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task GetDataAssetTemplateSpreadsheet_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var getDataAssetTemplateSpreadsheetRequest = _fixture.Create<GetDataAssetTemplateSpreadsheetRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IEmbeddedResourceData>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(r => r.Data).Returns(new EmbeddedResourceData
            {
                Content = _fixture.Create<byte[]>(),
                ContentType = "string",
                FileName = "test"
            });

            _mockDataAssetService
                .Setup(service => service.GetDataAssetTemplateSpreadsheetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            var response = new FileContentResult(_fixture.Create<byte[]>(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetDataAssetTemplateSpreadsheetResponse(It.IsAny<IEmbeddedResourceData>()))
                .Returns(response);

            // Act
            var result = await _controller.GetDataAssetTemplateSpreadsheet(getDataAssetTemplateSpreadsheetRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<FileContentResult>());
        }
        [Test]
        public async Task GetDataAssetTemplateSpreadsheet_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var getDataAssetTemplateSpreadsheetRequest = _fixture.Create<GetDataAssetTemplateSpreadsheetRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);
            var mockResult = new Mock<IServiceOperationDataResult<IEmbeddedResourceData>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(r => r.Data).Returns(new EmbeddedResourceData
            {
                Content = _fixture.Create<byte[]>(),
                ContentType = "string",
                FileName = "test"
            });
            mockResult.Setup(r => r.Error).Returns("Error retrieving data asset template");

            _mockDataAssetService
                .Setup(service => service.GetDataAssetTemplateSpreadsheetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetDataAssetTemplateSpreadsheet(getDataAssetTemplateSpreadsheetRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Error retrieving data asset template"));
        }
        [Test]
        public async Task GetDataAssetTemplateSpreadsheet_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var getDataAssetTemplateSpreadsheetRequest = _fixture.Create<GetDataAssetTemplateSpreadsheetRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            _mockDataAssetService
                .Setup(service => service.GetDataAssetTemplateSpreadsheetAsync(It.IsAny<IUserDetails>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetDataAssetTemplateSpreadsheet(getDataAssetTemplateSpreadsheetRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }
        [Test]
        public async Task MigrateProfiledDataAssetsFrom1p0To3p1_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var migrateProfiledDataAssetsFrom1P0To3P1Request = _fixture.Create<MigrateProfiledDataAssetsFrom1p0To3p1Request>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();
            var migrationResult = new List<string> { "Migration successful" };

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(r => r.Data.Results).Returns( _fixture.Create<IEnumerable<IMigrateProfiledDataAssetFrom1p0To3p1Result>>());

            _mockDataAssetService
                .Setup(service => service.MigrateProfiledDataAssetsFrom1p0To3p1Async(It.IsAny<IUserDetails>(), It.IsAny<List<Guid>>()))
                .ReturnsAsync(mockResult.Object);

            var response = _fixture.Create<MigrateProfiledDataAssetsFrom1p0To3p1Response>();

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateMigrateProfiledDataAssetsFrom1p0To3p1Response(It.IsAny<IEnumerable<IMigrateProfiledDataAssetFrom1p0To3p1Result>>()))
                .Returns(response);

            // Act
            var result = await _controller.MigrateProfiledDataAssetsFrom1p0To3p1(migrateProfiledDataAssetsFrom1P0To3P1Request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task MigrateProfiledDataAssetsFrom1p0To3p1_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var migrateProfiledDataAssetsFrom1P0To3P1Request = _fixture.Create<MigrateProfiledDataAssetsFrom1p0To3p1Request>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IMigrateProfiledDataAssetsFrom1p0To3p1Result>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(r => r.Error).Returns("Migration failed due to an error.");

            _mockDataAssetService
                .Setup(service => service.MigrateProfiledDataAssetsFrom1p0To3p1Async(It.IsAny<IUserDetails>(), It.IsAny<List<Guid>>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.MigrateProfiledDataAssetsFrom1p0To3p1(migrateProfiledDataAssetsFrom1P0To3P1Request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Migration failed due to an error."));
        }
        [Test]
        public async Task MigrateProfiledDataAssetsFrom1p0To3p1_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var migrateProfiledDataAssetsFrom1P0To3P1Request = _fixture.Create<MigrateProfiledDataAssetsFrom1p0To3p1Request>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            _mockDataAssetService
                .Setup(service => service.MigrateProfiledDataAssetsFrom1p0To3p1Async(It.IsAny<IUserDetails>(), It.IsAny<List<Guid>>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.MigrateProfiledDataAssetsFrom1p0To3p1(migrateProfiledDataAssetsFrom1P0To3P1Request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }

        [Test]
        public async Task GetEsdaOwnershipDetails_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            clearInvocations();

            var getEsdaOwnershipDetailsRequest = _fixture.Create<GetEsdaOwnershipDetailsRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            var esdaOwnershipDetails = new GetEsdaOwnershipDetailsResult
            {
                EsdaId = new Guid(),
                Title = "Sample Title",
                OrganisationId = 1,
                DomainId = 1,
                ContactPointName = "John Doe",
                ContactPointEmailAddress = "johndoe@example.com",
                DataShareRequestNotificationRecipientType = DataShareRequestNotificationRecipientType.EsdaContactPointEmailAddress,
                CustomDsrNotificationAddress = "customaddress@example.com"
            };

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IGetEsdaOwnershipDetailsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);
            mockResult.Setup(r => r.Data).Returns(esdaOwnershipDetails);

            _mockDataAssetService
                .Setup(service => service.GetEsdaOwnershipDetailsAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            var response = new GetEsdaOwnershipDetailsResponse
            {
                EsdaId = esdaOwnershipDetails.EsdaId,
                Title = esdaOwnershipDetails.Title,
                OrganisationId = esdaOwnershipDetails.OrganisationId,
                DomainId = esdaOwnershipDetails.DomainId,
                ContactPointName = esdaOwnershipDetails.ContactPointName,
                ContactPointEmailAddress = esdaOwnershipDetails.ContactPointEmailAddress,
                DataShareRequestNotificationRecipientType = esdaOwnershipDetails.DataShareRequestNotificationRecipientType,
                CustomDsrNotificationAddress = esdaOwnershipDetails.CustomDsrNotificationAddress
            };

            _mockDataAssetResponseFactory
                .Setup(factory => factory.CreateGetEsdaOwnershipDetailsResponse(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DataShareRequestNotificationRecipientType>(), It.IsAny<string>()))
                .Returns(response);

            // Act
            var result = await _controller.GetEsdaOwnershipDetails(getEsdaOwnershipDetailsRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult?.Value, Is.EqualTo(response));
        }
        [Test]
        public async Task GetEsdaOwnershipDetails_ShouldReturnBadRequest_WhenFailure()
        {
            // Arrange
            clearInvocations();

            var getEsdaOwnershipDetailsRequest = _fixture.Create<GetEsdaOwnershipDetailsRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            var mockResult = new Mock<IServiceOperationDataResult<IGetEsdaOwnershipDetailsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);
            mockResult.Setup(r => r.Error).Returns("Failed to retrieve ESDA Ownership Details");

            _mockDataAssetService
                .Setup(service => service.GetEsdaOwnershipDetailsAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResult.Object);

            // Act
            var result = await _controller.GetEsdaOwnershipDetails(getEsdaOwnershipDetailsRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Failed to retrieve ESDA Ownership Details"));
        }
        [Test]
        public async Task GetEsdaOwnershipDetails_ShouldReturnBadRequest_WhenExceptionThrown()
        {
            // Arrange
            clearInvocations();

            var getEsdaOwnershipDetailsRequest = _fixture.Create<GetEsdaOwnershipDetailsRequest>();
            var initiatingUserDetails = _fixture.Create<IUserDetails>();

            _mockUserProfilePresenter.Setup(x => x.GetInitiatingUserDetailsAsync()).ReturnsAsync(initiatingUserDetails);

            _mockDataAssetService
                .Setup(service => service.GetEsdaOwnershipDetailsAsync(It.IsAny<IUserDetails>(), It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetEsdaOwnershipDetails(getEsdaOwnershipDetailsRequest);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Unexpected error"));
        }

    }
}