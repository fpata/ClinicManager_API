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
    public class PatientTreatmentDetailController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientTreatmentDetailController> _logger;

        public PatientTreatmentDetailController(ClinicDbContext context, ILogger<PatientTreatmentDetailController> logger)
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
        public async Task<ActionResult<IEnumerable<PatientTreatmentDetail>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching patient treatment details page {pageNumber} with size {pageSize}");
            
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            var userIdClaim = User.FindFirst("userid")?.Value;
            
            var query = _context.PatientTreatmentDetails.AsNoTracking();

            if (roleClaim == "Patient")
            {
                if (int.TryParse(userIdClaim, out int loggedInUserId))
                {
                    query = query.Where(d => d.UserID == loggedInUserId);
                }
                else
                {
                    return Forbid();
                }
            }

            var details = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return details;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientTreatmentDetail>> Get(int id)
        {
            _logger.LogInformation($"Fetching patient treatment detail with ID: {id}");
            var entity = await _context.PatientTreatmentDetails.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment detail with ID: {id} not found");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<PatientTreatmentDetail>> Post(PatientTreatmentDetail detail)
        {
            if (!IsAuthorizedForPatient(detail.UserID))
            {
                return Forbid();
            }

            _context.PatientTreatmentDetails.Add(detail);
            await _context.SaveChangesAsync();
            await UpdateBillingSubtotalAsync(detail.PatientTreatmentID);
            _logger.LogInformation($"Created new patient treatment detail with ID: {detail.ID}");
            return CreatedAtAction(nameof(Get), new { id = detail.ID }, detail);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, PatientTreatmentDetail detail)
        {
            if (id != detail.ID)
            {
                _logger.LogWarning($"Patient treatment detail ID mismatch: {id} != {detail.ID}");
                return BadRequest();
            }

            if (!IsAuthorizedForPatient(detail.UserID))
            {
                return Forbid();
            }

            _context.Entry(detail).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await UpdateBillingSubtotalAsync(detail.PatientTreatmentID);
            _logger.LogInformation($"Updated patient treatment detail with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<PatientTreatmentDetail> patchDoc)
        {
            var entity = await _context.PatientTreatmentDetails.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment detail with ID: {id} not found for patch");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            patchDoc.ApplyTo(entity);
            await _context.SaveChangesAsync();
            await UpdateBillingSubtotalAsync(entity.PatientTreatmentID);
            _logger.LogInformation($"Patched patient treatment detail with ID: {id}");
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

            var entity = await _context.PatientTreatmentDetails.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Patient treatment detail with ID: {id} not found for deletion");
                return NotFound();
            }

            var patientTreatmentId = entity.PatientTreatmentID;
            _context.PatientTreatmentDetails.Remove(entity);
            await _context.SaveChangesAsync();
            await UpdateBillingSubtotalAsync(patientTreatmentId);
            _logger.LogInformation($"Deleted patient treatment detail with ID: {id}");
            return NoContent();
        }

        private async Task UpdateBillingSubtotalAsync(int? patientTreatmentId)
        {
            if (!patientTreatmentId.HasValue) return;

            // Calculate sum of ProcedureTreatmentCost from PatientTreatmentDetail for this treatment
            var totalDetailCost = await _context.PatientTreatmentDetails
                .Where(ptd => ptd.PatientTreatmentID == patientTreatmentId.Value && ptd.IsActive == 1)
                .SumAsync(ptd => ptd.ProcedureTreatmentCost ?? 0);

            // Fetch the billing record for this patient treatment
            var billingRecord = await _context.BillingRecords
                .FirstOrDefaultAsync(br => br.TreatmentID == patientTreatmentId.Value);

            if (billingRecord != null)
            {
                billingRecord.Subtotal = totalDetailCost;
                billingRecord.Total = billingRecord.Subtotal + (billingRecord.TaxTotal ?? 0) - (billingRecord.DiscountTotal ?? 0);
                billingRecord.BalanceDue = billingRecord.Total - (billingRecord.AmountPaid ?? 0);
                billingRecord.ModifiedDate = DateTime.Now;
                _context.BillingRecords.Update(billingRecord);
                await _context.SaveChangesAsync();
            }
            else
            {
                // If there's no billing record, create one
                var treatment = await _context.PatientTreatments.FindAsync(patientTreatmentId.Value);
                if (treatment != null)
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

                    var newBilling = new BillingRecord
                    {
                        TreatmentID = patientTreatmentId.Value,
                        Subtotal = totalDetailCost,
                        TaxTotal = 0,
                        DiscountTotal = 0,
                        Total = totalDetailCost,
                        AmountPaid = 0,
                        BalanceDue = totalDetailCost,
                        IsActive = 1,
                        CreatedBy = treatment.CreatedBy,
                        CreatedDate = DateTime.Now,
                        ModifiedBy = treatment.CreatedBy,
                        ModifiedDate = DateTime.Now,
                        Status = Models.Enums.BillingStatus.Submitted
                    };
                    _context.BillingRecords.Add(newBilling);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
