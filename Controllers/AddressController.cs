using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<AddressController> _logger;
        public AddressController(ClinicDbContext context, ILogger<AddressController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Address>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching addresses page {pageNumber} with size {pageSize}");
            var addresses = await _context.Addresses
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return addresses;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Address>> Get(int id)
        {
            _logger.LogInformation($"Fetching address with ID: {id}");
            var entity = await _context.Addresses.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Address with ID: {id} not found");
                return NotFound();
            }
            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<Address>> Post(Address address)
        {
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Created new address with ID: {address.ID}");
            return CreatedAtAction(nameof(Get), new { id = address.ID }, address);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Address address)
        {
            if (id != address.ID)
            {
                _logger.LogWarning($"Address ID mismatch: {id} != {address.ID}");
                return BadRequest();
            }
            _context.Entry(address).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated address with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<Address> patchDoc)
        {
            var entity = await _context.Addresses.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Address with ID: {id} not found for patch");
                return NotFound();
            }
            patchDoc.ApplyTo(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Patched address with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Addresses.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Address with ID: {id} not found for deletion");
                return NotFound();
            }
            _context.Addresses.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted address with ID: {id}");
            return NoContent();
        }
    }
}
