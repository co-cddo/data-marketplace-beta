using AutoFixture.AutoMoq;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Requests.Lookup;
using Agm.Catalog.DotNet.Logic.Services.DataAssets.Results;
using Cddo.Data.Marketplace.Logic.ServiceOperationResults;
using Moq;
using Agm.Catalog.DotNet.Logic.Services.Lookup.Results;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;

namespace Cddo.Data.Marketplace.Api.Test.Controllers
{
    [TestFixture]
    public class LookupControllerTests
    {
        protected readonly IFixture fixture;

        public LookupControllerTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public async Task GivenTopicsLookUpAsync_WhenTopicsLookUpAsyncFails_ThenAnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var request = fixture.Create<GetCddoTopicsRequest>();
            var topics = fixture.Create<List<string>>();

            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoTopicsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var data = new Mock<IGetCddoTopicsResult>();
            data.Setup(d => d.Topics).Returns(topics);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetCddoTopicsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<IEnumerable<DataAssetStatus>>())).ReturnsAsync(mockResult.Object);

            //Act
            var result = (BadRequestObjectResult)await testItems.LookupController.TopicsLookUpAsync(request);

            //Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
        }

        [Test]
        public async Task GivenTopicsLookUpAsync_WhenTopicsLookUpAsyncCalled_ReturnsTopics()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var request = fixture.Create<GetCddoTopicsRequest>();
            var topics = fixture.Create<List<string>>();

            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoTopicsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IGetCddoTopicsResult>();
            data.Setup(d => d.Topics).Returns(topics);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetCddoTopicsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<IEnumerable<DataAssetStatus>>())).ReturnsAsync(mockResult.Object);

            //Act
            var result = (OkObjectResult)await testItems.LookupController.TopicsLookUpAsync(request);

            //Assert
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task GivenTopicsLookUpAsync_WhenTopicsLookUpAsyncThrows_ThenAnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var request = fixture.Create<GetCddoTopicsRequest>();
            var topics = fixture.Create<List<string>>();

            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoTopicsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var data = new Mock<IGetCddoTopicsResult>();
            data.Setup(d => d.Topics).Returns(topics);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetCddoTopicsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<IEnumerable<DataAssetStatus>>())).ThrowsAsync(new Exception("Failed"));

            //Act
            var result = (BadRequestObjectResult)await testItems.LookupController.TopicsLookUpAsync(request);

            //Assert
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
        }

        [Test]
        public async Task GivenOrganisationLookUpAsync_WhenOrganisationLookUpAsyncFails_ThenAnErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var request = fixture.Create<GetCddoOrganisationsRequest>();
            var organisations = fixture.Create<List<string>>();

            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoOrganisationsResult>>();
            mockResult.Setup(r => r.Success).Returns(false);

            var data = new Mock<IGetCddoOrganisationsResult>();
            data.Setup(d => d.Organisations).Returns(organisations);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetCddoOrganisationsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<IEnumerable<DataAssetStatus>>())).ReturnsAsync(mockResult.Object);

            //Act
            var result = (BadRequestObjectResult)await testItems.LookupController.OrganisationLookUpAsync(request);

            //Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
        }

        [Test]
        public async Task GivenOrganisationLookUpAsync_WhenOrganisationLookUpAsyncCalled_ReturnsOrganisations()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var request = fixture.Create<GetCddoOrganisationsRequest>();
            var organisations = fixture.Create<List<string>>();

            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoOrganisationsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IGetCddoOrganisationsResult>();
            data.Setup(d => d.Organisations).Returns(organisations);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetCddoOrganisationsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<IEnumerable<DataAssetStatus>>())).ReturnsAsync(mockResult.Object);

            //Act
            var result = (OkObjectResult)await testItems.LookupController.OrganisationLookUpAsync(request);

            //Assert
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
        }

        [Test]
        public async Task GivenOrganisationLookUpAsync_WhenGetCddoOrganisationsAsyncThrows_ReturnsErrorMessage()
        {
            //Arrange
            var testItems = TestsSetUp.CreateTestItems();
            var request = fixture.Create<GetCddoOrganisationsRequest>();
            var organisations = fixture.Create<List<string>>();

            var mockResult = new Mock<IServiceOperationDataResult<IGetCddoOrganisationsResult>>();
            mockResult.Setup(r => r.Success).Returns(true);

            var data = new Mock<IGetCddoOrganisationsResult>();
            data.Setup(d => d.Organisations).Returns(organisations);
            mockResult.Setup(r => r.Data).Returns(data.Object);

            testItems.MockDataAssetService.Setup(d => d.GetCddoOrganisationsAsync(
                    It.IsAny<IUserDetails>(),
                    It.IsAny<IEnumerable<DataAssetStatus>>())).Throws(new Exception("Broken"));

            //Act
            var result = (BadRequestObjectResult)await testItems.LookupController.OrganisationLookUpAsync(request);

            //Assert
            result.Value.Should().NotBeNull();
            result.StatusCode.Should().Be(400);
        }
    }
}
