using Agrimetrics.DataShare.Api.Dto.Models.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Answers.DataShareRequestQuestionAnswers;
using Agrimetrics.DataShare.Api.Dto.Models.DataShareRequests.Questions;
using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Models.Supplier.DataShareRequests.Decisions;
using Agrimetrics.DataShare.Api.Dto.Requests.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Requests.Supplier;
using Agrimetrics.DataShare.Api.Dto.Responses.Acquirer.DataShareRequests;
using Agrimetrics.DataShare.Api.Dto.Responses.AuditLogs;
using Agrimetrics.DataShare.Api.Dto.Responses.Notifications;
using Agrimetrics.DataShare.Api.Dto.Responses.Supplier;
using AutoFixture;
using Cddo.Data.Marketplace.Api.Dto.Models;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Exceptions;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Cddo.Data.Marketplace.UI.Builders;
using Cddo.Data.Marketplace.UI.Controllers;
using Cddo.Data.Marketplace.UI.Pages.DataRequest;
using Cddo.Data.Marketplace.UI.Pages.DataShare;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Moq;
using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.UI.Test.Controllers
{
    [TestFixture]
    public class DataRequestControllerTests
    {
        private Mock<IDataShareRequestService> _mockDataShareRequestService;
        private Mock<IQuestionDataBuilder> _mockQuestionDataBuilder;
        private Mock<IAppInsightsLogger> _mockLogger;
        private Mock<IUserRoleService> _mockUserRoleService;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private DataRequestController _controller;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private Fixture _fixture;

        #region SetUp
        public DataRequestControllerTests()
        {
            _mockDataShareRequestService = new Mock<IDataShareRequestService>();
            _mockQuestionDataBuilder = new Mock<IQuestionDataBuilder>();
            _mockLogger = new Mock<IAppInsightsLogger>();
            _mockUserRoleService = new Mock<IUserRoleService>();

            _fixture = new Fixture();

            _controller = new DataRequestController(
                _mockDataShareRequestService.Object,
                _mockQuestionDataBuilder.Object,
                _mockLogger.Object,
                _mockUserRoleService.Object
            );
        }

        private void SetAuthenticatedUser(bool isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "TestUser")
            };

            ClaimsIdentity identity;

            if (isAuthenticated)
            {
                identity = new ClaimsIdentity(claims, "TestAuthenticationType");
            }
            else
            {
                identity = new ClaimsIdentity();
            }

            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private void ClearInvocations()
        {
            _mockDataShareRequestService.Invocations.Clear();
            _mockQuestionDataBuilder.Invocations.Clear();
            _mockLogger.Invocations.Clear();
            _mockUserRoleService.Invocations.Clear();

        }
        #endregion

        #region Questions Tests

        [Test]
        public async Task RequestTasksListQuestions_ShouldReturnView_WithValidData()
        {
            // Arrange
            var esdaId = _fixture.Create<Guid>();
            var esdaName = _fixture.Create<string>();
            var cancellationToken = CancellationToken.None;
            var mockData = _fixture.Create<GetEsdaQuestionSetOutlineResponse>();

            SetAuthenticatedUser(true);

            _mockDataShareRequestService
                .Setup(service => service.GetEsdaQuestionSetOutline(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockData);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasksListQuestions(esdaId, esdaName, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult!.ViewData["EsdaName"], Is.EqualTo(esdaName));
            _mockDataShareRequestService.Verify(service => service.GetEsdaQuestionSetOutline(esdaId, cancellationToken), Times.Once);
        }

        [Test]
        public async Task RequestTasksListQuestions_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            var esdaId = _fixture.Create<Guid>();
            var esdaName = _fixture.Create<string>();
            var cancellationToken = CancellationToken.None;
            var mockData = _fixture.Create<GetEsdaQuestionSetOutlineResponse>();

            _mockDataShareRequestService
                .Setup(service => service.GetEsdaQuestionSetOutline(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockData);

            SetAuthenticatedUser(false);

            _controller.ModelState.AddModelError("key", "Invalid model");

            // Act
            var result = await _controller.RequestTasksListQuestions(esdaId, esdaName, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());

        }
        [Test]
        public async Task RequestQuestion_ShouldReturnView_WithValidData()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            SetAuthenticatedUser(true);

            var mockData = _fixture.Create<GetDataShareRequestQuestionInformationResponse>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockData);

            _mockQuestionDataBuilder
                .Setup(builder => builder.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>()))
                .Returns(questionModel);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataShare/Question.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(questionModel));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }

        [Test]
        public async Task RequestQuestion_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            SetAuthenticatedUser(false);

            _controller.ModelState.AddModelError("key", "Invalid model");

            var mockData = _fixture.Create<GetDataShareRequestQuestionInformationResponse>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockData);

            _mockQuestionDataBuilder
                .Setup(builder => builder.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>()))
                .Returns(questionModel);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            _mockLogger.Verify(logger => logger.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RequestQuestion_ShouldReturnErrorView_WhenDataShareRequestException400Occurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "Exception", DsrResponseText = "Test", DsrStatusCode = 400 });

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/400.cshtml"));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }
        [Test]
        public async Task RequestQuestion_ShouldReturnErrorView_WhenDataShareRequestException401Occurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "Exception", DsrResponseText = "Test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }
        [Test]
        public async Task RequestQuestion_ShouldReturnErrorView_WhenDataShareRequestException403Occurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "Exception", DsrResponseText = "Test", DsrStatusCode = 403 });

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/403.cshtml"));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }
        [Test]
        public async Task RequestQuestion_ShouldReturnErrorView_WhenDataShareRequestException404Occurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "Exception", DsrResponseText = "Test", DsrStatusCode = 404 });

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/404.cshtml"));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }
        [Test]
        public async Task RequestQuestion_ShouldReturnErrorView_WhenDataShareRequestException405Occurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "Exception", DsrResponseText = "Test", DsrStatusCode = 405 });

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/405.cshtml"));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }
        [Test]
        public async Task RequestQuestion_ShouldReturnErrorView_WhenDataShareRequestException500Occurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionSummary(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "Exception", DsrResponseText = "Test", DsrStatusCode = 500 });

            // Act
            var result = await _controller.RequestQuestion(requestId, questionId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/500.cshtml"));

            _mockDataShareRequestService.Verify(service => service.QuestionSummary(requestId, questionId, cancellationToken), Times.Once);
        }

        #endregion

        #region Tasks Tests

        [Test]
        public async Task RequestTasks_ShouldReturnView_WithValidData()
        {
            // Arrange
            ClearInvocations();

            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockSummaryResponse = _fixture.Create<GetDataShareRequestQuestionsSummaryResponse>();
            var mockAuditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService
                .Setup(service => service.QuestionsSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockSummaryResponse);

            _mockDataShareRequestService
                .Setup(service => service.GetDataShareRequestReturnCommentsAuditLog(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAuditLogResponse);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasks(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataShare/RequestTasks.cshtml"));

            var model = viewResult.Model as RequestTasksModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.DataShareRequestId, Is.EqualTo(mockSummaryResponse.DataShareRequestId));
            Assert.That(model.DataShareRequestRequestId, Is.EqualTo(mockSummaryResponse.DataShareRequestRequestId));

            _mockDataShareRequestService.Verify(service => service.QuestionsSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);


        }

        [Test]
        public async Task RequestTasks_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();

            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _controller.ModelState.AddModelError("key", "Invalid model");

            // Act
            var result = await _controller.RequestTasks(requestId, cancellationToken);

            // Assert
            _mockLogger.Verify(logger => logger.LogWarning(It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task RequestTasks_ShouldReturnErrorView_WhenDataShareRequestExceptionOccurs()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.QuestionsSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText= "test", DsrStatusCode = 401});

            // Act
            var result = await _controller.RequestTasks(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml")); 
        }

        [Test]
        public async Task RequestTasks_ShouldCheckUserPermissions_ForDeletion()
        {
            // Arrange
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockSummaryResponse = _fixture.Create<GetDataShareRequestQuestionsSummaryResponse>();
            var mockAuditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();
            var mockUserProfile = _fixture.Create<UserProfile>();

            _mockDataShareRequestService
                .Setup(service => service.QuestionsSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockSummaryResponse);

            _mockDataShareRequestService
                .Setup(service => service.GetDataShareRequestReturnCommentsAuditLog(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAuditLogResponse);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(mockUserProfile);

            _mockUserRoleService
                .Setup(service => service.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(true); // Mock user is in a role that allows deletion

            // Act
            var result = await _controller.RequestTasks(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            var model = viewResult.Model as RequestTasksModel;
            Assert.That(model.UserCanDeleteDataShareRequest, Is.True);
        }
        [Test]
        public async Task RequestTasksReviewAnswers_ShouldReturnRedirectToErrorPage_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);

            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _controller.ModelState.AddModelError("key", "Invalid model");

            // Act
            var result = await _controller.RequestTasksReviewAnswers(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task RequestTasksReviewAnswers_ShouldReturnView_WithValidData()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockAnswersSummaryResponse = _fixture.Create<GetDataShareRequestAnswersSummaryResponse>();
            var mockAuditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnswersSummaryResponse);

            _mockDataShareRequestService
                .Setup(service => service.GetDataShareRequestReturnCommentsAuditLog(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAuditLogResponse);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasksReviewAnswers(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataShare/ReviewAnswers.cshtml"));

            var model = viewResult.Model as ReviewAnswersModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.AnswersSummary, Is.EqualTo(mockAnswersSummaryResponse.AnswersSummary));
            Assert.That(model.DataShareRequestAuditLog, Is.EqualTo(mockAuditLogResponse.DataShareRequestAuditLog));

            _mockDataShareRequestService.Verify(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RequestTasksReviewAnswers_ShouldLogEvent_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockAnswersSummaryResponse = _fixture.Create<GetDataShareRequestAnswersSummaryResponse>();
            var mockAuditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnswersSummaryResponse);

            _mockDataShareRequestService
                .Setup(service => service.GetDataShareRequestReturnCommentsAuditLog(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAuditLogResponse);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasksReviewAnswers(requestId, cancellationToken);

            // Assert
            _mockLogger.Verify(logger => logger.LogEventMainBase(It.IsAny<UserEvent>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public async Task RequestTasksReviewAnswers_ShouldReturnErrorView_WhenDataShareRequestExceptionOccurs()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText="test", DsrResponseText = "test", DsrStatusCode= 401});

            // Act
            var result = await _controller.RequestTasksReviewAnswers(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml")); 
        }

        [Test]
        public async Task RequestTasksReadAnswers_ShouldReturnRedirectToErrorPage_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _controller.ModelState.AddModelError("key", "Invalid model");

            // Act
            var result = await _controller.RequestTasksReadAnswers(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/403"));
        }

        [Test]
        public async Task RequestTasksReadAnswers_ShouldReturnView_WithValidData()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockAnswersSummaryResponse = _fixture.Create<GetDataShareRequestAnswersSummaryResponse>();

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnswersSummaryResponse);

            _mockUserRoleService
                .Setup(service => service.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(true);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasksReadAnswers(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataShare/ReviewReadOnlyAnswers.cshtml"));

            var model = viewResult.Model as ReviewReadOnlyAnswersModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.AnswersSummary, Is.EqualTo(mockAnswersSummaryResponse.AnswersSummary));
            Assert.That(model.DataShareRequestId, Is.EqualTo(mockAnswersSummaryResponse.DataShareRequestId));
            Assert.That(model.UserCanDeleteDataShareRequest, Is.True);

            _mockDataShareRequestService.Verify(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task RequestTasksReadAnswers_ShouldLogEvent_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockAnswersSummaryResponse = _fixture.Create<GetDataShareRequestAnswersSummaryResponse>();

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnswersSummaryResponse);

            _mockUserRoleService
                .Setup(service => service.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(true);

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasksReadAnswers(requestId, cancellationToken);

            // Assert
            _mockLogger.Verify(logger => logger.LogEventMainBase(It.IsAny<UserEvent>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }
        [Test]
        public async Task RequestTasksReadAnswers_ShouldLogEvent_WhenUserIsAuthenticatedAndUserRole_THrows()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;
            var mockAnswersSummaryResponse = _fixture.Create<GetDataShareRequestAnswersSummaryResponse>();

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnswersSummaryResponse);

            _mockUserRoleService
                .Setup(service => service.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .Throws(new Exception());

            _mockUserRoleService
                .Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(_fixture.Create<UserProfile>());

            // Act
            var result = await _controller.RequestTasksReadAnswers(requestId, cancellationToken);

            // Assert
            _mockLogger.Verify(logger => logger.LogEventMainBase(It.IsAny<UserEvent>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Test]
        public async Task RequestTasksReadAnswers_ShouldReturnErrorView_WhenDataShareRequestExceptionOccurs()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var cancellationToken = CancellationToken.None;

            _mockDataShareRequestService
                .Setup(service => service.GetAnswerSummary(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.RequestTasksReadAnswers(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task DeleteReadAnswersRequest_ShouldReturnView_WhenModelStateIsValid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var dataShareRequestRequestId = _fixture.Create<string>();
            var esdaName = _fixture.Create<string>();

            // Act
            var result = _controller.DeleteReadAnswersRequest(requestId, dataShareRequestRequestId, esdaName);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/DeleteReadAnswersRequest.cshtml"));

            var model = viewResult.Model as DeleteReadAnswersRequestModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.DataShareRequestId, Is.EqualTo(requestId));
            Assert.That(model.DataShareRequestRequestId, Is.EqualTo(dataShareRequestRequestId));
            Assert.That(model.EsdaName, Is.EqualTo(esdaName));
        }

        [Test]
        public void DeleteReadAnswersRequest_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var dataShareRequestRequestId = _fixture.Create<string>();
            var esdaName = _fixture.Create<string>();

            _controller.ModelState.AddModelError("key", "Invalid model");

            // Act
            var result = _controller.DeleteReadAnswersRequest(requestId, dataShareRequestRequestId, esdaName);

            // Assert
            _mockLogger.Verify(logger => logger.LogWarning(It.Is<string>(msg => msg.Contains("Model state is invalid for DeleteReadAnswersRequest."))), Times.Once);
        }

        [Test]
        public async Task ConfirmDeleteReadAnswersRequest_ShouldRedirect_WhenSuccessful()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var deleteDataShareRequestResponse = _fixture.Create<DeleteDataShareRequestResponse>();

            _mockDataShareRequestService.Setup(service => service.DeleteDataShareRequest(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteDataShareRequestResponse);

            _mockUserRoleService.Setup(service => service.GetUserProfileAsync())
                .ReturnsAsync(new UserProfile()); 

            // Act
            var result = await _controller.ConfirmDeleteReadAnswersRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult.ActionName, Is.EqualTo(nameof(ManageDataRequestController.GotoManageCreatedDataShare)));
            Assert.That(redirectResult.ControllerName, Is.EqualTo("ManageDataRequest"));
        }

        [Test]
        public void ConfirmDeleteReadAnswersRequest_ShouldLogWarning_WhenModelStateIsInvalid()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            _controller.ModelState.AddModelError("key", "Invalid model");

            // Act
            var result = _controller.ConfirmDeleteReadAnswersRequest(requestId).Result;

            // Assert
            _mockLogger.Verify(logger => logger.LogWarning(It.Is<string>(msg => msg.Contains("Model state is invalid for ConfirmDeleteReadAnswersRequest."))), Times.Once);
        }

        [Test]
        public async Task ConfirmDeleteReadAnswersRequest_ShouldLogEvent_WhenUserIsAuthenticated()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var deleteDataShareRequestResponse = _fixture.Create<DeleteDataShareRequestResponse>();

            _mockDataShareRequestService.Setup(service => service.DeleteDataShareRequest(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteDataShareRequestResponse);

            var userProfile = new UserProfile(); // Assuming UserProfile is a class that represents the user
            _mockUserRoleService.Setup(service => service.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Act
            var result = await _controller.ConfirmDeleteReadAnswersRequest(requestId);

            // Assert
            _mockLogger.Verify(logger => logger.LogEventMainBase(
                DataSharingEvent.DataSharingRequestDeleted,
                "DataShareRequest",
                "CDDO",
                "RequestDeleteReadOnlySubmit",
                "Request",
                requestId.ToString(),
                It.IsAny<Dictionary<string, string>>()
            ), Times.Once);
        }

        [Test]
        public async Task ConfirmDeleteReadAnswersRequest_ShouldHandleException_WhenDeleteFails()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var exception = new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 };

            _mockDataShareRequestService.Setup(service => service.DeleteDataShareRequest(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.ConfirmDeleteReadAnswersRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));

        }
        [Test]
        public async Task CancelDeleteReadAnswersRequest_InvalidModelState_LogsWarningAndRedirects()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            var result = await _controller.CancelDeleteReadAnswersRequest(requestId) as RedirectToActionResult;

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for CancelDeleteReadAnswersRequest."), Times.Once);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("RequestTasksReadAnswers"));
            Assert.That(result.RouteValues["requestId"], Is.EqualTo(requestId));
        }

        [Test]
        public async Task CancelDeleteReadAnswersRequest_ValidModelState_RedirectsToRequestTasksReadAnswers()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();

            // Act
            var result = await _controller.CancelDeleteReadAnswersRequest(requestId) as RedirectToActionResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ActionName, Is.EqualTo("RequestTasksReadAnswers"));
            Assert.That(result.RouteValues["requestId"], Is.EqualTo(requestId));
        }

        [Test]
        public async Task RequestSubmitDataShareRequest_InvalidModelState_LogsValidationErrors()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            await _controller.RequestSubmitDataShareRequest(requestId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogEventMainBase<DataSharingEvent>(
                It.IsAny<DataSharingEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                requestId.ToString(),
                It.IsAny<Dictionary<string, string>>()
            ), Times.Once);
        }

        [Test]
        public async Task RequestSubmitDataShareRequest_ValidModelState_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var userProfile = new UserProfile();
            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Act
            var result = await _controller.RequestSubmitDataShareRequest(requestId, CancellationToken.None) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataShare/SubmissionDeclaration.cshtml"));
            Assert.That(result.Model, Is.InstanceOf<SubmissionDeclarationModel>());
            Assert.That(((SubmissionDeclarationModel)result.Model).DataShareRequestId, Is.EqualTo(requestId));
        }
        [Test]
        public async Task RequestSubmitDataShareRequest_ValidModelState_ThrowsException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var userProfile = new UserProfile();
            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).Throws(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.RequestSubmitDataShareRequest(requestId, CancellationToken.None) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));

        }
        [Test]
        public void DataShareRequestComplete_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestRequestId = _fixture.Create<string>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            _controller.DataShareRequestComplete(requestRequestId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for DataShareRequestComplete."), Times.Once);
        }

        [Test]
        public void DataShareRequestComplete_ValidModelState_ReturnsViewWithViewBag()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestRequestId = _fixture.Create<string>();

            // Act
            var result = _controller.DataShareRequestComplete(requestRequestId, CancellationToken.None) as ViewResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.EqualTo("~/Pages/DataShare/DataShareRequestComplete.cshtml"));
            Assert.That(_controller.ViewBag.RequestRequestId, Is.EqualTo(requestRequestId));
        }

        [Test]
        public async Task SetQuestionAnswerAndContinue_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            var userProfile = _fixture.Create<UserProfile>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userProfile);
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionAnswerFromFormData(It.IsAny<IFormCollection>())).Returns(_fixture.Create<DataShareRequestQuestionAnswer>());
            _mockDataShareRequestService.Setup(x => x.SubmitAnswerQuestion(It.IsAny<SetDataShareRequestQuestionAnswerRequest>(), default)).ReturnsAsync(_fixture.Create<SetDataShareRequestQuestionAnswerResponse>());
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>())).Returns(questionModel);


            // Act
            await _controller.SetQuestionAnswerAndContinue(requestId, questionId, form, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for SetQuestionAnswerAndContinue."), Times.Once);
        }

        [Test]
        public async Task SetQuestionAnswerAndContinue_ValidUser_LogsEvent()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var userProfile = _fixture.Create<UserProfile>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userProfile);
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionAnswerFromFormData(It.IsAny<IFormCollection>())).Returns(_fixture.Create<DataShareRequestQuestionAnswer>());
            _mockDataShareRequestService.Setup(x => x.SubmitAnswerQuestion(It.IsAny<SetDataShareRequestQuestionAnswerRequest>(), default)).ReturnsAsync(_fixture.Create<SetDataShareRequestQuestionAnswerResponse>());
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>())).Returns(questionModel);

            // Act
            await _controller.SetQuestionAnswerAndContinue(requestId, questionId, form, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogEventMainBase<UserEvent>(
                It.IsAny<UserEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>()
            ), Times.Exactly(2));
        }
        [Test]
        public async Task SetQuestionAnswerAndContinue_ValidUser_LogsEvent_ThrowsError()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var userProfile = _fixture.Create<UserProfile>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userProfile);
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionAnswerFromFormData(It.IsAny<IFormCollection>())).Throws(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });
            _mockDataShareRequestService.Setup(x => x.SubmitAnswerQuestion(It.IsAny<SetDataShareRequestQuestionAnswerRequest>(), default)).ReturnsAsync(_fixture.Create<SetDataShareRequestQuestionAnswerResponse>());
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>())).Returns(questionModel);

            // Act
            var result = await _controller.SetQuestionAnswerAndContinue(requestId, questionId, form, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));

        }

        [Test]
        public async Task SetQuestionAnswerAndReturn_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var userProfile = _fixture.Create<UserProfile>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userProfile);
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionAnswerFromFormData(It.IsAny<IFormCollection>())).Returns(_fixture.Create<DataShareRequestQuestionAnswer>());
            _mockDataShareRequestService.Setup(x => x.SubmitAnswerQuestion(It.IsAny<SetDataShareRequestQuestionAnswerRequest>(), default)).ReturnsAsync(_fixture.Create<SetDataShareRequestQuestionAnswerResponse>());
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>())).Returns(questionModel);

            // Act
            await _controller.SetQuestionAnswerAndReturn(requestId, questionId, form, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for SetQuestionAnswerAndReturn."), Times.Once);
        }

        [Test]
        public async Task SetQuestionAnswerAndReturn_ValidModelState_CallsDoSetQuestionAnswer()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var questionId = _fixture.Create<Guid>();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var userProfile = _fixture.Create<UserProfile>();
            var questionModel = new QuestionModel()
            {
                DataShareRequestId = requestId,
                QuestionId = questionId,
                DataShareRequestRequestId = "1",
                Footer = new DataShareRequestQuestionFooter(),
                QuestionParts = new List<Pages.DataShare._Partial.QuestionPartModel>()
            };

            _mockUserRoleService.Setup(u => u.GetUserProfileAsync()).ReturnsAsync(userProfile);
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionAnswerFromFormData(It.IsAny<IFormCollection>())).Returns(_fixture.Create<DataShareRequestQuestionAnswer>());
            _mockDataShareRequestService.Setup(x => x.SubmitAnswerQuestion(It.IsAny<SetDataShareRequestQuestionAnswerRequest>(), default)).ReturnsAsync(_fixture.Create<SetDataShareRequestQuestionAnswerResponse>());
            _mockQuestionDataBuilder.Setup(x => x.BuildQuestionModelFromDataShareRequestQuestion(It.IsAny<DataShareRequestQuestion>())).Returns(questionModel);

            // Act
            var result = await _controller.SetQuestionAnswerAndReturn(requestId, questionId, form, CancellationToken.None);

            // Assert
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }
        #endregion

        #region Datashare Tests
        [Test]
        public async Task DataShareRequestPrevious_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var esdaId = _fixture.Create<Guid>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            var mockResponse = new GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse()
            {
                EsdaId = esdaId,
                DataShareRequestSummaries = _fixture.Create<DataShareRequestRaisedForEsdaByAcquirerOrganisationSummarySet>()
            };

            _mockDataShareRequestService.Setup(s => s.GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(esdaId, CancellationToken.None))
                .ReturnsAsync(mockResponse);

            // Act
            await _controller.DataShareRequestPrevious(esdaId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for DataShareRequestPrevious."), Times.Once);
        }

        [Test]
        public async Task DataShareRequestPrevious_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var esdaId = _fixture.Create<Guid>();
            var mockResponse = new GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisationResponse()
            {
                EsdaId = esdaId,
                DataShareRequestSummaries = _fixture.Create<DataShareRequestRaisedForEsdaByAcquirerOrganisationSummarySet>()
            };

            _mockDataShareRequestService.Setup(s => s.GetDataShareRequestSummariesRaisedForEsdaByAcquirerOrganisation(esdaId, CancellationToken.None))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.DataShareRequestPrevious(esdaId, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataShare/DataShareRequestPrevious.cshtml"));
            var model = viewResult.Model as DataShareRequestPreviousModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.EsdaId, Is.EqualTo(esdaId));
        }
        [Test]
        public async Task RequestStartSubmit_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var esdaId = _fixture.Create<Guid>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            // Act
            await _controller.RequestStartSubmit(esdaId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for RequestStartSubmit."), Times.Once);
        }

        [Test]
        public async Task RequestStartSubmit_ValidRequest_RedirectsToRequestTasks()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var esdaId = _fixture.Create<Guid>();
            var requestId = _fixture.Create<Guid>();

            _mockDataShareRequestService.Setup(s => s.StartDataSharingRequest(esdaId, CancellationToken.None))
                .ReturnsAsync(requestId);

            // Act
            var result = await _controller.RequestStartSubmit(esdaId, CancellationToken.None);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("RequestTasks"));
            Assert.That(redirectResult.RouteValues["requestId"], Is.EqualTo(requestId));
            Assert.That(redirectResult.RouteValues["esdaId"], Is.EqualTo(esdaId));
        }

        [Test]
        public async Task RequestStartSubmit_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var esdaId = _fixture.Create<Guid>();
            var exception = new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 };

            _mockDataShareRequestService.Setup(s => s.StartDataSharingRequest(esdaId, CancellationToken.None))
                .ThrowsAsync(exception);
            // Act
            var result = await _controller.RequestStartSubmit(esdaId, default);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task DeclareRequestSubmission_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            var requestRequestId = _fixture.Create<string>();
            var responseMock = new SubmitDataShareRequestResponse { DataShareRequestRequestId = requestRequestId, NotificationSuccess = NotificationSuccess.SentSuccessfully };

            _mockDataShareRequestService.Setup(s => s.SubmitDataShareRequest(requestId, CancellationToken.None))
                .ReturnsAsync(responseMock);

            // Act
            await _controller.DeclareRequestSubmission(requestId, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for DeclareRequestSubmission."), Times.Once);
        }

        [Test]
        public async Task DeclareRequestSubmission_ValidRequest_RedirectsToDataShareRequestComplete()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = _fixture.Create<Guid>();
            var requestRequestId = _fixture.Create<string>();
            var responseMock = new SubmitDataShareRequestResponse { DataShareRequestRequestId = requestRequestId, NotificationSuccess = NotificationSuccess.SentSuccessfully };

            _mockDataShareRequestService.Setup(s => s.SubmitDataShareRequest(requestId, CancellationToken.None))
                .ReturnsAsync(responseMock);

            // Act
            var result = await _controller.DeclareRequestSubmission(requestId, CancellationToken.None);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("DataShareRequestComplete"));
            Assert.That(redirectResult.RouteValues["requestRequestId"], Is.EqualTo(requestRequestId));
        }

        [Test]
        public async Task DeclareRequestSubmission_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = _fixture.Create<Guid>();
            var exception = new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 };

            _mockDataShareRequestService.Setup(s => s.SubmitDataShareRequest(requestId, CancellationToken.None))
                .ThrowsAsync(exception);

            //Act
            var result = await _controller.DeclareRequestSubmission(requestId, default);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }

        [Test]
        public async Task RequestDashboard_UserAuthenticated_ReturnsViewResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);

            // Act
            var result = await _controller.RequestDashboard();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task RequestDashboard_UserNotAuthenticated_RedirectsToIndex()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);

            // Act
            var result = await _controller.RequestDashboard();

            // Assert
            var redirectResult = result as RedirectToPageResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.PageName, Is.EqualTo("/Index"));
        }
        [Test]
        public async Task CreatedRequests_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var mockRequests = _fixture.Create<GetDataShareRequestSummariesResponse>();
            _mockDataShareRequestService.Setup(s => s.GetAcquirerDataShareRequests(It.IsAny<List<DataShareRequestStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRequests);

            // Act
            await _controller.CreatedRequests();

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for CreatedRequests."), Times.Once);
        }

        [Test]
        public async Task CreatedRequests_ValidRequest_ReturnsViewWithRequests()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var mockRequests = _fixture.Create<GetDataShareRequestSummariesResponse>(); 
            _mockDataShareRequestService.Setup(s => s.GetAcquirerDataShareRequests(It.IsAny<List<DataShareRequestStatus>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockRequests);

            // Act
            var result = await _controller.CreatedRequests();

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/CreatedRequests.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(mockRequests));
        }

        [Test]
        public async Task CreatedRequests_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _mockDataShareRequestService.Setup(s => s.GetAcquirerDataShareRequests(It.IsAny<List<DataShareRequestStatus>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.CreatedRequests();

            // Act & Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }

        [Test]
        public void CancelCreatedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var requestId = Guid.NewGuid();

            // Act
            _controller.CancelCreatedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for CancelCreatedRequest."), Times.Once);
        }

        [Test]
        public void CancelCreatedRequest_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();

            // Act
            var result = _controller.CancelCreatedRequest(requestId);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/CancelCreatedRequest.cshtml"));
            Assert.That(viewResult.Model, Is.InstanceOf<CancelCreatedRequestModel>());
            Assert.That(((CancelCreatedRequestModel)viewResult.Model).DataShareRequestId, Is.EqualTo(requestId));
        }


        [Test]
        public async Task ConfirmCancelCreatedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            _mockDataShareRequestService
            .Setup(s => s.CancelDataShareRequest(requestId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelDataShareRequestResponse { NotificationSuccess = NotificationSuccess.NotSent });

            // Act
            await _controller.ConfirmCancelCreatedRequest(requestId, formCollection);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for ConfirmCancelCreatedRequest."), Times.Once);
        }

        [Test]
        public async Task ConfirmCancelCreatedRequest_ValidRequest_RedirectsToCreatedRequests()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "cancellation-reasons", "Some reason" }
            });
            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);
            //_mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            _mockDataShareRequestService
                .Setup(s => s.CancelDataShareRequest(requestId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CancelDataShareRequestResponse { NotificationSuccess = NotificationSuccess.SentSuccessfully });

            // Act
            var result = await _controller.ConfirmCancelCreatedRequest(requestId, formCollection);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("CreatedRequests"));
        }

        [Test]
        public async Task ConfirmCancelCreatedRequest_ExceptionThrown_ThrowsException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            _mockDataShareRequestService
                .Setup(s => s.CancelDataShareRequest(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });
            // Act
            var result = await _controller.ConfirmCancelCreatedRequest(requestId, formCollection);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }

        [Test]
        public void DeleteCreatedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var requestId = Guid.NewGuid();

            // Act
            _controller.DeleteCreatedRequest(requestId, "Request123", "Esda Name");

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for DeleteCreatedRequest."), Times.Once);
        }

        [Test]
        public void DeleteCreatedRequest_ValidRequest_ReturnsCorrectView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var requestRequestId = "Request123";
            var esdaName = "Esda Name";

            // Act
            var result = _controller.DeleteCreatedRequest(requestId, requestRequestId, esdaName);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/DeleteCreatedRequest.cshtml"));

            var model = viewResult.Model as DeleteCreatedRequestModel;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.DataShareRequestId, Is.EqualTo(requestId));
            Assert.That(model.DataShareRequestRequestId, Is.EqualTo(requestRequestId));
            Assert.That(model.EsdaName, Is.EqualTo(esdaName));
        }
        [Test]
        public async Task ConfirmDeleteCreatedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var requestId = Guid.NewGuid();
            var deleteDatashareResponse = _fixture.Create<DeleteDataShareRequestResponse>();

            _mockDataShareRequestService
                .Setup(s => s.DeleteDataShareRequest(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteDatashareResponse);

            // Act
            await _controller.ConfirmDeleteCreatedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for ConfirmDeleteCreatedRequest."), Times.Once);
        }

        [Test]
        public async Task ConfirmDeleteCreatedRequest_ValidRequest_DeletesRequestAndRedirects()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var deleteDatashareResponse = _fixture.Create<DeleteDataShareRequestResponse>();

            _mockDataShareRequestService
                .Setup(s => s.DeleteDataShareRequest(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(deleteDatashareResponse);

            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.ConfirmDeleteCreatedRequest(requestId);

            // Assert
            _mockDataShareRequestService.Verify(s => s.DeleteDataShareRequest(requestId, It.IsAny<CancellationToken>()), Times.Once);
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("CreatedRequests"));
        }

        [Test]
        public async Task ConfirmDeleteCreatedRequest_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var exception = new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 };

            _mockDataShareRequestService
                .Setup(s => s.DeleteDataShareRequest(requestId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _controller.ConfirmDeleteCreatedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task CancelDeleteCreatedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            var requestId = Guid.NewGuid();

            // Act
            var result = await _controller.CancelDeleteCreatedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning("Model state is invalid for CancelDeleteCreatedRequest."), Times.Once);
        }

        [Test]
        public async Task CancelDeleteCreatedRequest_ValidRequest_RedirectsToRequestTasks()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();

            // Act
            var result = await _controller.CancelDeleteCreatedRequest(requestId);

            // Assert
            var redirectResult = result as RedirectToActionResult;
            Assert.That(redirectResult, Is.Not.Null);
            Assert.That(redirectResult.ActionName, Is.EqualTo("RequestTasks"));
            Assert.That(redirectResult.RouteValues["requestId"], Is.EqualTo(requestId));
        }
        #endregion

        #region Supplier Interface Tests
        [Test]
        public async Task ReceivedRequests_UserHasRole_ReturnsViewResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _mockUserRoleService.Setup(s => s.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(true);
            var response = _fixture.Create<GetSubmissionSummariesResponse>();
            _mockDataShareRequestService.Setup(s => s.GetSupplierDataShareRequests(default))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ReceivedRequests();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/ReceivedRequests.cshtml"));
        }

        [Test]
        public async Task ReceivedRequests_UserHasNoRole_RedirectsToErrorPage()
        {
            ClearInvocations();
            SetAuthenticatedUser(true);
            // Arrange
            _mockUserRoleService.Setup(s => s.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ReceivedRequests();

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Error/NoPermissions"));
            Assert.That(redirectResult.RouteValues["requiredPermission"], Is.EqualTo("datarequest"));
        }

        [Test]
        public async Task ReceivedRequests_ExceptionThrown_ReturnsErrorView()
        {
            ClearInvocations();
            SetAuthenticatedUser(true);
            // Arrange
            _mockUserRoleService.Setup(s => s.IsUserInRoleAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(true);
            _mockDataShareRequestService.Setup(s => s.GetSupplierDataShareRequests(default))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ReceivedRequests();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ReceivedRequests_InvalidModelState_LogsWarningAndReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            // Act
            var result = await _controller.ReceivedRequests();

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.TypeOf<ViewResult>());
        }
        [Test]
        public async Task ReviewNewReceivedRequest_ValidRequest_ReturnsViewResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var response = _fixture.Create<GetSubmissionInformationResponse>();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionInformation(requestId, default))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ReviewNewReceivedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/ReviewNewReceivedRequest.cshtml"));
        }

        [Test]
        public async Task ReviewNewReceivedRequest_InvalidModelState_LogsWarningAndReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));
            var response = _fixture.Create<GetSubmissionInformationResponse>();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionInformation(requestId, default))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ReviewNewReceivedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task ReviewNewReceivedRequest_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionInformation(requestId, default))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ReviewNewReceivedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ReviewInProgressReceivedRequest_ValidRequest_ReturnsViewResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var submissionResponse = _fixture.Create<GetSubmissionReviewInformationResponse>();
            var auditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService.Setup(s => s.GetSubmissionReviewInformation(requestId, default))
                .ReturnsAsync(submissionResponse);
            _mockDataShareRequestService.Setup(s => s.GetDataShareRequestReturnCommentsAuditLog(requestId, default))
                .ReturnsAsync(auditLogResponse);

            // Act
            var result = await _controller.ReviewInProgressReceivedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/ReviewInProgressReceivedRequest.cshtml"));
        }

        [Test]
        public async Task ReviewInProgressReceivedRequest_InvalidModelState_LogsWarningAndReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));
            var submissionResponse = _fixture.Create<GetSubmissionReviewInformationResponse>();
            var auditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService.Setup(s => s.GetSubmissionReviewInformation(requestId, default))
                .ReturnsAsync(submissionResponse);
            _mockDataShareRequestService.Setup(s => s.GetDataShareRequestReturnCommentsAuditLog(requestId, default))
                .ReturnsAsync(auditLogResponse);

            // Act
            var result = await _controller.ReviewInProgressReceivedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task ReviewInProgressReceivedRequest_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionReviewInformation(requestId, default))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ReviewInProgressReceivedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ViewReturnedRequest_ValidRequest_ReturnsViewResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var response = _fixture.Create<GetReturnedSubmissionInformationResponse>();
            _mockDataShareRequestService.Setup(s => s.GetReturnedSubmissionInformation(requestId, default))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ViewReturnedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/ViewReturnedRequest.cshtml"));
        }

        [Test]
        public async Task ViewReturnedRequest_InvalidModelState_LogsWarningAndReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            var response = _fixture.Create<GetReturnedSubmissionInformationResponse>();
            _mockDataShareRequestService.Setup(s => s.GetReturnedSubmissionInformation(requestId, default))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ViewReturnedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task ViewReturnedRequest_ExceptionThrown_ReturnsErrorView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.GetReturnedSubmissionInformation(requestId, default))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ViewReturnedRequest(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ViewCompletedSubmissionDetails_ValidRequest_ReturnsViewResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var submissionResponse = _fixture.Create<GetSubmissionDetailsResponse>();
            var auditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService.Setup(s => s.GetSubmissionDetails(requestId, default))
                .ReturnsAsync(submissionResponse);
            _mockDataShareRequestService.Setup(s => s.GetDataShareRequestReturnCommentsAuditLog(requestId, default))
                .ReturnsAsync(auditLogResponse);

            // Act
            var result = await _controller.ViewCompletedSubmissionDetails(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/CompletedSubmissionDetails.cshtml"));
        }

        [Test]
        public async Task ViewCompletedSubmissionDetails_InvalidModelState_LogsWarningAndReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            var submissionResponse = _fixture.Create<GetSubmissionDetailsResponse>();
            var auditLogResponse = _fixture.Create<GetDataShareRequestAuditLogResponse>();

            _mockDataShareRequestService.Setup(s => s.GetSubmissionDetails(requestId, default))
                .ReturnsAsync(submissionResponse);
            _mockDataShareRequestService.Setup(s => s.GetDataShareRequestReturnCommentsAuditLog(requestId, default))
                .ReturnsAsync(auditLogResponse);

            // Act
            var result = await _controller.ViewCompletedSubmissionDetails(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task ViewCompletedSubmissionDetails_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionDetails(requestId, default))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act 
            var result = await _controller.ViewCompletedSubmissionDetails(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }

        [Test]
        public async Task StartSubmissionReview_ValidRequest_RedirectsToReviewInProgress()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();
            var submissionReview = _fixture.Create<StartSubmissionReviewResponse>();

            _mockDataShareRequestService.Setup(s => s.StartSubmissionReview(requestId, cancellationToken))
                .ReturnsAsync(submissionReview);

            // Act
            var result = await _controller.StartSubmissionReview(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("ReviewInProgressReceivedRequest"));
            Assert.That(redirectResult.RouteValues["requestId"], Is.EqualTo(requestId));
        }

        [Test]
        public async Task StartSubmissionReview_InvalidModelState_LogsWarningAndReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            var submissionReview = _fixture.Create<StartSubmissionReviewResponse>();

            _mockDataShareRequestService.Setup(s => s.StartSubmissionReview(requestId, cancellationToken))
                .ReturnsAsync(submissionReview);

            // Act
            var result = await _controller.StartSubmissionReview(requestId, cancellationToken);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("ReviewInProgressReceivedRequest"));
            Assert.That(redirectResult.RouteValues["requestId"], Is.EqualTo(requestId));
        }

        [Test]
        public async Task StartSubmissionReview_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();
            _mockDataShareRequestService.Setup(s => s.StartSubmissionReview(requestId, cancellationToken))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.StartSubmissionReview(requestId, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task SetSubmissionNotesAndContinue_ValidRequest_CallsSetSubmissionNotes()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var cancellationToken = new CancellationToken();

            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            _mockDataShareRequestService.Setup(s => s.SetSubmissionNotes(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));


            // Act
            var result = await _controller.SetSubmissionNotesAndContinue(requestId, formCollection, cancellationToken);

            // Assert
            _mockDataShareRequestService.Verify(s => s.SetSubmissionNotes(requestId, It.IsAny<string>(), cancellationToken), Times.Once);
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        }

        [Test]
        public async Task SetSubmissionNotesAndContinue_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var cancellationToken = new CancellationToken();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            _mockDataShareRequestService.Setup(s => s.SetSubmissionNotes(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.SetSubmissionNotesAndContinue(requestId, formCollection, cancellationToken);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(2));
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());

        }

        [Test]
        public async Task SetSubmissionNotesAndContinue_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var cancellationToken = new CancellationToken();
            _mockDataShareRequestService.Setup(s => s.SetSubmissionNotes(requestId, It.IsAny<string>(), cancellationToken))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act 
            var result = await _controller.SetSubmissionNotesAndContinue(requestId, formCollection, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task SetSubmissionNotesAndReturn_ValidRequest_CallsSetSubmissionNotes()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var cancellationToken = new CancellationToken();

            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            _mockDataShareRequestService.Setup(s => s.SetSubmissionNotes(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));


            // Act
            var result = await _controller.SetSubmissionNotesAndReturn(requestId, formCollection, cancellationToken);

            // Assert
            _mockDataShareRequestService.Verify(s => s.SetSubmissionNotes(requestId, It.IsAny<string>(), cancellationToken), Times.Once);
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task SetSubmissionNotesAndReturn_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var cancellationToken = new CancellationToken();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            _mockDataShareRequestService.Setup(s => s.SetSubmissionNotes(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

            // Act
            var result = await _controller.SetSubmissionNotesAndReturn(requestId, formCollection, cancellationToken);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Exactly(3));
            Assert.That(result, Is.TypeOf<ViewResult>());

        }

        [Test]
        public async Task SetSubmissionNotesAndReturn_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var cancellationToken = new CancellationToken();
            _mockDataShareRequestService.Setup(s => s.SetSubmissionNotes(requestId, It.IsAny<string>(), cancellationToken))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act 
            var result = await _controller.SetSubmissionNotesAndReturn(requestId, formCollection, cancellationToken);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ShowSubmissionDecision_ValidRequest_ReturnsViewWithSubmissionInformation()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var submissionInformation = _fixture.Create<GetSubmissionInformationResponse>();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionInformation(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submissionInformation);

            // Act
            var result = await _controller.ShowSubmissionDecision(requestId);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/SubmissionDecision.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(submissionInformation.SubmissionInformation));
        }

        [Test]
        public async Task ShowSubmissionDecision_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));
            var submissionInformation = _fixture.Create<GetSubmissionInformationResponse>();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionInformation(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(submissionInformation);

            // Act
            await _controller.ShowSubmissionDecision(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task ShowSubmissionDecision_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.GetSubmissionInformation(requestId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ShowSubmissionDecision(requestId);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task AcceptReceivedRequest_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "acceptance-feedback-for-acquirer", "Test Feedback" }
            });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.AcceptReceivedRequest(requestId, formCollection, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/RequestAcceptanceDeclaration.cshtml"));
            Assert.That(viewResult.Model, Is.Not.Null);
        }

        [Test]
        public async Task AcceptReceivedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            await _controller.AcceptReceivedRequest(requestId, formCollection, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task AcceptReceivedRequest_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var formCollection = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            _mockUserRoleService.Setup(s => s.GetUserProfileAsync())
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.AcceptReceivedRequest(requestId, formCollection, default);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task DeclareRequestAcceptance_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var feedback = "Test Feedback";
            var expectedResponse = _fixture.Create<AcceptedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.AcceptReceivedRequest(requestId, feedback, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AcceptSubmissionResponse { AcceptedDecisionSummary = expectedResponse });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.DeclareRequestAcceptance(requestId, feedback, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/RequestAccepted.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task DeclareRequestAcceptance_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var feedback = "Test Feedback";
            _controller.ModelState.AddModelError("Error", "Invalid model state");
            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            var expectedResponse = _fixture.Create<AcceptedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.AcceptReceivedRequest(requestId, feedback, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AcceptSubmissionResponse { AcceptedDecisionSummary = expectedResponse });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            await _controller.DeclareRequestAcceptance(requestId, feedback, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task DeclareRequestAcceptance_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var feedback = "Test Feedback";
            _mockUserRoleService.Setup(s => s.GetUserProfileAsync())
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.DeclareRequestAcceptance(requestId, feedback, default);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }

        [Test]
        public async Task RejectReceivedRequest_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "rejection-feedback-for-acquirer", "Test Feedback" }
            });
            var expectedResponse = _fixture.Create<RejectedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.RejectReceivedRequest(requestId, "Test Feedback", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RejectSubmissionResponse { RejectedDecisionSummary = expectedResponse });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.RejectReceivedRequest(requestId, form, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/RequestRejected.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task RejectReceivedRequest_InvalidModelState_LogsWarningAndReturnsDefaultView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "rejection-feedback-for-acquirer", "Test Feedback" }
            });
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            var expectedResponse = _fixture.Create<RejectedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.RejectReceivedRequest(requestId, "Test Feedback", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RejectSubmissionResponse { RejectedDecisionSummary = expectedResponse });

            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            // Act
            var result = await _controller.RejectReceivedRequest(requestId, form, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task RejectReceivedRequest_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            _mockDataShareRequestService.Setup(s => s.RejectReceivedRequest(requestId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.RejectReceivedRequest(requestId, form, default);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ReturnReceivedRequest_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "return-feedback-for-acquirer", "Test Feedback" }
            });
            var expectedResponse = _fixture.Create<ReturnedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.ReturnReceivedRequest(requestId, "Test Feedback", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReturnSubmissionResponse { ReturnedDecisionSummary = expectedResponse });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.ReturnReceivedRequest(requestId, form, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/RequestReturned.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(expectedResponse));
        }
        [Test]
        public async Task ReturnReceivedRequest_NoFeedbackInFrom_ReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "test", "Test Feedback" }
            });
            var expectedResponse = _fixture.Create<ReturnedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.ReturnReceivedRequest(requestId, "Test Feedback", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReturnSubmissionResponse { ReturnedDecisionSummary = expectedResponse });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.ReturnReceivedRequest(requestId, form, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/SubmissionDecision.cshtml"));
        }
        [Test]
        public async Task ReturnReceivedRequest_RequestFeedbackInFrom_ReturnsView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "request-status", "Test Feedback" }
            });
            var expectedResponse = _fixture.Create<ReturnedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.ReturnReceivedRequest(requestId, "Test Feedback", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReturnSubmissionResponse { ReturnedDecisionSummary = expectedResponse });


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);


            // Act
            var result = await _controller.ReturnReceivedRequest(requestId, form, CancellationToken.None);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/SubmissionDecision.cshtml"));
        }

        [Test]
        public async Task ReturnReceivedRequest_InvalidModelState_LogsWarningAndReturnsDefaultView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "return-feedback-for-acquirer", "Test Feedback" }
            });
            var expectedResponse = _fixture.Create<ReturnedDecisionSummary>();

            _mockDataShareRequestService.Setup(s => s.ReturnReceivedRequest(requestId, "Test Feedback", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReturnSubmissionResponse { ReturnedDecisionSummary = expectedResponse });

            _controller.ModelState.AddModelError("Error", "Invalid model state");


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            // Act
            var result = await _controller.ReturnReceivedRequest(requestId, form, CancellationToken.None);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task ReturnReceivedRequest_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = Guid.NewGuid();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "return-feedback-for-acquirer", "Test Feedback" }
            });
            _mockDataShareRequestService.Setup(s => s.ReturnReceivedRequest(requestId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ReturnReceivedRequest(requestId, form, default);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task ViewCompletedReceivedRequest_ValidRequest_ReturnsViewWithModel()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var expectedResponse = _fixture.Create<CompletedSubmissionInformation>();

            _mockDataShareRequestService.Setup(s => s.GetCompletedReceivedRequest(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCompletedSubmissionInformationResponse { CompletedSubmissionInformation = expectedResponse });

            // Act
            var result = await _controller.ViewCompletedReceivedRequest(requestId);

            // Assert
            var viewResult = result as ViewResult;
            Assert.That(viewResult, Is.Not.Null);
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/DataRequest/ViewCompletedRequest.cshtml"));
            Assert.That(viewResult.Model, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task ViewCompletedReceivedRequest_InvalidModelState_LogsWarningAndReturnsDefaultView()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            var expectedResponse = _fixture.Create<CompletedSubmissionInformation>();

            _mockDataShareRequestService.Setup(s => s.GetCompletedReceivedRequest(requestId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCompletedSubmissionInformationResponse { CompletedSubmissionInformation = expectedResponse });

            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            // Act
            var result = await _controller.ViewCompletedReceivedRequest(requestId);

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.InstanceOf<ViewResult>());
        }

        [Test]
        public async Task ViewCompletedReceivedRequest_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.GetCompletedReceivedRequest(requestId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.ViewCompletedReceivedRequest(requestId);
            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml"));
        }
        [Test]
        public async Task DownloadCompletedRequest_ValidRequest_ReturnsFileResult()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            var requestRequestId = "TestRequest";
            var fileContent = new byte[] { 1, 2, 3, 4 };

            _mockDataShareRequestService.Setup(s => s.DownloadCompletedRequest(requestId, DataShareRequestFileFormat.Pdf, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileContent);


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Act
            var result = await _controller.DownloadCompletedRequest(requestId, requestRequestId);

            // Assert
            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(fileResult.FileDownloadName, Is.EqualTo("TestRequest.pdf"));
            Assert.That(fileResult.FileContents, Is.EqualTo(fileContent));
        }

        [Test]
        public async Task DownloadCompletedRequest_InvalidModelState_LogsWarning()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(false);
            var requestId = Guid.NewGuid();
            _controller.ModelState.AddModelError("Error", "Invalid model state");

            _mockLogger.Setup(l => l.LogWarning(It.IsAny<string>()));

            var fileContent = new byte[] { 1, 2, 3, 4 };

            _mockDataShareRequestService.Setup(s => s.DownloadCompletedRequest(requestId, DataShareRequestFileFormat.Pdf, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileContent);


            var userProfile = new UserProfile()
            {
                Domain = new UserDomain() { DomainId = 1, DomainName = "test", IsEnabled = true },
                EmailNotification = true,
                LastLogin = DateTime.Now,
                Organisation = new UserOrganisation() { IsEnabled = true, OrganisationId = 1, OrganisationName = "test" },
                Roles = _fixture.Create<List<Role>>(),
                User = new UserInfo() { UserEmail = "test", UserId = 1, UserName = "test" },
                WelcomeNotification = true,
            };

            _mockUserRoleService.Setup(x => x.GetUserProfileAsync()).ReturnsAsync(userProfile);

            // Act
            var result = await _controller.DownloadCompletedRequest(requestId, "TestRequest");

            // Assert
            _mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.Once);
            Assert.That(result, Is.InstanceOf<FileContentResult>()); 
        }

        [Test]
        public async Task DownloadCompletedRequest_ExceptionThrown_ThrowsDataShareRequestException()
        {
            // Arrange
            ClearInvocations();
            SetAuthenticatedUser(true);
            var requestId = Guid.NewGuid();
            _mockDataShareRequestService.Setup(s => s.DownloadCompletedRequest(requestId, DataShareRequestFileFormat.Pdf, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataShareRequestException() { DsrExceptionText = "test", DsrResponseText = "test", DsrStatusCode = 401 });

            // Act
            var result = await _controller.DownloadCompletedRequest(requestId, "test");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = result as ViewResult;
            Assert.That(viewResult.ViewName, Is.EqualTo("~/Pages/Error/401.cshtml")); ;
        }


        #endregion
    }
}
