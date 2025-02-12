using Agm.Catalog.DotNet.Core.Validation.EmailAddress;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Moq;
using NUnit.Framework;

namespace Cddo.Data.Marketplace.Logic.Test.Services.Users
{
    [TestFixture]
    public class DataShareRequestMailboxAddressValidationTests
    {
        #region TryValidateDataShareRequestMailboxAddress() Tests
        #region Empty Data Share Request Mailbox Address
        [Test]
        [TestCaseSource(nameof(EmptyDataShareRequestMailboxAddressesTestCaseData))]
        public void GivenAnEmptyDataShareRequestMailboxAddress_WhenITryValidateDataShareRequestMailboxAddress_ThenFalseIsReturned(
            string? testDataShareRequestMailboxAddress)
        {
            var testItems = CreateTestItems();

            var result = testItems.DataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                testDataShareRequestMailboxAddress!,
                out _);

            Assert.That(result, Is.False);
        }

        [Test]
        [TestCaseSource(nameof(EmptyDataShareRequestMailboxAddressesTestCaseData))]
        public void GivenAnEmptyDataShareRequestMailboxAddress_WhenITryValidateDataShareRequestMailboxAddress_ThenAValidationErrorIsProvided(
            string? testDataShareRequestMailboxAddress)
        {
            var testItems = CreateTestItems();

            testItems.DataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                testDataShareRequestMailboxAddress!,
                out var validationError);

            Assert.That(validationError, Is.EqualTo("Enter a valid email address"));
        }

        private static IEnumerable<TestCaseData> EmptyDataShareRequestMailboxAddressesTestCaseData()
        {
            yield return new TestCaseData(null);
            yield return new TestCaseData("");
            yield return new TestCaseData("  ");
        }
        #endregion

        #region Invalid Format Data Share Request Mailbox Address
        [Test]
        public void GivenADataShareRequestMailboxAddressWithInvalidFormat_WhenITryValidateDataShareRequestMailboxAddress_ThenFalseIsReturned()
        {
            var testItems = CreateTestItems();

            testItems.MockCddoEmailAddressValidation.Setup(x => x.IsEmailAddressValid(It.IsAny<string>()))
                .Returns(false);

            var result = testItems.DataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                "abc", out _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GivenADataShareRequestMailboxAddressWithInvalidFormat_WhenITryValidateDataShareRequestMailboxAddress_ThenAValidationErrorIsProvided()
        {
            var testItems = CreateTestItems();

            testItems.MockCddoEmailAddressValidation.Setup(x => x.IsEmailAddressValid(It.IsAny<string>()))
                .Returns(() => false);

            testItems.DataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                "abc", out var validationError);

            Assert.That(validationError, Is.EqualTo("Enter a valid email address"));
        }

        [Test]
        public void GivenADataShareRequestMailboxAddressWithValidFormat_WhenITryValidateDataShareRequestMailboxAddress_ThenNoValidationErrorIsProvided()
        {
            var testItems = CreateTestItems();

            testItems.DataShareRequestMailboxAddressValidation.TryValidateDataShareRequestMailboxAddress(
                "abc", out var validationError);

            Assert.That(validationError, Is.EqualTo(null));
        }
        #endregion
        #endregion

        #region Test Item Creation
        private static TestItems CreateTestItems()
        {
            var mockCddoEmailAddressValidation = new Mock<ICddoEmailAddressValidation>();

            ConfigureHappyPathTesting();
            
            var dataShareRequestMailboxAddressValidation = new DataShareRequestMailboxAddressValidation(
                mockCddoEmailAddressValidation.Object);

            return new TestItems(
                dataShareRequestMailboxAddressValidation,
                mockCddoEmailAddressValidation);

            void ConfigureHappyPathTesting()
            {
                mockCddoEmailAddressValidation.Setup(x => x.IsEmailAddressValid(It.IsAny<string>()))
                    .Returns(true);
            }
        }

        private class TestItems(
            IDataShareRequestMailboxAddressValidation dataShareRequestMailboxAddressValidation,
            Mock<ICddoEmailAddressValidation> mockCddoEmailAddressValidation)
        {
            public IDataShareRequestMailboxAddressValidation DataShareRequestMailboxAddressValidation { get; } = dataShareRequestMailboxAddressValidation;
            public Mock<ICddoEmailAddressValidation> MockCddoEmailAddressValidation { get; } = mockCddoEmailAddressValidation;
        }
        #endregion
    }
}
