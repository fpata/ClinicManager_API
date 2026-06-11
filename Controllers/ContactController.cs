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
    public class ContactController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<ContactController> _logger;
        public ContactController(ClinicDbContext context, ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contact>>> Get(int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching contacts page {pageNumber} with size {pageSize}");
            var contacts = await _context.Contacts
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return contacts;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Contact>> Get(int id)
        {
            _logger.LogInformation($"Fetching contact with ID: {id}");
            var entity = await _context.Contacts.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Contact with ID: {id} not found");
                return NotFound();
            }
            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<Contact>> Post(Contact contact)
        {
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Created new contact with ID: {contact.ID}");
            return CreatedAtAction(nameof(Get), new { id = contact.ID }, contact);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Contact contact)
        {
            if (id != contact.ID)
            {
                _logger.LogWarning($"Contact ID mismatch: {id} != {contact.ID}");
                return BadRequest();
            }
            _context.Entry(contact).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Updated contact with ID: {id}");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<Contact> patchDoc)
        {
            var entity = await _context.Contacts.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Contact with ID: {id} not found for patch");
                return NotFound();
            }
            patchDoc.ApplyTo(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Patched contact with ID: {id}");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Contacts.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Contact with ID: {id} not found for deletion");
                return NotFound();
            }
            _context.Contacts.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Deleted contact with ID: {id}");
            return NoContent();
        }
    }
}
