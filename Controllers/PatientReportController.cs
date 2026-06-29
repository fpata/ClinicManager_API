using ClinicManager.DAL;
using ClinicManager.Models;
using ClinicManager;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientReportController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientReportController> _logger;
        private readonly IConfiguration _configuration;

        public PatientReportController(ClinicDbContext context, ILogger<PatientReportController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private bool IsAuthorizedForPatient(int? patientUserId)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            if (roleClaim == "Administrator" || roleClaim == "Doctor" || roleClaim == "Nurse" || roleClaim == "Accountant")
            {
                return true;
            }

            var userIdClaim = User.FindFirst("userid")?.Value;
            if (roleClaim == "Patient" && userIdClaim != null && patientUserId != null && userIdClaim == patientUserId.ToString())
            {
                return true;
            }

            return false;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PatientReport>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching patient reports page {pageNumber} with size {pageSize}");
            
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            var userIdClaim = User.FindFirst("userid")?.Value;
            
            var query = _context.PatientReports.AsNoTracking();

            if (roleClaim == "Patient")
            {
                if (int.TryParse(userIdClaim, out int loggedInUserId))
                {
                    query = query.Where(r => r.UserID == loggedInUserId);
                }
                else
                {
                    return Forbid();
                }
            }

            var reports = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return reports;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientReport>> Get(int id)
        {
            _logger.LogInformation($"Fetching patient report with ID: {id}");
            var entity = await _context.PatientReports.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient report with ID: {id} not found");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<PatientReport>> Post(PatientReport report)
        {
            if (!IsAuthorizedForPatient(report.UserID))
            {
                return Forbid();
            }

            _context.PatientReports.Add(report);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Created new patient report with ID: {report.ID}");
            return CreatedAtAction(nameof(Get), new { id = report.ID }, report);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, PatientReport report)
        {
            if (id != report.ID)
            {
                _logger.LogWarning($"Patient report ID mismatch: {id} != {report.ID}");
                return BadRequest();
            }

            if (!IsAuthorizedForPatient(report.UserID))
            {
                return Forbid();
            }

            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated patient report with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<PatientReport> patchDoc)
        {
            var entity = await _context.PatientReports.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient report with ID: {id} not found for patch");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            patchDoc.ApplyTo(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Patched patient report with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            if (roleClaim == "Patient")
            {
                return Forbid();
            }

            var entity = await _context.PatientReports.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient report with ID: {id} not found for deletion");
                return NotFound();
            }

            _context.PatientReports.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted patient report with ID: {id}");
            return NoContent();
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadReport(string filePath)
        {
            // Simple validation, file path must exist
            if(string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                _logger.LogWarning($"File not found: {filePath}");
                return NotFound();
            }
            return File(System.IO.File.ReadAllBytes(filePath), "application/octet-stream", Path.GetFileName(filePath));
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        [HttpPost("analyze")]
        public async Task<ActionResult<AnalyzeReportResponse>> Analyze([FromBody] AnalyzeReportRequest request)
        {
            _logger.LogInformation($"Analyzing report file: {request?.FilePath}");

            if (request == null || string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest("File path is required.");
            }

            // Simple validation, file path must exist
            if (!System.IO.File.Exists(request.FilePath))
            {
                _logger.LogWarning($"File not found for analysis: {request.FilePath}");
                return NotFound("Report file not found on disk.");
            }

            var rawApiKey = _configuration["ChatGPTApiKey"] ?? "YOUR_OPENAI_API_KEY";
            var apiKey = DecodeBase64Key(rawApiKey);
            var model = _configuration["ChatGPTModel"] ?? "gpt-4o-mini";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_OPENAI_API_KEY")
            {
                _logger.LogWarning("ChatGPT API key is not configured. Returning simulated mock report analysis.");
                return Ok(new AnalyzeReportResponse
                {
                    ReportName = "Simulated " + Path.GetFileNameWithoutExtension(request.FilePath) + " Analysis",
                    DoctorName = "Dr. Automated Simulator",
                    ReportDetails = "IMPORTANT: ChatGPT API key is not configured in app.config. " +
                                    "This is a high-fidelity simulated response for testing UI integration.\n\n" +
                                    "Findings Summary:\n" +
                                    "- Patient shows excellent oral hygiene overall.\n" +
                                    "- Mild calculus accumulation noted in the lower anterior sextant.\n" +
                                    "- No active caries or pathological radiographic findings identified.\n" +
                                    "Recommendation: Schedule standard prophylaxis (scaling & polishing) in 6 months."
                });
            }

            try
            {
                string extension = Path.GetExtension(request.FilePath)?.ToLowerInvariant() ?? "";
                bool isImage = extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".webp" || extension == ".gif";
                bool isPdf = extension == ".pdf";

                object messageContent;

                if (isImage)
                {
                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(request.FilePath);
                    string base64Image = Convert.ToBase64String(fileBytes);
                    string mimeType = extension switch
                    {
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        ".gif" => "image/gif",
                        _ => "image/jpeg"
                    };

                    messageContent = new object[]
                    {
                        new { type = "text", text = GetPrompt() },
                        new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Image}" } }
                    };
                }
                else if (isPdf)
                {
                    string extractedText = ExtractTextFromPdf(request.FilePath);
                    messageContent = $"{GetPrompt()}\n\nHere is the text extracted from the report PDF:\n{extractedText}";
                }
                else
                {
                    // Assume text-based file
                    string textContent = await System.IO.File.ReadAllTextAsync(request.FilePath);
                    messageContent = $"{GetPrompt()}\n\nHere is the text of the report:\n{textContent}";
                }

                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = messageContent }
                    },
                    response_format = new { type = "json_object" }
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"OpenAI API call failed: {response.StatusCode} - {errorContent}");
                    return StatusCode((int)response.StatusCode, $"OpenAI API call failed: {errorContent}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var contentString = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(contentString))
                {
                    return StatusCode(500, "Received empty response content from OpenAI.");
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var analysis = JsonSerializer.Deserialize<AnalyzeReportResponse>(contentString, options);

                if (analysis == null)
                {
                    return StatusCode(500, "Failed to deserialize JSON response from OpenAI.");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while analyzing the report file.");
                return StatusCode(500, $"An error occurred during AI analysis: {ex.Message}");
            }
        }

        private string GetPrompt()
        {
            return "You are an AI assistant in a dental and general medical clinic. " +
                   "Analyze the attached medical/dental report. " +
                   "Extract the following three fields and return them in a JSON object with the exact keys: " +
                   "\"reportName\" (a concise name for the report, e.g., 'Dental X-Ray', 'Routine Scaling Findings'), " +
                   "\"doctorName\" (the name of the doctor who created/signed the report, if any, or empty string), " +
                   "\"reportDetails\" (a structured summary of findings, diagnoses, teeth numbers affected, and recommended treatments). " +
                   "Do not include any extra text outside the JSON object.";
        }

        private string ExtractTextFromPdf(string filePath)
        {
            try
            {
                using (var pdf = UglyToad.PdfPig.PdfDocument.Open(filePath))
                {
                    var text = new StringBuilder();
                    foreach (var page in pdf.GetPages())
                    {
                        text.AppendLine(page.Text);
                    }
                    return text.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting text from PDF: {filePath}");
                return $"[Error extracting text from PDF: {ex.Message}]";
            }
        }

        private static string DecodeBase64Key(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            try
            {
                string paddedInput = input.Trim();
                int mod = paddedInput.Length % 4;
                if (mod == 2) paddedInput += "==";
                else if (mod == 3) paddedInput += "=";

                var bytes = Convert.FromBase64String(paddedInput);
                foreach (var b in bytes)
                {
                    if ((b < 32 && b != 9 && b != 10 && b != 13) || b >= 127)
                    {
                        return input;
                    }
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return input;
            }
        }
    }

    public class AnalyzeReportRequest
    {
        public string? FilePath { get; set; }
    }

    public class AnalyzeReportResponse
    {
        [JsonPropertyName("reportName")]
        public string? ReportName { get; set; }

        [JsonPropertyName("doctorName")]
        public string? DoctorName { get; set; }

        [JsonPropertyName("reportDetails")]
        public string? ReportDetails { get; set; }
    }
}
