using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientReportController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientReportController> _logger;

        public PatientReportController(ClinicDbContext context, ILogger<PatientReportController> logger)
        {
            _context = context;
            _logger = logger;
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
    }
}
