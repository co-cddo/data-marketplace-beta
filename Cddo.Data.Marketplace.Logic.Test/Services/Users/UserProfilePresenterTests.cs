using Agm.Catalog.DotNet.Core.Validation.EmailAddress;
using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Configuration;
using Cddo.Data.Marketplace.Logic.Services.Users.UserIdPresentation;
using FluentAssertions;
using Flurl;
using Flurl.Http.Testing;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Logic.Test.Services.Users
{
    public class UserProfilePresenterTests
    {
        protected readonly IFixture fixture;
        public UserProfilePresenterTests()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public async Task WhenGetInitiatingUserIdSetAsync_IscalledWithNoUserProfile_Null()
        {
            //Arrange
            var testItems = CreateTestItems();

            //Act
            var result = await testItems.UserProfilePresenter.GetInitiatingUserIdSetAsync();

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetInitiatingUserIdSetAsync_WhenInitiatingUserIdIsPresent_UserIdSet()
        {
            //Arrange
            var testItems = CreateTestItems();
            var userIdSet = fixture.Create<string>();
            var testResponse = fixture.Create<UserProfile>();
            var getUserInfoByTokenEndPoint = "http://xyz/users/UsersId";
            using var httpTest = new HttpTest();

            testItems.MockUserIdPresenter.Setup(p => p.GetInitiatingUserIdToken()).ReturnsAsync(userIdSet);
            testItems.MockUsersServiceConfigurationPresenter.Setup(c => c.GetUserInfoByTokenEndPoint()).Returns(getUserInfoByTokenEndPoint);

            httpTest.ForCallsTo(getUserInfoByTokenEndPoint)
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.UserProfilePresenter.GetInitiatingUserIdSetAsync();

            //Assert
            result.DomainId.Should().Be(testResponse.Domain.DomainId);
            result.UserId.Should().Be(testResponse.User.UserId);
            result.OrganisationId.Should().Be(testResponse.Organisation.OrganisationId);
        }

        [Test]
        public async Task WhenGetInitiatingUserDetailsAsync_IscalledWithNoUserProfile_Null()
        {
            //Arrange
            var testItems = CreateTestItems();

            //Act
            var result = await testItems.UserProfilePresenter.GetInitiatingUserDetailsAsync();

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetInitiatingUserDetailsAsync_WhenInitiatingUserIdIsPresent_UserIdSet()
        {
            //Arrange
            var testItems = CreateTestItems();
            var userIdSet = fixture.Create<string>();
            var testResponse = fixture.Create<UserProfile>();
            var getUserInfoByTokenEndPoint = "http://xyz/users/UsersDetails";
            using var httpTest = new HttpTest();

            testItems.MockUserIdPresenter.Setup(p => p.GetInitiatingUserIdToken()).ReturnsAsync(userIdSet);
            testItems.MockUsersServiceConfigurationPresenter.Setup(c => c.GetUserInfoByTokenEndPoint()).Returns(getUserInfoByTokenEndPoint);

            httpTest.ForCallsTo(getUserInfoByTokenEndPoint)
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.UserProfilePresenter.GetInitiatingUserDetailsAsync();

            //Assert
            result.UserIdSet.DomainId.Should().Be(testResponse.Domain.DomainId);
            result.UserIdSet.UserId.Should().Be(testResponse.User.UserId);
            result.UserIdSet.OrganisationId.Should().Be(testResponse.Organisation.OrganisationId);

            result.UserContactDetails.UserName.Should().Be(testResponse.User.UserName);
            result.UserContactDetails.EmailAddress.Should().Be(testResponse.User.UserEmail);

            result.OrganisationInformation.OrganisationName.Should().Be(testResponse.Organisation.OrganisationName);
            result.OrganisationInformation.OrganisationId.Should().Be(testResponse.Organisation.OrganisationId);
            result.OrganisationInformation.Domains.First().DomainId.Should().Be(testResponse.Domain.DomainId);
            result.OrganisationInformation.Domains.First().DomainName.Should().Be(testResponse.Domain.DomainName);
        }

        [Test]
        public async Task WhenGetUserDetailsByUserIdAsync_IscalledWithNoUserProfile_Null()
        {
            //Arrange
            var testItems = CreateTestItems();
            var userId = fixture.Create<int>();

            //Act
            var result = await testItems.UserProfilePresenter.GetUserDetailsByUserIdAsync(userId);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetUserDetailsByUserIdAsync_WhenInitiatingUserIdIsPresent_UserIdSet()
        {
            //Arrange
            var testItems = CreateTestItems();
            var userIdSet = fixture.Create<string>();
            var testResponse = fixture.Create<UserProfile>();
            var getUserInfoByTokenEndPoint = "http://xyz/users/Usersinfo";
            var userId = fixture.Create<int>();
            using var httpTest = new HttpTest();

            testItems.MockUserIdPresenter.Setup(p => p.GetInitiatingUserIdToken()).ReturnsAsync(userIdSet);
            testItems.MockUsersServiceConfigurationPresenter.Setup(c => c.GetUserInfoByUserIdEndPoint()).Returns(getUserInfoByTokenEndPoint);

            httpTest.ForCallsTo(getUserInfoByTokenEndPoint)
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.UserProfilePresenter.GetUserDetailsByUserIdAsync(userId);

            //Assert
            result.UserIdSet.DomainId.Should().Be(testResponse.Domain.DomainId);
            result.UserIdSet.UserId.Should().Be(testResponse.User.UserId);
            result.UserIdSet.OrganisationId.Should().Be(testResponse.Organisation.OrganisationId);

            result.UserContactDetails.UserName.Should().Be(testResponse.User.UserName);
            result.UserContactDetails.EmailAddress.Should().Be(testResponse.User.UserEmail);

            result.OrganisationInformation.OrganisationName.Should().Be(testResponse.Organisation.OrganisationName);
            result.OrganisationInformation.OrganisationId.Should().Be(testResponse.Organisation.OrganisationId);
            result.OrganisationInformation.Domains.First().DomainId.Should().Be(testResponse.Domain.DomainId);
            result.OrganisationInformation.Domains.First().DomainName.Should().Be(testResponse.Domain.DomainName);
        }

        [Test]
        public async Task WhenGetOrganisationInformationAsync_IscalledWithNoUserProfile_Null()
        {
            //Arrange
            var testItems = CreateTestItems();
            var organisationId = fixture.Create<int>();

            //Act
            var result = await testItems.UserProfilePresenter.GetOrganisationInformationAsync(organisationId);

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetOrganisationInformationAsync_WhenInitiatingUserIdIsPresent_UserIdSet()
        {
            //Arrange
            var testItems = CreateTestItems();
            var userIdSet = fixture.Create<string>();
            var testResponse = fixture.Create<OrganisationDetail>();
            var organisationId = fixture.Create<int>();
            var getUserInfoByTokenEndPoint = $"http://xyz/Organisation/Details/";

            using var httpTest = new HttpTest();

            testItems.MockUserIdPresenter.Setup(p => p.GetInitiatingUserIdToken()).ReturnsAsync(userIdSet);
            testItems.MockUsersServiceConfigurationPresenter.Setup(c => c.GetUserOrganisationByOrganisationIdEndPoint()).Returns(getUserInfoByTokenEndPoint);

            httpTest.ForCallsTo(getUserInfoByTokenEndPoint.AppendPathSegment(organisationId))
              .RespondWithJson(testResponse);

            //Act
            var result = await testItems.UserProfilePresenter.GetOrganisationInformationAsync(organisationId);

            //Assert
            result.OrganisationId.Should().Be(testResponse.OrganisationId);
            result.OrganisationName.Should().Be(testResponse.OrganisationName);

            result.Domains.First().DomainId.Should().Be(testResponse.Domains.First().DomainId);
            result.Domains.First().DomainName.Should().Be(testResponse.Domains.First().DomainName);
        }


        [Test]
        public async Task WhenGetDomainInformationOfInitiatingUserAsync_IscalledWithNoUserProfile_Null()
        {
            //Arrange
            var testItems = CreateTestItems();

            //Act
            var result = await testItems.UserProfilePresenter.GetDomainInformationOfInitiatingUserAsync();

            //Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetDomainInformationOfInitiatingUserAsync_WhenInitiatingUserIdIsPresent_DomainInformation()
        {
            //Arrange
            var testItems = CreateTestItems();
            var userIdSet = fixture.Create<string>();
            var testResponse = fixture.Create<OrganisationDetail>();
            var organisationId = fixture.Create<int>();
            var getUserOrganisationByOrganisationIdEndPoint = $"http://xyz/Organisation/Details/";
            var getUserInfoByTokenDetailsEndPoint = $"http://xyz/User/details/";

            var testResponseUser = fixture.Create<UserProfile>();

            //Set the domains for response
            var firstDomain = testResponse.Domains.First();
            testResponseUser.Domain = new UserDomain()
            {
                DomainId = (int)firstDomain.DomainId,
                DomainName = firstDomain.DomainName
            };
            using var httpTest = new HttpTest();

            testItems.MockUserIdPresenter.Setup(p => p.GetInitiatingUserIdToken()).ReturnsAsync(userIdSet);
            testItems.MockUsersServiceConfigurationPresenter.Setup(c => c.GetUserOrganisationByOrganisationIdEndPoint()).Returns(getUserOrganisationByOrganisationIdEndPoint);

            testItems.MockUsersServiceConfigurationPresenter.Setup(c => c.GetUserInfoByTokenEndPoint()).Returns(getUserInfoByTokenDetailsEndPoint);

            httpTest.ForCallsTo(getUserOrganisationByOrganisationIdEndPoint.AppendPathSegment(testResponseUser.Organisation.OrganisationId))
              .RespondWithJson(testResponse);

            httpTest.ForCallsTo(getUserInfoByTokenDetailsEndPoint)
             .RespondWithJson(testResponseUser);


            //Act
            var result = await testItems.UserProfilePresenter.GetDomainInformationOfInitiatingUserAsync();

            //Assert
            result.DomainId.Should().Be(firstDomain.DomainId);
            result.DomainName.Should().Be(firstDomain.DomainName);
        }
        #region Test Item Creation
        private static TestItems CreateTestItems()
        {
            var userIdPresenter = new Mock<IUserIdPresenter>();
            var usersServiceConfigurationPresenter = new Mock<IUsersServiceConfigurationPresenter>();

            //ConfigureHappyPathTesting();

            var mockAppInsightsLogger = new Mock<IAppInsightsLogger>();
            var userProfilePresenter = new UserProfilePresenter(userIdPresenter.Object, mockAppInsightsLogger.Object, usersServiceConfigurationPresenter.Object);

            return new TestItems(
               userProfilePresenter,
                userIdPresenter,
                usersServiceConfigurationPresenter);

            //void ConfigureHappyPathTesting()
            //{
            //    mockCddoEmailAddressValidation.Setup(x => x.IsEmailAddressValid(It.IsAny<string>()))
            //        .Returns(true);
            //}
        }

        private class TestItems(
            IUserProfilePresenter userProfilePresenter,
            Mock<IUserIdPresenter> mockUserIdPresenter,
            Mock<IUsersServiceConfigurationPresenter> mockUsersServiceConfigurationPresenter)
        {
            public IUserProfilePresenter UserProfilePresenter { get; } = userProfilePresenter;
            public Mock<IUserIdPresenter> MockUserIdPresenter { get; } = mockUserIdPresenter;
            public Mock<IUsersServiceConfigurationPresenter> MockUsersServiceConfigurationPresenter { get; } = mockUsersServiceConfigurationPresenter;
        }
        #endregion
    }
}
