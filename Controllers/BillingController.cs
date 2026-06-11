using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;


namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,Doctor,Accountant")]
    public class BillingController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<BillingController> _logger;
        public BillingController(ClinicDbContext context, ILogger<BillingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillingRecord>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching billing records page {pageNumber} with size {pageSize}");
            var records = await _context.BillingRecords
                .Include(br => br.Payments)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return records;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillingRecord>> Get(int id)
        {
            _logger.LogInformation($"Fetching billing record with ID: {id}");
            var entity = await _context.BillingRecords
                .Include(br => br.Payments)
                .FirstOrDefaultAsync(br => br.ID == id);
            if (entity == null)
            {
                _logger.LogWarning($"Billing record with ID: {id} not found");
                return NotFound();
            }
            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<BillingRecord>> Post(BillingRecord billingRecord)
        {
            await PopulateBillingRecordFields(billingRecord);
            _context.BillingRecords.Add(billingRecord);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Created new billing record with ID: {billingRecord.ID}");
            return CreatedAtAction(nameof(Get), new { id = billingRecord.ID }, billingRecord);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, BillingRecord billingRecord)
        {
            if (id != billingRecord.ID)
            {
                _logger.LogWarning($"Billing record ID mismatch: {id} != {billingRecord.ID}");
                return BadRequest();
            }
            await PopulateBillingRecordFields(billingRecord);
            _context.Entry(billingRecord).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated billing record with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<BillingRecord> patchDoc)
        {
            var entity = await _context.BillingRecords.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Billing record with ID: {id} not found for patch");
                return NotFound();
            }
            patchDoc.ApplyTo(entity);
            await PopulateBillingRecordFields(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Patched billing record with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.BillingRecords.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Billing record with ID: {id} not found for delete");
                return NotFound();
            }
            _context.BillingRecords.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted billing record with ID: {id}");
            return NoContent();
        }


        [HttpPost("search")]
        public async Task<ActionResult<IEnumerable<BillingRecord>>> Search([FromBody] BillingSearchCriteria searchCriteria)
        {
            _logger.LogInformation("Searching billing records with provided criteria");
            IQueryable<BillingRecord> query = _context.BillingRecords;

            if (searchCriteria.PatientID != 0 && searchCriteria.PatientID != null)
            {
                query = query.Where(br => _context.PatientTreatments.Any(pt => pt.ID == br.TreatmentID && pt.PatientID == searchCriteria.PatientID));
            }
            if (searchCriteria.DoctorID != 0  && searchCriteria.DoctorID != null)
            {
                query = query.Where(br => _context.PatientTreatments.Any(pt => pt.ID == br.TreatmentID && pt.DoctorID == searchCriteria.DoctorID));
            }
            if(searchCriteria.TreatmentID != 0 && searchCriteria.TreatmentID != null)
            {
                query = query.Where(br => br.TreatmentID == searchCriteria.TreatmentID);
            }
            if(searchCriteria.Total != 0 && searchCriteria.Total != null)
            {
                query = query.Where(br => br.Total == searchCriteria.Total );
            }
            if (searchCriteria.BalanceDue != 0 && searchCriteria.BalanceDue != null)
            {
                query = query.Where(br => br.BalanceDue == searchCriteria.BalanceDue );
            }
            if (searchCriteria.Status != null)
            {
                query = query.Where(br => br.Status == searchCriteria.Status);
            }
            if(!String.IsNullOrWhiteSpace(searchCriteria.PatientName))
            {
                // Optimize search results by filtering directly on the flattened column in the DB
                query = query.Where(br => br.PatientName != null && br.PatientName.Contains(searchCriteria.PatientName));
            }
            if (searchCriteria.StartDate != null)
            {
                query = query.Where(br => br.CreatedDate >= searchCriteria.StartDate);
            }
            if (searchCriteria.EndDate != null)
            {
                query = query.Where(br => br.CreatedDate <= searchCriteria.EndDate);
            }
            
            // Get total count before pagination
            var totalCount = await query.CountAsync();
            var results = await query
                .Include(br => br.Payments)
                .Select(model => model)
                .AsNoTracking()
                .OrderByDescending(a => a.TreatmentID)
                .Skip((searchCriteria.PageNumber - 1) * searchCriteria.PageSize)
                .Take(searchCriteria.PageSize)
                .ToListAsync();

            // For backward compatibility, dynamically populate any missing flattened fields for older database records in results in bulk
            var missingFieldsRecords = results.Where(r => string.IsNullOrEmpty(r.PatientName) || string.IsNullOrEmpty(r.DoctorName) || string.IsNullOrEmpty(r.TreatmentName) || r.ServiceDate == null).ToList();
            if (missingFieldsRecords.Any())
            {
                var treatmentIds = missingFieldsRecords.Select(r => r.TreatmentID).Distinct().ToList();
                var treatments = await _context.PatientTreatments
                    .Include(pt => pt.PatientTreatmentDetails)
                    .Where(pt => treatmentIds.Contains(pt.ID))
                    .ToListAsync();

                var patientIds = treatments.Select(t => t.PatientID).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
                var doctorIds = treatments.Select(t => t.DoctorID).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

                var patients = await _context.Patients
                    .Where(p => patientIds.Contains(p.ID))
                    .ToListAsync();

                var patientUserIds = patients.Select(p => p.UserID).Distinct().ToList();
                var userIdsToFetch = patientUserIds.Concat(doctorIds).Distinct().ToList();

                var users = await _context.Users
                    .Where(u => userIdsToFetch.Contains(u.ID))
                    .ToListAsync();

                var treatmentsDict = treatments.ToDictionary(t => t.ID);
                var patientsDict = patients.ToDictionary(p => p.ID);
                var usersDict = users.ToDictionary(u => u.ID);

                foreach (var record in missingFieldsRecords)
                {
                    if (treatmentsDict.TryGetValue(record.TreatmentID, out var treatment))
                    {
                        if (string.IsNullOrEmpty(record.TreatmentName))
                        {
                            record.TreatmentName = treatment.PatientTreatmentDetails != null && treatment.PatientTreatmentDetails.Any()
                                ? string.Join(", ", treatment.PatientTreatmentDetails.Select(d => d.Procedure))
                                : treatment.TreatmentPlan;
                        }

                        if (string.IsNullOrEmpty(record.PatientName) && treatment.PatientID.HasValue && patientsDict.TryGetValue(treatment.PatientID.Value, out var patient))
                        {
                            if (usersDict.TryGetValue(patient.UserID, out var pUser))
                            {
                                record.PatientName = $"{pUser.FirstName} {pUser.LastName}".Trim();
                            }
                        }

                        if (string.IsNullOrEmpty(record.DoctorName) && treatment.DoctorID.HasValue && usersDict.TryGetValue(treatment.DoctorID.Value, out var dUser))
                        {
                            record.DoctorName = $"{dUser.FirstName} {dUser.LastName}".Trim();
                        }

                        if (record.ServiceDate == null)
                        {
                            record.ServiceDate = treatment.CreatedDate;
                        }
                    }
                }
            }

            var hasMoreRecords = (searchCriteria.PageNumber * searchCriteria.PageSize) < totalCount;
            var message = results.Count > 0 ? "Records found." : "No Billing Information found.";
            var response = new SearchResultsBillingRecord
            {
                billingRecords = results,
                TotalCount = totalCount,
                HasMoreRecords = hasMoreRecords,
                Message = message
            };
            return Ok(response);
        }

        private async Task PopulateBillingRecordFields(BillingRecord billingRecord)
        {
            var treatment = await _context.PatientTreatments
                .Include(pt => pt.PatientTreatmentDetails)
                .FirstOrDefaultAsync(pt => pt.ID == billingRecord.TreatmentID);
            if (treatment != null)
            {
                billingRecord.TreatmentName = treatment.PatientTreatmentDetails != null && treatment.PatientTreatmentDetails.Any()
                    ? string.Join(", ", treatment.PatientTreatmentDetails.Select(d => d.Procedure))
                    : treatment.TreatmentPlan;

                // Get Patient User
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ID == treatment.PatientID);
                if (patient != null)
                {
                    var pUser = await _context.Users.FirstOrDefaultAsync(u => u.ID == patient.UserID);
                    if (pUser != null)
                    {
                        billingRecord.PatientName = $"{pUser.FirstName} {pUser.LastName}".Trim();
                    }
                }

                // Get Doctor User
                if (treatment.DoctorID != null)
                {
                    var dUser = await _context.Users.FirstOrDefaultAsync(u => u.ID == treatment.DoctorID);
                    if (dUser != null)
                    {
                        billingRecord.DoctorName = $"{dUser.FirstName} {dUser.LastName}".Trim();
                    }
                }

                // Get ServiceDate from CreatedDate of treatment
                billingRecord.ServiceDate = treatment.CreatedDate;
            }
        }
    }
}
