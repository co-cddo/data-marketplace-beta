using Agm.Catalog.DotNet.Core.Utilities;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1.Enums;
using AutoFixture;
using AutoFixture.AutoMoq;
using Cddo.Data.Marketplace.Api.CustomErrors;
using Cddo.Data.Marketplace.Api.Validation;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Api.Test.Validation
{
    [TestFixture]
    public class ModelValidationServiceTests
    {
        private Mock<IAppInsightsLogger> _appsInsightMock;
        private Mock<IEnumMemberConverter> enumMemberConverter;
        private ModelValidationService modelValidationService;
        protected IFixture fixture;

        [SetUp]
        public void SetUp()
        {
            _appsInsightMock = new Mock<IAppInsightsLogger>();
            enumMemberConverter = new Mock<IEnumMemberConverter>();
            modelValidationService = new ModelValidationService(_appsInsightMock.Object, enumMemberConverter.Object);
            fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [Test]
        public void RecordModelStateErrorsAndBuildErrorResponse_WhenThereAreNoError_ReturnNull()
        {
            //Arrange
            var mockHttpContext = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            mockHttpContext.Setup(c => c.Request).Returns(requestMock.Object);
            mockHttpContext.Setup(c => c.Response).Returns(new DefaultHttpContext().Response);

            var routeData = new RouteData();
            routeData.Values["controller"] = "Home";
            routeData.Values["action"] = "Index";

            var actionDescriptor = new ActionDescriptor
            {
                
            };

            var actionContext = new ActionContext(
                mockHttpContext.Object,
                routeData,
                actionDescriptor
            );

            var initiatingUserDetails = fixture.Create<IUserDetails>();

            //Act
            var result = modelValidationService.RecordModelStateErrorsAndBuildErrorResponse(actionContext, initiatingUserDetails);

            result.Should().Be(null);

        }

        [Test]
        [TestCase("Title", "Missing title property")]
        [TestCase("Type", "Missing Type property")]
        [TestCase("Identifier", "Missing Identifier property")]
        [TestCase("AccessRights", "Missing AccessRights property")]
        [TestCase("ContactPoint.Name", "Missing ContactPoint name property")]
        [TestCase("ContactPoint.Email", "Missing ContactPoint email property")]
        [TestCase("Description", "Missing Description property")]
        [TestCase("Keyword", "Missing Keyword property")]
        [TestCase("Modified", "Missing Modified property")]
        [TestCase("Publisher", "Missing Publisher property")]
        [TestCase("SecurityClassification", "Missing SecurityClassification property")]
        [TestCase("Status", "Missing Status property")]
        [TestCase("Theme", "Missing Theme property")]
        [TestCase("Distribution.AccessService", "Missing AccessService property")]
        [TestCase("Distribution.DownloadUrl", "Missing DownloadUrl property")]
        [TestCase("Distribution.MediaType", "Missing MediaType property")]
        [TestCase("Issued", "Missing Issued property")]
        [TestCase("UpdateFrequency", "Missing UpdateFrequency property")]
        [TestCase("ApiType", "Missing ApiType property")]
        [TestCase("EndpointDescription", "Missing EndpointDescription property")]
        [TestCase("EndpointUrl", "Missing EndpointUrl property")]
        [TestCase("ServesDataset", "Missing ServesDataset property")]
        [TestCase("ServiceType", "Missing ServiceType property")]
        [TestCase("", "")]
        public void RecordModelStateErrorsAndBuildErrorResponse_WhenModelStateHasErrors_ErrorMessage(string key, string errorMessage)
        {
            //Arrange
            var mockHttpContext = new Mock<HttpContext>();
            var requestMock = new Mock<HttpRequest>();
            mockHttpContext.Setup(c => c.Request).Returns(requestMock.Object);
            mockHttpContext.Setup(c => c.Response).Returns(new DefaultHttpContext().Response);

            var routeData = new RouteData();
            routeData.Values["controller"] = "Home";
            routeData.Values["action"] = "Index";

            var actionDescriptor = new ActionDescriptor
            {

            };

            var actionContext = new ActionContext(
                mockHttpContext.Object,
                routeData,
                actionDescriptor
            );

            actionContext.ModelState.AddModelError(key, errorMessage);

            var initiatingUserDetails = fixture.Create<IUserDetails>();

            //Act
            var result = modelValidationService.RecordModelStateErrorsAndBuildErrorResponse(actionContext, initiatingUserDetails);

            result.Message.Should().Be("Validation failures");

        }


        [Test]
        public void RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid_WhenThereAreNoError_ReturnNull()
        {
            //Arrange
            var validationPropertyResults = fixture.Create<IEnumerable<IDataAssetValidationPropertyResult>>();
            var initiatingUserDetails = fixture.Create<IUserDetails>();

            //Act
            var result = modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResults, initiatingUserDetails);

            result.Should().Be(null);

        }

        [Test]
        public void RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid_WhenValidationErrorsExists_ReturnErrorMessage()
        {
            //Arrange
            var validationPropertyResults = fixture.Create<IEnumerable<IDataAssetValidationPropertyResult>>();
            var mockedErrors = fixture.Create<IEnumerable<DataAssetValidationPropertyError>>();
            var validationPropertyResult = new Mock<IDataAssetValidationPropertyResult>();
            validationPropertyResult.Setup(p=>p.PropertyName).Returns("Title");
            validationPropertyResult.Setup(p=>p.Errors).Returns(mockedErrors);

            var validationPropertyResultsList = validationPropertyResults.ToList();

            validationPropertyResultsList[0] = validationPropertyResult.Object;

            var initiatingUserDetails = fixture.Create<IUserDetails>();

            //Act
            var result = modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResultsList, initiatingUserDetails);

            result.Message.Should().Be("Validation failures");

        }

        [Test]
        public void HandleSimulatedErrors_WhenThereAreNoError_ReturnNull()
        {
            //Arrange
            var validationPropertyResults = fixture.Create<IEnumerable<IDataAssetValidationPropertyResult>>();
            var initiatingUserDetails = fixture.Create<IUserDetails>();

            //Act
            var result = modelValidationService.RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(validationPropertyResults, initiatingUserDetails);

            result.Should().Be(null);

        }
        [Test]
        [TestCase("trigger-404", 404)]
        [TestCase("trigger-500", 500)]
        public void HandleSimulatedErrors_TriggerErrorsForDataset_ReturnsRelavantStatusCodeAndError(string datasetId, int statusCode)
        {
            //Arrange
            //Act
            var result = modelValidationService.HandleSimulatedErrors(null, datasetId, true);

            result.Value.Item1.Should().Be(statusCode);
        }

        [Test]
        [TestCase("trigger-400", 400)]
        [TestCase("trigger-409", 409)]
        public void HandleSimulatedErrors_TriggerErrorsForDataService_ReturnsRelavantStatusCodeAndError(string datasetId, int statusCode)
        {
            //Arrange
            var dataset = fixture.Create<CataloguedResource>();
            dataset.Title = datasetId;

            //Act
            var result = modelValidationService.HandleSimulatedErrors(dataset, null, false);

            result.Value.Item1.Should().Be(statusCode);
        }
        [Test]
        [TestCase("trigger-409", 409)]
        public void HandleSimulatedErrors_TriggerErrorsForDataServiceSupplierIdentifier_ReturnsRelavantStatusCodeAndError(string datasetId, int statusCode)
        {
            //Arrange
            var dataset = fixture.Create<CataloguedResource>();
            dataset.SupplierIdentifier = datasetId;

            //Act
            var result = modelValidationService.HandleSimulatedErrors(dataset, null, false);

            result.Value.Item1.Should().Be(statusCode);
        }

        [Test]
        [TestCase("trigger-500", 500)]
        public void HandleSimulatedErrors_TriggerErrorsForDataServiceSupplierIdentifierTitle_ReturnsRelavantStatusCodeAndError(string datasetId, int statusCode)
        {
            //Arrange
            var dataset = fixture.Create<CataloguedResource>();
            dataset.Title = datasetId;

            //Act
            var result = modelValidationService.HandleSimulatedErrors(dataset, null, false);

            result.Value.Item1.Should().Be(statusCode);
        }
        [Test]
        [TestCase("trigger-500", 500)]
        public void HandleSimulatedErrors_TriggerErrorsForDataServiceSupplierIdentifierIdentifier_ReturnsRelavantStatusCodeAndError(string datasetId, int statusCode)
        {
            //Arrange
            var dataset = fixture.Create<CataloguedResource>();
            dataset.Identifier = datasetId;

            //Act
            var result = modelValidationService.HandleSimulatedErrors(dataset, "datasetId", false);

            result.Value.Item1.Should().Be(statusCode);
        }

        [Test]
        [TestCase("datasetId", null)]
        public void HandleSimulatedErrors_TriggerErrorsForDataServiceWhenThereAreNoErrors_ReturnsRelavantStatusCodeAndError(string datasetId, int statusCode)
        {
            //Arrange
            var dataset = fixture.Create<CataloguedResource>();
            dataset.Identifier = datasetId;

            //Act
            var result = modelValidationService.HandleSimulatedErrors(dataset, "datasetId", false);

            result.Should().Be(null);
        }

        [Test]
        public void GetMockedCataloguedResources_ReturnMockedCatalogue()
        {
            var expected = new List<CataloguedResource>
                {
                    new()
                    {
                        Type = ResourceEnum.DataSet,
                        Identifier = Guid.NewGuid().ToString(),
                        Title = "Mocked Dataset Title",
                        AccessRights = AccessRightsEnum.Open,
                        ContactPoint = new List<Contact>
                        {
                            new() { Name = "John Doe", Email = "contact@example.com", Role = ContactRoleEnum.Owner }
                        },
                        Description = "This is a mocked dataset description for sandbox.",
                        Keyword = new List<string> { "mock", "dataset", "Sandbox" },
                        Modified = DateTime.UtcNow.AddDays(-1),
                        Publisher = "Mock Publisher",
                        SecurityClassification = SecurityClassificationEnum.Official,
                        Status = ResourceStatusEnum.Published,
                        SupplierIdentifier = "supplier-12345",
                        Theme = new List<ThemeEnum> { ThemeEnum.Education, ThemeEnum.ScienceAndTechnology }
                    },
                    new()
                    {
                        Type = ResourceEnum.DataService,
                        Identifier = Guid.NewGuid().ToString(),
                        Title = "Mocked Data Service Title",
                        AccessRights = AccessRightsEnum.Internal,
                        ContactPoint = new List<Contact>
                        {
                            new() { Name = "Jane Smith", Email = "servicecontact@example.com", Role = ContactRoleEnum.Contact }
                        },
                        Description = "This is a mocked data service description for sandbox.",
                        Keyword = new List<string> { "mock", "data-service", "Sandbox" },
                        Modified = DateTime.UtcNow.AddDays(-3),
                        Publisher = "Another Mock Publisher",
                        SecurityClassification = SecurityClassificationEnum.Secret,
                        Status = ResourceStatusEnum.Draft,
                        SupplierIdentifier = "supplier-67890",
                        Theme = new List<ThemeEnum> { ThemeEnum.HealthAndCare, ThemeEnum.TransportAndInfrastructure }
                    }
                };

            var result = modelValidationService.GetMockedCataloguedResources();

            result[0].Type.Should().Be(expected[0].Type);
            result[0].Title.Should().Be(expected[0].Title);
            result[0].AccessRights.Should().Be(expected[0].AccessRights);
            result[0].ContactPoint[0].Name.Should().Be(expected[0].ContactPoint[0].Name);
            result[0].Description.Should().Be(expected[0].Description);
            result[0].Keyword.Count().Should().Be(expected[0].Keyword.Count());
            result[0].Publisher.Should().Be(expected[0].Publisher);
            result[0].SecurityClassification.Should().Be(expected[0].SecurityClassification);
            result[0].Status.Should().Be(expected[0].Status);
            result[0].SupplierIdentifier.Should().Be(expected[0].SupplierIdentifier);
            result[0].Theme.Count().Should().Be(expected[0].Theme.Count());
        }

        [Test]
        public void GetMockedCataloguedResources()
        { 
            var datasetId = Guid.NewGuid().ToString();
            var expected = new DataSet
            {
                Type = ResourceEnum.DataSet,
                Identifier = datasetId,
                Title = "Mocked Dataset Title",
                AccessRights = AccessRightsEnum.Open,
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mock Dataset Owner", Email = "owner@example.com", Role = ContactRoleEnum.Owner }
                    },
                Description = "This is a mocked dataset description tailored for the sandbox environment.",
                Keyword = new List<string> { "Sandbox", "mocked", "dataset" },
                Modified = DateTime.UtcNow.AddDays(-7),
                Publisher = "Mock Dataset Publisher",
                SecurityClassification = SecurityClassificationEnum.Official,
                Status = ResourceStatusEnum.Published,
                SupplierIdentifier = "supplier-12345-dataset",
                Theme = new List<ThemeEnum> { ThemeEnum.BusinessEconomicsAndFinance, ThemeEnum.EnvironmentAndNature },
                Issued = DateTime.Now,
                Distribution = new List<Distribution>
                    {
                        new()
                        {
                            AccessService = ["17554d2c-7251-4822-8813-872effcc5650"],
                            DownloadUrl = "https://testing.com/api",
                            MediaType = ["application/xml"]
                        }
                    },
                UpdateFrequency = "Yearly"
            };

            var result = modelValidationService.GetMockedDataset(datasetId);

            result.Type.Should().Be(expected.Type);
            result.Title.Should().Be(expected.Title);
            result.AccessRights.Should().Be(expected.AccessRights);
            result.ContactPoint[0].Name.Should().Be(expected.ContactPoint[0].Name);
            result.Description.Should().Be(expected.Description);
            result.Keyword.Count().Should().Be(expected.Keyword.Count());
            result.Publisher.Should().Be(expected.Publisher);
            result.SecurityClassification.Should().Be(expected.SecurityClassification);
            result.Status.Should().Be(expected.Status);
            result.SupplierIdentifier.Should().Be(expected.SupplierIdentifier);
            result.Theme.Count().Should().Be(expected.Theme.Count());
            result.UpdateFrequency.Should().Be(expected.UpdateFrequency);
        }

        [Test]
        public void GetUpdatedMockedCataloguedResources()
        {
            var datasetId = Guid.NewGuid().ToString();

            var patchModel = fixture.Create<DataSet>();
            var expected = new DataSet
            {
                Identifier = datasetId,
                Title = patchModel.Title ?? "Mocked Updated Dataset",
                Description = patchModel.Description ?? "This is a mocked description for the sandbox environment.",
                Status = patchModel.Status ?? ResourceStatusEnum.Published,
                SupplierIdentifier = patchModel.SupplierIdentifier ?? "mocked-supplier-123",
                Modified = DateTime.Now,
                Keyword = new List<string> { "Sandbox", "mock", "updated" },
                Publisher = patchModel.Publisher ?? "Mocked Publisher",
                SecurityClassification = SecurityClassificationEnum.Official,
                Type = ResourceEnum.DataSet,
                Theme = new List<ThemeEnum> { ThemeEnum.BusinessEconomicsAndFinance, ThemeEnum.EnvironmentAndNature },
                Issued = DateTime.Now,
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mock Dataset Owner", Email = "owner@example.com", Role = ContactRoleEnum.Owner }
                    },
                Distribution = new List<Distribution>
                    {
                        new()
                        {
                            AccessService = ["17554d2c-7251-4822-8813-872effcc5650"],
                            DownloadUrl = "https://testing.com/api",
                            MediaType = ["application/xml"],
                        }
                    },
                AccessRights = AccessRightsEnum.Internal,
                UpdateFrequency = "Yearly"
            };

            var result = modelValidationService.GetMockedUpdatedDataset(datasetId, expected);

            result.Type.Should().Be(expected.Type);
            result.Title.Should().Be(expected.Title);
            result.AccessRights.Should().Be(expected.AccessRights);
            result.ContactPoint[0].Name.Should().Be(expected.ContactPoint[0].Name);
            result.Description.Should().Be(expected.Description);
            result.Keyword.Count().Should().Be(expected.Keyword.Count());
            result.Publisher.Should().Be(expected.Publisher);
            result.SecurityClassification.Should().Be(expected.SecurityClassification);
            result.Status.Should().Be(expected.Status);
            result.SupplierIdentifier.Should().Be(expected.SupplierIdentifier);
            result.Theme.Count().Should().Be(expected.Theme.Count());
            result.UpdateFrequency.Should().Be(expected.UpdateFrequency);
        }

        [Test]
        public void GetMockedDataServive_ReturnsGetMockedDataServive()
        {
            var dataServiceId = Guid.NewGuid().ToString();
            var expected = new DataService
            {
                Identifier = dataServiceId,
                Title = "Mocked Data Service",
                Description = "This is a mocked description for the sandbox environment.",
                Keyword = new List<string> { "Sandbox", "mock", "data-service" },
                Publisher = "Mocked Publisher",
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mocked Contact", Email = "mocked.contact@example.com", Role = ContactRoleEnum.Owner }
                    },
                AccessRights = AccessRightsEnum.Open,
                Status = ResourceStatusEnum.Published,
                Modified = DateTime.Now,
                Type = ResourceEnum.DataService,
                Theme = new List<ThemeEnum> { ThemeEnum.AgricultureFisheriesAndForestry, ThemeEnum.BusinessEconomicsAndFinance },
                SecurityClassification = SecurityClassificationEnum.Official,
                EndpointDescription = "Endpoint Description",
                EndpointUrl = "https://testingurl.com",
                ServesDataset = new List<string> { "1da70e32-2762-465e-b00a-28664af24264" },
                ApiType = ApiTypeEnum.Rest,
                ServiceType = ServiceTypeEnum.Transactional
            };

            var result = modelValidationService.GetMockedDataServive(dataServiceId);

            result.Type.Should().Be(expected.Type);
            result.Title.Should().Be(expected.Title);
            result.AccessRights.Should().Be(expected.AccessRights);
            result.ContactPoint[0].Name.Should().Be(expected.ContactPoint[0].Name);
            result.Description.Should().Be(expected.Description);
            result.Keyword.Count().Should().Be(expected.Keyword.Count());
            result.Publisher.Should().Be(expected.Publisher);
            result.SecurityClassification.Should().Be(expected.SecurityClassification);
            result.Status.Should().Be(expected.Status);
            result.SupplierIdentifier.Should().Be(expected.SupplierIdentifier);
            result.Theme.Count().Should().Be(expected.Theme.Count());
            result.EndpointDescription.Should().Be(expected.EndpointDescription);
            result.EndpointUrl.Should().Be(expected.EndpointUrl);
            result.ApiType.Should().Be(expected.ApiType);
            result.ServiceType.Should().Be(expected.ServiceType);
            result.ServesDataset.Count().Should().Be(expected.ServesDataset.Count());
        }

        [Test]
        public void GetUpdatedMockedDataServive_ReturnsGetMockedDataServive()
        {
            var dataServiceId = Guid.NewGuid().ToString();
            var expected = new DataService
            {
                Identifier = dataServiceId,
                Title = "Mocked Data Service",
                Description = "This is a mocked description for the sandbox environment.",
                Keyword = new List<string> { "Sandbox", "mock", "data-service" },
                Publisher = "Mocked Publisher",
                ContactPoint = new List<Contact>
                    {
                        new() { Name = "Mocked Contact", Email = "mocked.contact@example.com", Role = ContactRoleEnum.Owner }
                    },
                AccessRights = AccessRightsEnum.Open,
                Status = ResourceStatusEnum.Published,
                Modified = DateTime.UtcNow,
                Type = ResourceEnum.DataService,
                Theme = new List<ThemeEnum> { ThemeEnum.AgricultureFisheriesAndForestry, ThemeEnum.BusinessEconomicsAndFinance },
                SecurityClassification = SecurityClassificationEnum.Official,
                EndpointDescription = "Endpoint Description",
                EndpointUrl = "https://testingurl.com",
                ServesDataset = new List<string> { "1da70e32-2762-465e-b00a-28664af24264" },
                ApiType = ApiTypeEnum.Rest,
                ServiceType = ServiceTypeEnum.Transactional
            };

            var result = modelValidationService.GetMockedUpdatedDataService(dataServiceId);

            result.Type.Should().Be(expected.Type);
            result.Title.Should().Be(expected.Title);
            result.AccessRights.Should().Be(expected.AccessRights);
            result.ContactPoint[0].Name.Should().Be(expected.ContactPoint[0].Name);
            result.Description.Should().Be(expected.Description);
            result.Keyword.Count().Should().Be(expected.Keyword.Count());
            result.Publisher.Should().Be(expected.Publisher);
            result.SecurityClassification.Should().Be(expected.SecurityClassification);
            result.Status.Should().Be(expected.Status);
            result.SupplierIdentifier.Should().Be(expected.SupplierIdentifier);
            result.Theme.Count().Should().Be(expected.Theme.Count());
            result.EndpointDescription.Should().Be(expected.EndpointDescription);
            result.EndpointUrl.Should().Be(expected.EndpointUrl);
            result.ApiType.Should().Be(expected.ApiType);
            result.ServiceType.Should().Be(expected.ServiceType);
            result.ServesDataset.Count().Should().Be(expected.ServesDataset.Count());
        }
    }
}
