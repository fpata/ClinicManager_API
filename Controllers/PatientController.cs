using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientController : ControllerBase
    {
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
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientController> _logger;

        private const int CACHE_EXPIRY_MINUTES = 10;

        public PatientController(ClinicDbContext context, ILogger<PatientController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching patients page {pageNumber} with size {pageSize}");
            
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            var userIdClaim = User.FindFirst("userid")?.Value;
            
            var query = _context.Patients
                .AsNoTracking()
                .Where(p => p.IsActive == 1);

            if (roleClaim == "Patient")
            {
                if (int.TryParse(userIdClaim, out int loggedInUserId))
                {
                    query = query.Where(p => p.UserID == loggedInUserId);
                }
                else
                {
                    return Forbid();
                }
            }

            var patients = await query
                .OrderBy(p => p.ID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);
           
            return patients;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> Get(int id)
        {
            _logger.LogInformation($"Fetching patient with ID: {id}");
            
            // Use Include to solve N+1 problem with single query
            var entity = await _context.Patients
                .AsNoTracking()
                .AsSplitQuery() // Use AsSplitQuery to optimize loading related entities
                .Include(p => p.PatientAppointments)
                .Include(p => p.PatientReports)
                .Include(p => p.PatientVitals)
                .Include(p => p.PatientTreatment)
                    .ThenInclude(pt => pt!.PatientTreatmentDetails)
                .FirstOrDefaultAsync(p => p.ID == id && p.IsActive == 1);

            if (entity == null)
            {
                _logger.LogWarning($"Patient with ID: {id} not found");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            _logger.LogInformation($"Fetched patient with ID: {id}");
            return entity;
        }

        [HttpGet("Latest/{userId}")]
        public async Task<ActionResult<Patient>> GetLatestPatientByUserId(int userId)
        {
            _logger.LogInformation($"Fetching latest patient for user ID: {userId}");

            if (!IsAuthorizedForPatient(userId))
            {
                return Forbid();
            }
            
            var patient = await _context.Patients
                  .AsNoTracking()
                .AsSplitQuery() // Use AsSplitQuery to optimize loading related entities
                .Include(p => p.PatientAppointments)
                .Include(p => p.PatientReports)
                .Include(p => p.PatientVitals)
                .Include(p => p.PatientTreatment)
                    .ThenInclude(pt => pt!.PatientTreatmentDetails)
                .FirstOrDefaultAsync(p => p.UserID == userId && p.IsActive == 1)
                .ConfigureAwait(false);
            
            if (patient == null)
            {
                _logger.LogWarning($"No active patient found for user ID: {userId}");
                return NotFound();
            }
            
            _logger.LogInformation($"Fetched latest patient with ID: {patient.ID} for user ID: {userId}");
            return patient;
        }

        [HttpGet("Complete/{userId}")]
        public async Task<ActionResult<User>> GetComplete(int userId)
        {
            _logger.LogInformation($"Fetching complete data for user ID: {userId}");

            if (!IsAuthorizedForPatient(userId))
            {
                return Forbid();
            }

            // First, get the user by ID
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID == userId && u.IsActive == 1);

            if (user == null)
            {
                _logger.LogWarning($"User with ID: {userId} not found or is inactive");
                return NotFound();
            }

            // Get user's address and contact
            var address = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserID == userId && a.IsActive == 1);

            var contact = await _context.Contacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserID == userId && c.IsActive == 1);

            user.Address = address;
            user.Contact = contact;

            // Try to get the max patient ID for the user
            var maxPatientId = await _context.Patients
                .Where(p => p.UserID == userId && p.IsActive == 1)
                .MaxAsync(p => (int?)p.ID);

            // If patient exists, fetch complete patient data
            if (maxPatientId.HasValue)
            {
                var patientId = maxPatientId.Value;

                // Fetch patient data with all related entities
                var patientData = await (from _patient in _context.Patients
                                         where _patient.ID == patientId && _patient.IsActive == 1
                                         select new
                                         {
                                             Patient = _patient,
                                             PatientAppointments = _context.PatientAppointments
                                                 .Where(pa => pa.PatientID == patientId)
                                                 .ToList(),
                                             PatientReports = _context.PatientReports
                                                 .Where(pr => pr.PatientID == patientId)
                                                 .ToList(),
                                             PatientTreatment = _context.PatientTreatments
                                                 .Where(pt => pt.PatientID == patientId)
                                                 .FirstOrDefault(),
                                             PatientVitals = _context.PatientVitals
                                                 .Where(pv => pv.PatientID == patientId)
                                                 .ToList(),
                                             PatientTreatmentDetails = _context.PatientTreatmentDetails
                                                 .Where(ptd => ptd.PatientID == patientId)
                                                 .ToList()
                                         })
                                        .AsNoTracking()
                                        .AsSplitQuery()
                                        .FirstOrDefaultAsync();

                if (patientData != null)
                {
                    var patient = patientData.Patient;
                    patient.PatientAppointments = patientData.PatientAppointments;
                    patient.PatientReports = patientData.PatientReports;
                    patient.PatientVitals = patientData.PatientVitals;
                    patient.PatientTreatment = patientData.PatientTreatment;

                    if (patient.PatientTreatment != null)
                    {
                        patient.PatientTreatment.PatientTreatmentDetails = patientData.PatientTreatmentDetails;
                    }

                    user.Patients = new List<Patient> { patient };
                    _logger.LogInformation($"Fetched complete data for user ID: {userId} with patient ID: {patientId}");
                }
                else
                {
                    // Patient ID found but patient data not available
                    user.Patients = new List<Patient>();
                    _logger.LogWarning($"Patient with ID: {maxPatientId} not found for user ID: {userId}");
                }
            }
            else
            {
                // No patient found for user, return empty patient list
                user.Patients = new List<Patient>();
                _logger.LogInformation($"No active patients found for user ID: {userId}. Returning user data without patient.");
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> Post(Patient patient)
        {
            if (!IsAuthorizedForPatient(patient.UserID))
            {
                return Forbid();
            }

            using var dbContextTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Set timestamps and IsActive for the main patient entity
                patient.ID = 0; // Ensure new patient
                patient.CreatedDate = DateTime.Now;
                patient.ModifiedDate = DateTime.Now;
                patient.CreatedBy = patient.CreatedBy = 1; // Default to 1 if not set
                patient.ModifiedBy = patient.ModifiedBy ?? 1; // Default to 1 if not set
                patient.IsActive = 1;

                // Reset IDs and set properties for all related entities to ensure they are treated as new.
                if (patient.PatientReports?.Any() == true)
                {
                    foreach (var report in patient.PatientReports)
                    {
                        report.ID = 0;
                        report.UserID = patient.UserID;
                        report.CreatedDate = DateTime.Now;
                        report.ModifiedDate = DateTime.Now;
                        report.CreatedBy = report.CreatedBy ?? 1; // Default to 1 if not set
                        report.ModifiedBy = report.ModifiedBy ?? 1; // Default to 1 if not set
                        report.PatientID = null; // Clear foreign key - EF will set it
                        report.IsActive = 1;
                    }
                }

                if (patient.PatientAppointments?.Any() == true)
                {
                    foreach (var appointment in patient.PatientAppointments)
                    {
                        appointment.ID = 0;
                        appointment.UserID = patient.UserID;
                        appointment.CreatedDate = DateTime.Now;
                        appointment.ModifiedDate = DateTime.Now;
                        appointment.CreatedBy = appointment.CreatedBy ?? 1; // Default to 1 if not set
                        appointment.ModifiedBy = appointment.ModifiedBy ?? 1; // Default to 1 if not set
                        appointment.PatientID = null; // Clear foreign key - EF will set it
                        appointment.IsActive = 1;
                    }
                }
                if(patient.PatientVitals?.Any() == true)
                {
                    foreach (var vital in patient.PatientVitals)
                    {
                        vital.ID = 0;
                        vital.UserID = patient.UserID;
                        vital.CreatedDate = DateTime.Now;
                        vital.ModifiedDate = DateTime.Now;
                        vital.CreatedBy = vital.CreatedBy ?? 1; // Default to 1 if not set
                        vital.ModifiedBy = vital.ModifiedBy ?? 1; // Default to 1 if not set
                        vital.PatientID = null; // Clear foreign key - EF will set it
                        vital.IsActive = 1;
                    }
                }

                if (!(patient.PatientTreatment == null || String.IsNullOrWhiteSpace(patient.PatientTreatment.ChiefComplaint)))
                {
                    patient.PatientTreatment.ID = 0;
                    patient.PatientTreatment.UserID = patient.UserID;
                    patient.PatientTreatment.CreatedDate = DateTime.Now;
                    patient.PatientTreatment.ModifiedDate = DateTime.Now;
                    patient.PatientTreatment.CreatedBy = patient.PatientTreatment.CreatedBy ?? 1;
                    patient.PatientTreatment.ModifiedBy = patient.PatientTreatment.ModifiedBy ?? 1;
                    patient.PatientTreatment.PatientID = null; // Will be set by EF through relationship
                    patient.PatientTreatment.IsActive = 1;

                    if (patient.PatientTreatment.PatientTreatmentDetails?.Any() == true)
                    {
                        foreach (var detail in patient.PatientTreatment.PatientTreatmentDetails)
                        {
                            detail.ID = 0;
                            detail.UserID = patient.UserID;
                            detail.CreatedDate = DateTime.Now;
                            detail.ModifiedDate = DateTime.Now;
                            detail.CreatedBy = detail.CreatedBy ?? 1;
                            detail.ModifiedBy = detail.ModifiedBy ?? 1;
                            // CRITICAL: DO NOT null out PatientTreatmentID - EF needs this relationship to be tracked
                            // When PatientTreatment is added to Patient, EF will automatically set PatientTreatmentID
                            // ONLY set PatientID explicitly to maintain relationship
                            detail.PatientID = null; // EF will set through relationship when patient is saved
                            detail.IsActive = 1;
                        }
                    }
                }
                else
                {
                    patient.PatientTreatment = null;
                }

                // Add only the top-level patient object to the context.
                // EF Core will traverse the object graph and automatically detect and add all related entities:
                // 1. Patient -> PatientTreatment (via navigation property)
                // 2. PatientTreatment -> PatientTreatmentDetails (via navigation property)
                // All relationships configured in OnModelCreating will cascade the insert
                _context.Patients.Add(patient);

                // First save: This inserts Patient, and through cascade relationships, also inserts:
                // - PatientTreatment (with PatientID set from Patient relationship)
                // - PatientTreatmentDetails (with PatientTreatmentID set from PatientTreatment relationship)
                await _context.SaveChangesAsync();

                // Second save: Update FK references if needed (for PatientID on details)
                // This ensures all children have correct parent references
                if (patient.PatientTreatment?.PatientTreatmentDetails?.Any() == true)
                {
                    foreach (var detail in patient.PatientTreatment.PatientTreatmentDetails)
                    {
                        detail.PatientID = patient.ID;
                    }
                    await _context.SaveChangesAsync();
                }

                await dbContextTransaction.CommitAsync();
                _logger.LogInformation($"Created new patient with ID: {patient.ID}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                await dbContextTransaction.RollbackAsync();
                return StatusCode(500, "Internal server error");
            }

            return CreatedAtAction (nameof(Get), new { id = patient.ID }, patient);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Patient patient)
        {
            if (id != patient.ID)
            {
                _logger.LogWarning($"Patient ID mismatch: {id} != {patient.ID}");
                return BadRequest();
            }

            if (!IsAuthorizedForPatient(patient.UserID))
            {
                return Forbid();
            }

            using var dbContextTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Fetch existing tracked entities
                var existingPatient = await _context.Patients
                    .Include(p => p.PatientAppointments)
                    .Include(p => p.PatientReports)
                    .Include(p => p.PatientVitals)
                    .Include(p => p.PatientTreatment)
                        .ThenInclude(pt => pt!.PatientTreatmentDetails)
                    .FirstOrDefaultAsync(p => p.ID == id);

                if (existingPatient == null)
                {
                    _logger.LogWarning($"Patient with ID: {id} not found for update");
                    return NotFound();
                }

                // Update patient scalar properties
                existingPatient.UserID = patient.UserID;
                existingPatient.Allergies = patient.Allergies;
                existingPatient.Medications = patient.Medications;
                existingPatient.FatherMedicalHistory = patient.FatherMedicalHistory;
                existingPatient.MotherMedicalHistory = patient.MotherMedicalHistory;
                existingPatient.PersonalMedicalHistory = patient.PersonalMedicalHistory;
                //existingPatient.InsuranceProvider = patient?.InsuranceProvider || null;
                //xistingPatient.InsurancePolicyNumber = patient?.InsurancePolicyNumber;
                existingPatient.ModifiedDate = DateTime.Now;
                existingPatient.ModifiedBy = patient.ModifiedBy ?? 1;

                // Handle PatientAppointments
                if (patient.PatientAppointments?.Any() == true)
                {
                    var incomingIds = patient.PatientAppointments.Select(pa => pa.ID).ToHashSet();
                    var appointmentsToRemove = existingPatient?.PatientAppointments?
                        .Where(pa => !incomingIds.Contains(pa.ID))
                        .ToList();

                    foreach (var toRemove in appointmentsToRemove ?? Enumerable.Empty<PatientAppointment>())
                    {
                        _context.PatientAppointments.Remove(toRemove);
                    }

                    foreach (var incomingAppointment in patient.PatientAppointments)
                    {
                        var existingAppointment = existingPatient?.PatientAppointments?
                            .FirstOrDefault(pa => pa.ID == incomingAppointment.ID);

                        if (existingAppointment != null)
                        {
                            // Update existing tracked entity
                            _context.Entry(existingAppointment).CurrentValues.SetValues(incomingAppointment);
                            existingAppointment.PatientID = id;
                            existingAppointment.UserID = patient.UserID;
                            existingAppointment.ModifiedDate = DateTime.Now;
                            existingAppointment.ModifiedBy = incomingAppointment.ModifiedBy ?? 1;
                        }
                        else
                        {
                            // Add new appointment
                            incomingAppointment.ID = 0;
                            incomingAppointment.PatientID = id;
                            incomingAppointment.UserID = patient.UserID;
                            incomingAppointment.CreatedDate = DateTime.Now;
                            incomingAppointment.ModifiedDate = DateTime.Now;
                            incomingAppointment.CreatedBy = incomingAppointment.CreatedBy ?? 1;
                            incomingAppointment.ModifiedBy = incomingAppointment.ModifiedBy ?? 1;
                            incomingAppointment.IsActive = 1;
                            existingPatient?.PatientAppointments?.Add(incomingAppointment);
                        }
                    }
                }
                else if (existingPatient.PatientAppointments?.Any() == true)
                {
                    foreach (var appointment in existingPatient?.PatientAppointments?.ToList() ?? Enumerable.Empty<PatientAppointment>())
                    {
                        _context.PatientAppointments.Remove(appointment);
                    }
                }

                // Handle PatientReports
                if (patient.PatientReports?.Any() == true)
                {
                    var incomingIds = patient.PatientReports.Select(pr => pr.ID).ToHashSet();
                    var reportsToRemove = existingPatient?.PatientReports?
                        .Where(pr => !incomingIds.Contains(pr.ID))
                        .ToList();

                    foreach (var toRemove in reportsToRemove ?? Enumerable.Empty<PatientReport>())
                    {
                        _context.PatientReports.Remove(toRemove);
                    }

                    foreach (var incomingReport in patient.PatientReports)
                    {
                        var existingReport = existingPatient?.PatientReports?
                            .FirstOrDefault(pr => pr.ID == incomingReport.ID);

                        if (existingReport != null)
                        {
                            _context.Entry(existingReport).CurrentValues.SetValues(incomingReport);
                            existingReport.PatientID = id;
                            existingReport.UserID = patient.UserID;
                            existingReport.ModifiedDate = DateTime.Now;
                            existingReport.ModifiedBy = incomingReport.ModifiedBy ?? 1;
                        }
                        else
                        {
                            incomingReport.ID = 0;
                            incomingReport.PatientID = id;
                            incomingReport.UserID = patient.UserID;
                            incomingReport.CreatedDate = DateTime.Now;
                            incomingReport.ModifiedDate = DateTime.Now;
                            incomingReport.CreatedBy = incomingReport.CreatedBy ?? 1;
                            incomingReport.ModifiedBy = incomingReport.ModifiedBy ?? 1;
                            incomingReport.IsActive = 1;
                            existingPatient?.PatientReports?.Add(incomingReport);
                        }
                    }
                }
                else if (existingPatient?.PatientReports?.Any() == true)
                {
                    foreach (var report in existingPatient?.PatientReports?.ToList() ?? Enumerable.Empty<PatientReport>())
                    {
                        _context.PatientReports.Remove(report);
                    }
                }

                // Handle PatientVitals
                if (patient.PatientVitals?.Any() == true)
                {
                    var incomingIds = patient.PatientVitals.Select(pv => pv.ID).ToHashSet();
                    var vitalsToRemove = existingPatient?.PatientVitals?
                        .Where(pv => !incomingIds.Contains(pv.ID))
                        .ToList();

                    foreach (var toRemove in vitalsToRemove ?? Enumerable.Empty<PatientVitals>())
                    {
                        _context.PatientVitals.Remove(toRemove);
                    }

                    foreach (var incomingVital in patient.PatientVitals)
                    {
                        var existingVital = existingPatient?.PatientVitals?
                            .FirstOrDefault(pv => pv.ID == incomingVital.ID);

                        if (existingVital != null)
                        {
                            _context.Entry(existingVital).CurrentValues.SetValues(incomingVital);
                            existingVital.PatientID = id;
                            existingVital.UserID = patient.UserID;
                            existingVital.ModifiedDate = DateTime.Now;
                            existingVital.ModifiedBy = incomingVital.ModifiedBy ?? 1;
                        }
                        else
                        {
                            incomingVital.ID = 0;
                            incomingVital.PatientID = id;
                            incomingVital.UserID = patient.UserID;
                            incomingVital.CreatedDate = DateTime.Now;
                            incomingVital.ModifiedDate = DateTime.Now;
                            incomingVital.CreatedBy = incomingVital.CreatedBy ?? 1;
                            incomingVital.ModifiedBy = incomingVital.ModifiedBy ?? 1;
                            incomingVital.IsActive = 1;
                            existingPatient?.PatientVitals?.Add(incomingVital);
                        }
                    }
                }
                else if (existingPatient?.PatientVitals?.Any() == true)
                {
                    foreach (var vital in existingPatient.PatientVitals.ToList())
                    {
                        _context.PatientVitals.Remove(vital);
                    }
                }

                // Handle PatientTreatment
                if (!(patient.PatientTreatment == null || string.IsNullOrWhiteSpace(patient.PatientTreatment?.ChiefComplaint)))
                {
                    var incomingTreatment = patient.PatientTreatment;

                    if (existingPatient?.PatientTreatment != null && existingPatient?.PatientTreatment.ID == incomingTreatment.ID)
                    {
                        // Update existing treatment
                        _context.Entry(existingPatient.PatientTreatment).CurrentValues.SetValues(incomingTreatment);
                        existingPatient.PatientTreatment.PatientID = id;
                        existingPatient.PatientTreatment.UserID = patient.UserID;
                        existingPatient.PatientTreatment.ModifiedDate = DateTime.Now;
                        existingPatient.PatientTreatment.ModifiedBy = incomingTreatment.ModifiedBy ?? 1;
                        existingPatient.PatientTreatment.IsActive = 1;

                        // Handle treatment details
                        if (incomingTreatment.PatientTreatmentDetails?.Any() == true)
                        {
                            var incomingDetailIds = incomingTreatment.PatientTreatmentDetails.Select(ptd => ptd.ID).ToHashSet();
                            var detailsToRemove = existingPatient?.PatientTreatment?.PatientTreatmentDetails?
                                .Where(ptd => !incomingDetailIds.Contains(ptd.ID))
                                .ToList();
                           

                                foreach (var toRemove in detailsToRemove ?? Enumerable.Empty<PatientTreatmentDetail>())
                                {
                                    _context.PatientTreatmentDetails.Remove(toRemove);
                                }
                           
                            foreach (var incomingDetail in incomingTreatment.PatientTreatmentDetails)
                            {
                                var existingDetail = existingPatient?.PatientTreatment.PatientTreatmentDetails?
                                    .FirstOrDefault(ptd => ptd.ID == incomingDetail.ID);

                                if (existingDetail != null)
                                {
                                    _context.Entry(existingDetail).CurrentValues.SetValues(incomingDetail);
                                    existingDetail.PatientTreatmentID = existingPatient?.PatientTreatment.ID;
                                    existingDetail.PatientID = id;
                                    existingDetail.UserID = patient.UserID;
                                    existingDetail.ModifiedDate = DateTime.Now;
                                    existingDetail.ModifiedBy = incomingDetail.ModifiedBy ?? 1;
                                }
                                else
                                {
                                    incomingDetail.ID = 0;
                                    incomingDetail.PatientTreatmentID = existingPatient?.PatientTreatment.ID;
                                    incomingDetail.PatientID = id;
                                    incomingDetail.UserID = patient.UserID;
                                    incomingDetail.CreatedDate = DateTime.Now;
                                    incomingDetail.ModifiedDate = DateTime.Now;
                                    incomingDetail.CreatedBy = incomingDetail.CreatedBy ?? 1;
                                    incomingDetail.ModifiedBy = incomingDetail.ModifiedBy ?? 1;
                                    incomingDetail.IsActive = 1;
                                    if (existingPatient?.PatientTreatment?.PatientTreatmentDetails != null)
                                    {
                                        existingPatient.PatientTreatment.PatientTreatmentDetails.Add(incomingDetail);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var detail in existingPatient.PatientTreatment.PatientTreatmentDetails?.ToList() ?? new List<PatientTreatmentDetail>())
                            {
                                _context.PatientTreatmentDetails.Remove(detail);
                            }
                        }
                    }
                    else if (existingPatient?.PatientTreatment == null)
                    {
                        // Create new treatment
                        var newTreatment = new PatientTreatment
                        {
                            PatientID = id,
                            UserID = patient.UserID,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            CreatedBy = incomingTreatment.CreatedBy ?? 1,
                            ModifiedBy = incomingTreatment.ModifiedBy ?? 1,
                            IsActive = 1
                        };

                        // Copy scalar properties from incoming treatment
                        _context.Entry(newTreatment).CurrentValues.SetValues(incomingTreatment);
                        newTreatment.PatientID = id;
                        newTreatment.UserID = patient.UserID;

                        existingPatient?.PatientTreatment = newTreatment;

                        if (incomingTreatment.PatientTreatmentDetails?.Any() == true)
                        {
                            foreach (var incomingDetail in incomingTreatment.PatientTreatmentDetails)
                            {
                                var newDetail = new PatientTreatmentDetail
                                {
                                    PatientTreatmentID = newTreatment.ID,
                                    PatientID = id,
                                    UserID = patient.UserID,
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now,
                                    CreatedBy = incomingDetail.CreatedBy ?? 1,
                                    ModifiedBy = incomingDetail.ModifiedBy ?? 1,
                                    IsActive = 1
                                };
                                _context.Entry(newDetail).CurrentValues.SetValues(incomingDetail);
                                newTreatment.PatientTreatmentDetails?.Add(newDetail);
                            }
                        }
                    }
                }
                else if (existingPatient?.PatientTreatment != null)
                {
                    // Remove existing treatment and details
                    if (existingPatient.PatientTreatment.PatientTreatmentDetails?.Any() == true)
                    {
                        foreach (var detail in existingPatient.PatientTreatment.PatientTreatmentDetails.ToList())
                        {
                            _context.PatientTreatmentDetails.Remove(detail);
                        }
                    }
                    _context.PatientTreatments.Remove(existingPatient.PatientTreatment);
                    existingPatient.PatientTreatment = null;
                }

                await _context.SaveChangesAsync();
                await dbContextTransaction.CommitAsync();

                _logger.LogInformation($"Updated patient with ID: {id}");

                var refreshed = await Get(id);
                return Ok(refreshed.Value!);
            }
            catch (Exception ex)
            {
                await dbContextTransaction.RollbackAsync();
                _logger.LogError(ex, $"Error updating patient with ID: {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<Patient> patchDoc)
        {
            var entity = await _context.Patients
                .FirstOrDefaultAsync(p => p.ID == id);
                
            if (entity == null)
            {
                _logger.LogWarning($"Patient with ID: {id} not found for patch");
                return NotFound();
            }

            if (!IsAuthorizedForPatient(entity.UserID))
            {
                return Forbid();
            }

            patchDoc.ApplyTo(entity);
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Patched patient with ID: {id}");
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

            if(id==0)
            {
                _logger.LogWarning($"Invalid patient ID: {id}");
                return BadRequest();
            }
            var entity = await _context.Patients
                .AsSplitQuery() // Use AsSplitQuery to optimize loading related entities
                .Include(p => p.PatientAppointments)
                .Include(p => p.PatientReports)
                .Include(p => p.PatientVitals)
                .Include(p => p.PatientTreatment)
                    .ThenInclude(pt => pt!.PatientTreatmentDetails)
                .FirstOrDefaultAsync(p => p.ID == id);
                
            if (entity == null)
            {
                _logger.LogWarning($"Patient with ID: {id} not found for deletion");
                return NotFound();
            }

            // Soft delete instead of hard delete for better performance and data integrity
            entity.IsActive = 0;
            entity.ModifiedDate = DateTime.Now;
            if (entity.PatientTreatment != null)
            {
                entity.PatientTreatment.IsActive = 0;
                entity.PatientTreatment.ModifiedDate = DateTime.Now;
            }

            if (entity.PatientVitals != null)
            {
                entity.PatientVitals.ToList().ForEach(pv =>
                {
                    pv.IsActive = 0;
                    pv.ModifiedDate = DateTime.Now;
                });
            }
            if(entity.PatientTreatment !=null && entity.PatientTreatment.PatientTreatmentDetails != null)
            {
                entity.PatientTreatment.PatientTreatmentDetails.ToList().ForEach(ptd =>
                {
                    ptd.IsActive = 0;
                    ptd.ModifiedDate = DateTime.Now;
                });
            }
            if(entity.PatientReports != null)
            {
                entity.PatientReports.ToList().ForEach(pr =>
                {
                    pr.IsActive = 0;
                    pr.ModifiedDate = DateTime.Now;
                });
            }
            if(entity.PatientAppointments != null)
            {
                entity.PatientAppointments.ToList().ForEach(pa =>
                {
                    pa.IsActive = 0;
                    pa.ModifiedDate = DateTime.Now;
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Soft deleted patient with ID: {id}");
            return NoContent();
        }
    }
}
