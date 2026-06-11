using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using ClinicManager.Models.Enums;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientAppointmentController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PatientAppointmentController> _logger;
        private const int CACHE_EXPIRY_MINUTES = 5;

        public PatientAppointmentController(ClinicDbContext context, ILogger<PatientAppointmentController> logger)
        {
            _context = context;
            _logger = logger;
        }
      
       [HttpGet]
        public async Task<ActionResult<AppointmentSearchResponse>> GetAll(UserType userType, int userID, DateTime startDate, DateTime endDate,int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching patient appointments page {pageNumber} with size {pageSize}");

         
            var query = _context.PatientAppointments
                .AsNoTracking()
                .Where(a => a.StartDateTime >= startDate)
                .Where(a => a.EndDateTime <= endDate);
            switch(userType)
            {
                case UserType.Patient:
                    
                        query = query.Where(a => a.UserID == userID);
                        break;
                case UserType.Doctor:
                    query = query.Where(a => a.DoctorID == userID );
                    break;
                case UserType.Administrator:
                    break;
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            var results = await query
                    .OrderByDescending(a => a.StartDateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            var hasMoreRecords = (pageNumber * pageSize) < totalCount;
         
            var message = results.Count > 0 ? "Appointments found." : "No appointments found.";
            var response = new AppointmentSearchResponse
            {
                PatientAppointments = results,
                TotalCount = totalCount,
                HasMoreRecords = hasMoreRecords,
                Message = message
            };
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientAppointment>> Get(int id)
        {
            _logger.LogInformation($"Fetching patient appointment with ID: {id}");
            
            var entity = await _context.PatientAppointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ID == id)
                .ConfigureAwait(false);
            if (entity == null)
            {
                _logger.LogWarning($"Patient appointment with ID: {id} not found");
                return NotFound();
            }

            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<PatientAppointment>> Post(PatientAppointment appointment)
        {
            appointment.CreatedDate = DateTime.Now;
            appointment.ModifiedDate = DateTime.Now;
            appointment.IsActive = 1;
            
            _context.PatientAppointments.Add(appointment);
            await _context.SaveChangesAsync();
            
            // Clear cache for patient's appointments
         
            _logger.LogInformation($"Created new patient appointment with ID: {appointment.ID}");
            return CreatedAtAction(nameof(Get), new { id = appointment.ID }, appointment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, PatientAppointment appointment)
        {
            if (id != appointment.ID)
            {
                _logger.LogWarning($"Patient appointment ID mismatch: {id} != {appointment.ID}");
                return BadRequest();
            }

          

            appointment.ModifiedDate = DateTime.Now;
            _context.Entry(appointment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
           
            
            _logger.LogInformation($"Updated patient appointment with ID: {id}");
            return CreatedAtAction(nameof(Get), new { id = appointment.ID }, appointment); ;
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<PatientAppointment> patchDoc)
        {
            var entity = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.ID == id);
                
            if (entity == null)
            {
                _logger.LogWarning($"Patient appointment with ID: {id} not found for patch");
                return NotFound();
            }

            patchDoc.ApplyTo(entity);
            entity.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Patched patient appointment with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.PatientAppointments
                .FirstOrDefaultAsync(a => a.ID == id);
                
            if (entity == null)
            {
                _logger.LogWarning($"Patient appointment with ID: {id} not found for deletion");
                return NotFound();
            }

            _context.PatientAppointments.Remove(entity);
            await _context.SaveChangesAsync();
          
            _logger.LogInformation($"Deleted patient appointment with ID: {id}");
            return NoContent();
        }

        [HttpGet("doctor/{doctorId}")]
        public async Task<IActionResult> GetByDoctor(int doctorID, int pageNumber = 1, int pageSize = 10)
        {
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            if (roleClaim == "Patient")
            {
                return Forbid();
            }

            _logger.LogInformation($"Get all appointments for Doctor ID: {doctorID}");
            
            var cacheKey = $"appointments_doctor_{doctorID}";
           
            var appointments = await _context.PatientAppointments
                .AsNoTracking()
                .Where(a => a.DoctorID == doctorID && a.IsActive == 1)
                .OrderByDescending(a => a.StartDateTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize) // Limit results for performance
                .ToListAsync();
                
            if (appointments.Count == 0)
            {
                _logger.LogInformation($"No appointments found for doctor ID: {doctorID}");
                return NotFound();
            }
            return Ok(appointments);
        }

        [HttpGet("patient/{patientID}")]
        public async Task<IActionResult> GetByPatient(int patientID, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Get all appointments for Patient ID: {patientID}");
            
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            var userIdClaim = User.FindFirst("userid")?.Value;
            if (roleClaim == "Patient")
            {
                var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.ID == patientID);
                if (patient == null || patient.UserID.ToString() != userIdClaim)
                {
                    return Forbid();
                }
            }

            var cacheKey = $"appointments_patient_{patientID}_all";
          
            var appointments = await _context.PatientAppointments
                .AsNoTracking()
                .Where(a => a.PatientID == patientID)
                .OrderByDescending(a => a.StartDateTime)
                 .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize) // Limit results for performance
                .ToListAsync();
                
            if (appointments.Count == 0)
            {
                _logger.LogInformation($"No appointments found for patient ID: {patientID}");
                return NotFound();
            }
            return Ok(appointments);
        }



        [HttpPost("doctor/search")]
        public async Task<IActionResult> SearchAppointments([FromBody] SearchModel model)
        {
            _logger.LogInformation($"Searching appointments with criteria");
            try
            {
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
                var userIdClaim = User.FindFirst("userid")?.Value;

                var query = from appointment in _context.PatientAppointments
                            join user in _context.Users on appointment.UserID equals user.ID
                            join address in _context.Addresses on user.ID equals address.UserID into addressGroup
                            from address in addressGroup.DefaultIfEmpty()
                            join contact in _context.Contacts on user.ID equals contact.UserID into contactGroup
                            from contact in contactGroup.DefaultIfEmpty()
                            where user.IsActive == 1 &&
                                  (address == null || address.IsActive == 1) &&
                                  (contact == null || contact.IsActive == 1)
                            select new { appointment, user, address, contact };

                if (roleClaim == "Patient")
                {
                    if (int.TryParse(userIdClaim, out int loggedInUserId))
                    {
                        query = query.Where(x => x.appointment.UserID == loggedInUserId);
                    }
                    else
                    {
                        return Forbid();
                    }
                }

                // Apply filters
                if (!string.IsNullOrWhiteSpace(model.FirstName))
                    query = query.Where(x => x.user.FirstName.Contains(model.FirstName));
                if (!string.IsNullOrWhiteSpace(model.LastName))
                    query = query.Where(x => x.user.LastName.Contains(model.LastName));
                if (!string.IsNullOrWhiteSpace(model.PrimaryEmail))
                    query = query.Where(x => x.contact != null && x.contact.PrimaryEmail!.Contains(model.PrimaryEmail));
                if (!string.IsNullOrWhiteSpace(model.PrimaryPhone))
                    query = query.Where(x => x.contact != null && x.contact.PrimaryPhone!.Contains(model.PrimaryPhone));
                if (!string.IsNullOrWhiteSpace(model.PermCity))
                    query = query.Where(x => x.address != null && x.address.PermCity!.Contains(model.PermCity));
                if (model.DoctorID > 0)
                    query = query.Where(x => x.appointment.DoctorID == model.DoctorID);
                if (model.PatientID > 0)
                    query = query.Where(x => x.appointment.PatientID == model.PatientID);
                if (!string.IsNullOrWhiteSpace(model.DoctorName))
                    query = query.Where(x => x.appointment.DoctorName!.Contains(model.DoctorName));
                if (model.StartDate.HasValue)
                    query = query.Where(x => x.appointment.StartDateTime >= model.StartDate.Value);

                // Get total count before pagination
                var totalCount = await query
                    .Select(x => x.appointment)
                    .Distinct()
                    .CountAsync();
                var results = await query
                    .Select(x => x.appointment)
                    .Distinct()
                    .AsNoTracking()
                    .OrderByDescending(a => a.StartDateTime)
                    .Skip((model.pageNumber - 1) * model.pageSize)
                    .Take(model.pageSize)
                    .ToListAsync();
                var hasMoreRecords = (model.pageNumber * model.pageSize) < totalCount;
                var message = results.Count > 0 ? "Appointments found." : "No appointments found.";
                var response = new AppointmentSearchResponse
                {
                    PatientAppointments = results,
                    TotalCount = totalCount,
                    HasMoreRecords = hasMoreRecords,
                    Message = message
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching appointments");
                return StatusCode(500, "An error occurred while searching appointments");
            }
        }

     
    }

    public class AppointmentSearchResponse 
    {
        public int TotalCount { get; set; }
        public bool HasMoreRecords { get; set; }
        public string? Message { get; set; }
        public IEnumerable<PatientAppointment>? PatientAppointments { get; set; }

    }
}
