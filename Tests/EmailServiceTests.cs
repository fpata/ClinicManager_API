using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ClinicManager.DAL;
using ClinicManager.Services;
using System.Reflection;

namespace ClinicManager.Tests
{
    public class EmailServiceTests
    {
        [Theory]
        [InlineData("ngdmexihnijtlccn", "ngdmexihnijtlccn")] // Plain-text Gmail App Password (should NOT decode, false positive check catches it)
        [InlineData("bmdkbWV4aWhuaWp0bGNjbg", "ngdmexihnijtlccn")] // Unpadded base64 encoded "ngdmexihnijtlccn"
        [InlineData("bmdkbWV4aWhuaWp0bGNjbg==", "ngdmexihnijtlccn")] // Padded base64 encoded "ngdmexihnijtlccn"
        [InlineData("SGVsbG8gV29ybGQ=", "Hello World")] // True Base64 string for "Hello World"
        [InlineData("plain_text_password", "plain_text_password")] // Regular plain text password
        [InlineData("c2VjcmV0X3Bhc3N3b3Jk", "secret_password")] // True Base64 string for "secret_password"
        [InlineData("", "")]
        [InlineData(null, "")]
        public void DecodeBase64Key_HandlesValuesCorrectly(string? input, string expected)
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseInMemoryDatabase(databaseName: "DecodeBase64Key_Test")
                .Options;

            using var context = new ClinicDbContext(options);
            var mockConfig = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<EmailService>>();
            var service = new EmailService(context, mockConfig.Object, mockLogger.Object);

            var methodInfo = typeof(EmailService).GetMethod("DecodeBase64Key", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(methodInfo);

            // Act
            var result = methodInfo.Invoke(service, new object[] { input! }) as string;

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
