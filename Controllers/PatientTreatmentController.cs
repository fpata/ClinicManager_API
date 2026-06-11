using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ClinicManager.Services;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientTreatmentController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientTreatmentController> _logger;
        private readonly IPrescriptionService _prescriptionService;

        public PatientTreatmentController(ClinicDbContext context, ILogger<PatientTreatmentController> logger, IPrescriptionService prescriptionService)
        {
            _context = context;
            _logger = logger;
            _prescriptionService = prescriptionService;
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
        public async Task<ActionResult<IEnumerable<PatientTreatment>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching patient treatments page {pageNumber} with size {pageSize}");
            
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            var userIdClaim = User.FindFirst("userid")?.Value;
            
            var query = _context.PatientTreatments.AsNoTracking();

            if (roleClaim == "Patient")
            {
                if (int.TryParse(userIdClaim, out int loggedInUserId))
                {
                    query = query.Where(t => t.UserID == loggedInUserId);
                }
                else
                {
                    return Forbid();
                }
            }

            var treatments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return treatments;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientTreatment>> Get(int id)
        {
            _logger.LogInformation($"Fetching patient treatment with ID: {id}");
            var entity = await _context.PatientTreatments
                .Include(pt => pt.PatientTreatmentDetails)
                .FirstOrDefaultAsync(pt => pt.ID == id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment with ID: {id} not found");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<PatientTreatment>> Post(PatientTreatment treatment)
        {
            if (!IsAuthorizedForPatient(treatment.UserID))
            {
                return Forbid();
            }

            _context.PatientTreatments.Add(treatment);
            await _context.SaveChangesAsync();
            await SaveBillingRecordAsync(treatment.ID, treatment, true);         
            _logger.LogInformation($"Created new patient treatment with ID: {treatment.ID}");
            return CreatedAtAction(nameof(Get), new { id = treatment.ID }, treatment);
        }

        private async Task SaveBillingRecordAsync(int treatmentId, PatientTreatment treatment, bool IsCreate)
        {
            string patientName = "";
            if (treatment.PatientID.HasValue)
            {
                var patient = await _context.Patients.FindAsync(treatment.PatientID.Value);
                if (patient != null)
                {
                    var user = await _context.Users.FindAsync(patient.UserID);
                    if (user != null)
                    {
                        patientName = user.FullName;
                    }
                }
            }

            string doctorName = "";
            if (treatment.DoctorID.HasValue)
            {
                var doctor = await _context.Users.FindAsync(treatment.DoctorID.Value);
                if (doctor != null)
                {
                    doctorName = doctor.FullName;
                }
            }

            if (IsCreate)
            {
                var billingRecord = new BillingRecord
                {
                    TreatmentID = treatmentId,
                    BalanceDue = treatment.EstimatedCost ?? 0,
                    CreatedBy = treatment.CreatedBy,
                    CreatedDate = DateTime.Now,
                    DiscountTotal = 0,
                    Subtotal = treatment.EstimatedCost ?? 0,
                    TaxTotal = 0,
                    Total = treatment.EstimatedCost ?? 0,
                    IsActive = 1,
                    ModifiedBy = treatment.CreatedBy,
                    ModifiedDate = DateTime.Now,
                    Status = Models.Enums.BillingStatus.Submitted,
                    AmountPaid = 0
                };
                _context.BillingRecords.Add(billingRecord);
                await _context.SaveChangesAsync();
            }
            else
            {
                BillingRecord? bilrec = await _context.BillingRecords
                    .FirstOrDefaultAsync(br => br.TreatmentID == treatmentId);
                if (bilrec != null)
                {
                    if (treatment.EstimatedCost.HasValue && treatment.EstimatedCost.Value != bilrec.Total)
                    {
                        bilrec.Subtotal = treatment.EstimatedCost.Value;
                        bilrec.Total = treatment.EstimatedCost.Value - (bilrec.DiscountTotal ?? 0) + (bilrec.TaxTotal ?? 0);
                        bilrec.BalanceDue = bilrec.Total - (bilrec.AmountPaid ?? 0);
                        bilrec.ModifiedBy = treatment.ModifiedBy;
                        bilrec.ModifiedDate = DateTime.Now;
                        _context.BillingRecords.Update(bilrec);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, PatientTreatment treatment)
        {
            if (id != treatment.ID)
            {
                _logger.LogWarning($"Patient treatment ID mismatch: {id} != {treatment.ID}");
                return BadRequest();
            }

            if (!IsAuthorizedForPatient(treatment.UserID))
            {
                return Forbid();
            }

            _context.Entry(treatment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await SaveBillingRecordAsync(treatment.ID, treatment, false);
            _logger.LogInformation($"Updated patient treatment with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<PatientTreatment> patchDoc)
        {
            var entity = await _context.PatientTreatments.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment with ID: {id} not found for patch");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            patchDoc.ApplyTo(entity);
            await _context.SaveChangesAsync();
            await SaveBillingRecordAsync(entity.ID, entity, false);
            _logger.LogInformation($"Patched patient treatment with ID: {id}");
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

            var entity = await _context.PatientTreatments.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment with ID: {id} not found for deletion");
                return NotFound();
            }

            _context.PatientTreatments.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted patient treatment with ID: {id}");
            return NoContent();
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetbybyUserId(int id)
        {
            _logger.LogInformation($"Fetching patient treatment with User ID: {id}");

            if (!IsAuthorizedForPatient(id))
            {
                return Forbid();
            }

            var entity = await _context.PatientTreatments.Where(x => x.UserID == id).ToListAsync();
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment with User ID: {id} not found");
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("{treatmentId}/prescription/print")]
        public async Task<IActionResult> PrintPrescription(
            int treatmentId, 
            [FromQuery] int? treatmentDetailId = null,
            [FromQuery] bool includeHeader = true,
            [FromQuery] string? doctorNotes = null)
        {
            _logger.LogInformation($"Generating prescription RTF for Treatment ID: {treatmentId}, Detail ID: {treatmentDetailId}");

            var treatment = await _context.PatientTreatments.FindAsync(treatmentId);
            if (treatment == null)
            {
                _logger.LogWarning($"Treatment with ID {treatmentId} not found");
                return NotFound($"Treatment with ID {treatmentId} not found.");
            }

            if (!IsAuthorizedForPatient(treatment.UserID))
            {
                return Forbid();
            }

            try
            {
                var rtfBytes = await _prescriptionService.GeneratePrescriptionRtfAsync(
                    treatmentId, 
                    treatmentDetailId, 
                    includeHeader, 
                    doctorNotes);
                
                string filename = $"Prescription_{treatmentId}";
                if (treatmentDetailId.HasValue)
                {
                    filename += $"_{treatmentDetailId.Value}";
                }
                filename += ".rtf";

                return File(rtfBytes, "application/rtf", filename);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prescription RTF.");
                return StatusCode(500, "Internal server error generating prescription.");
            }
        }
    }
}
