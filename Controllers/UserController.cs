using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<UserController> _logger;
       
        private const int CACHE_EXPIRY_MINUTES = 10;

        public UserController(ClinicDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> Get(int pageNumber = 1, int pageSize = 20)
        {
            _logger.LogInformation($"Fetching users page {pageNumber} with size {pageSize}");
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive == 1)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync()
                .ConfigureAwait(false);

            return users;
        }

        // Updated to use async EF Core call and proper null handling
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(int id)
        {
            // Pseudocode:
            // 1. Log request
            // 2. Query Users with Includes asynchronously using FirstOrDefaultAsync
            // 3. If null -> return NotFound
            // 4. Log fetched details
            // 5. Return entity (implicit 200 OK)
            _logger.LogInformation("Fetching user with ID: {Id}", id);

            var entity = await _context.Users
                .AsNoTracking()
                .Include(u => u.Address)
                .Include(u => u.Contact)
                .FirstOrDefaultAsync(u => u.ID == id && u.IsActive == 1)
                .ConfigureAwait(false);

            if (entity == null)
            {
                _logger.LogWarning("User with ID: {Id} not found", id);
                return NotFound();
            }

            Console.WriteLine($"Fetched user with ID: {id}, Address: {entity.Address?.ID}, Contact: {entity.Contact?.ID}");
            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<User>> Post(User user)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Set timestamps
                user.CreatedDate = DateTime.Now;
                user.ModifiedDate = DateTime.Now;
                user.CreatedBy = user.CreatedBy ?? 1; // Default to 1 if not set
                user.ModifiedBy = user.ModifiedBy ?? 1; // Default to 1 if not set
                user.IsActive = 1;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Process related entities in batch
                var entitiesToAdd = new List<object>();

                if (user.Address != null && user.Address.ID  <= 0 )
                {
                    user.Address.ID = 0; // Reset ID to ensure new record is created
                    user.Address.UserID = user.ID;
                    user.Address.CreatedDate = DateTime.Now;
                    user.Address.ModifiedDate = DateTime.Now;
                    user.CreatedBy = user.CreatedBy ?? 1; // Default to 1 if not set
                    user.ModifiedBy = user.ModifiedBy ?? 1; // Default to 1 if not set
                    user.Address.IsActive = 1;
                    entitiesToAdd.Add(user.Address);
                }

                if (user.Contact != null && user.Contact.ID <= 0)
                {
                    user.Contact.ID = 0; // Reset ID to ensure new record is created
                    user.Contact.UserID = user.ID;
                    user.Contact.CreatedDate = DateTime.Now;
                    user.Contact.ModifiedDate = DateTime.Now;
                    user.Contact.CreatedBy = user.CreatedBy ?? 1; // Default to 1 if not set
                    user.Contact.ModifiedBy = user.ModifiedBy ?? 1; // Default to 1 if not set
                    user.Contact.IsActive = 1;
                    entitiesToAdd.Add(user.Contact);
                }

                // Add all related entities in batch
                if (entitiesToAdd.Any())
                {
                    _context.AddRange(entitiesToAdd);
                    await _context.SaveChangesAsync();
                }

                await dbTransaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error while creating user");
            }

            _logger.LogInformation($"Created new user with ID: {user.ID}");
            // Clear cache for all users
           
            user =  Get(user.ID).Result.Value!; // Refresh user to get full details including related entities

            return Created("Get",user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, User? user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            if (id != user?.ID)
            {
                _logger.LogWarning($"User ID mismatch: {id} != {user?.ID}");
                return BadRequest();
            }


            try
            {
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = user.ModifiedBy ?? 1; // Default to 1 if not set
                // Update user
                _context.Entry(user).State = EntityState.Modified;

                // Update related entities if they exist
                if (user.Address != null)
                {
                    user.Address.ModifiedDate = DateTime.Now;
                    if (user.Address.ID > 0)
                    {
                        _context.Entry(user.Address).State = EntityState.Modified;
                    }
                    else
                    {
                        user.Address.UserID = user.ID;
                        user.Address.CreatedDate = user.Address.CreatedDate ?? DateTime.Now;
                        user.Address.ModifiedDate = DateTime.Now;
                        user.Address.CreatedBy = user.CreatedBy ?? 1; // Default to 1 if not set
                        user.Address.ModifiedBy = user.ModifiedBy ?? 1; // Default to 1 if not set
                        user.Address.IsActive = 1;
                        _context.Addresses.Add(user.Address);
                    }
                }

                if (user.Contact != null)
                {
                    user.Contact.ModifiedDate = DateTime.Now;
                    if (user.Contact.ID > 0)
                    {
                        _context.Entry(user.Contact).State = EntityState.Modified;
                    }
                    else
                    {
                        user.Contact.UserID = user.ID;
                        user.Contact.CreatedDate = user.CreatedDate ?? DateTime.Now;
                        user.Contact.ModifiedDate = DateTime.Now;
                        user.Contact.IsActive = 1;
                        user.Contact.CreatedBy = user.CreatedBy ?? 1; // Default to 1 if not set
                        user.Contact.ModifiedBy = user.ModifiedBy ?? 1; // Default to 1 if not set
                        _context.Contacts.Add(user.Contact);
                    }
                }
              
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation($"Updated user with ID: {id}");
                user = Get(id).Result?.Value;
                return Ok(user);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error updating user with ID: {id}");
                return StatusCode(500, "Internal server error while updating user");
            }

        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<User> patchDoc)
        {
            var entity = await _context.Users
                .FirstOrDefaultAsync(u => u.ID == id && u.IsActive == 1)
                .ConfigureAwait(false);
                
            if (entity == null)
            {
                _logger.LogWarning($"User with ID: {id} not found for patch");
                return NotFound();
            }

            patchDoc.ApplyTo(entity);
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
                       
            _logger.LogInformation($"Patched user with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Users
                .FirstOrDefaultAsync(u => u.ID == id && u.IsActive==1);
                
            if (entity == null)
            {
                _logger.LogWarning($"User with ID: {id} not found for deletion");
                return NotFound();
            }

            // Soft delete instead of hard delete
            entity.IsActive = 0;
            entity.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
                        
            _logger.LogInformation($"Soft deleted user with ID: {id}");
            return NoContent();
        }

        [HttpGet("access")]
        public async Task<IActionResult> GetAccess()
        {
            var userIdClaim = User.FindFirst("userid")?.Value;
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;

            if (string.IsNullOrEmpty(roleClaim))
            {
                return Unauthorized();
            }

            var roleAccess = await _context.RoleAccesses.AsNoTracking().FirstOrDefaultAsync(ra => ra.RoleName == roleClaim);

            var permissions = new Dictionary<string, bool>
            {
                { "Patient", roleAccess?.CanAccessPatient ?? (roleClaim == "Administrator" || roleClaim == "Doctor" || roleClaim == "Nurse" || roleClaim == "Accountant" || roleClaim == "Patient") },
                { "Dashboard", roleAccess?.CanAccessDashboard ?? (roleClaim == "Administrator" || roleClaim == "Doctor" || roleClaim == "Nurse") },
                { "Billing", roleAccess?.CanAccessBilling ?? (roleClaim == "Administrator" || roleClaim == "Doctor" || roleClaim == "Accountant") },
                { "Config", roleAccess?.CanAccessConfig ?? (roleClaim == "Administrator" || roleClaim == "Doctor") }
            };

            return Ok(new
            {
                userId = userIdClaim,
                role = roleClaim,
                permissions = permissions
            });
        }
    }
}
