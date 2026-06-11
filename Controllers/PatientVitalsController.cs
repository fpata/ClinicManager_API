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
    public class PatientVitalsController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientVitalsController> _logger;

        public PatientVitalsController(ClinicDbContext context, ILogger<PatientVitalsController> logger)
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
        public async Task<ActionResult<IEnumerable<PatientVitals>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching patient vitals page {pageNumber} with size {pageSize}");
            
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            var userIdClaim = User.FindFirst("userid")?.Value;
            
            var query = _context.PatientVitals.AsNoTracking();

            if (roleClaim == "Patient")
            {
                if (int.TryParse(userIdClaim, out int loggedInUserId))
                {
                    query = query.Where(v => v.UserID == loggedInUserId);
                }
                else
                {
                    return Forbid();
                }
            }

            var vitals = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return vitals;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientVitals>> Get(int id)
        {
            _logger.LogInformation($"Fetching vitals with ID: {id}");
            var entity = await _context.PatientVitals.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Vitals with ID: {id} not found");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<PatientVitals>> Post(PatientVitals vitals)
        {
            if (!IsAuthorizedForPatient(vitals.UserID))
            {
                return Forbid();
            }

            _context.PatientVitals.Add(vitals);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Created new vitals with ID: {vitals.ID}");
            return CreatedAtAction(nameof(Get), new { id = vitals.ID }, vitals);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, PatientVitals vitals)
        {
            if (id != vitals.ID)
            {
                _logger.LogWarning($"Vitals ID mismatch: {id} != {vitals.ID}");
                return BadRequest();
            }

            if (!IsAuthorizedForPatient(vitals.UserID))
            {
                return Forbid();
            }

            _context.Entry(vitals).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated vitals with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<PatientVitals> patchDoc)
        {
            var entity = await _context.PatientVitals.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Vitals with ID: {id} not found for patch");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            patchDoc.ApplyTo(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Patched vitals with ID: {id}");
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

            var entity = await _context.PatientVitals.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Vitals with ID: {id} not found for deletion");
                return NotFound();
            }

            _context.PatientVitals.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted vitals with ID: {id}");
            return NoContent();
        }
    }
}