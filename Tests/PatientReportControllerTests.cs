using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Controllers;
using ClinicManager.DAL;
using ClinicManager.Models;
using System.IO;
using System.Threading.Tasks;

namespace ClinicManager.Tests
{
    public class PatientReportControllerTests
    {
        [Fact]
        public async Task Analyze_ReturnsBadRequest_WhenRequestIsEmpty()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseInMemoryDatabase(databaseName: "Analyze_ReturnsBadRequest_WhenRequestIsEmpty")
                .Options;

            using var context = new ClinicDbContext(options);
            var mockLogger = new Mock<ILogger<PatientReportController>>();
            var controller = new PatientReportController(context, mockLogger.Object);

            // Act
            var result = await controller.Analyze(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Analyze_ReturnsNotFound_WhenFileDoesNotExist()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseInMemoryDatabase(databaseName: "Analyze_ReturnsNotFound_WhenFileDoesNotExist")
                .Options;

            using var context = new ClinicDbContext(options);
            var mockLogger = new Mock<ILogger<PatientReportController>>();
            var controller = new PatientReportController(context, mockLogger.Object);

            var request = new AnalyzeReportRequest { FilePath = "nonexistent_file.pdf" };

            // Act
            var result = await controller.Analyze(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Analyze_ReturnsSimulatedResponse_WhenKeyIsDefaultOrMissing()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseInMemoryDatabase(databaseName: "Analyze_ReturnsSimulatedResponse_WhenKeyIsDefaultOrMissing")
                .Options;

            using var context = new ClinicDbContext(options);
            var mockLogger = new Mock<ILogger<PatientReportController>>();
            var controller = new PatientReportController(context, mockLogger.Object);

            // Create a temporary file to test file checking
            var tempFile = Path.GetTempFileName();
            try
            {
                var request = new AnalyzeReportRequest { FilePath = tempFile };

                // Act
                var result = await controller.Analyze(request);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var response = Assert.IsType<AnalyzeReportResponse>(okResult.Value);
                Assert.Contains("Simulated", response.ReportName);
                Assert.Equal("Dr. Automated Simulator", response.DoctorName);
                Assert.Contains("ChatGPT API key is not configured", response.ReportDetails);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
