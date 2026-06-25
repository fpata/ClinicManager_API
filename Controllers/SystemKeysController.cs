using ClinicManager.DAL;
using ClinicManager.Models;
using ClinicManager.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemKeysController : ControllerBase
    {
        private readonly ClinicDbContext _context;

        public SystemKeysController(ClinicDbContext context)
        {
            _context = context;
        }

        // Request DTO to accept plain-text values
        public class KeyRequest
        {
            public string KeyName { get; set; } = string.Empty;
            public string KeyValue { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> Get()
        {
            var keys = await _context.SystemKeys.AsNoTracking().ToListAsync();
            var maskedKeys = keys.Select(k => new
            {
                k.ID,
                k.KeyName,
                KeyValue = "********", // Mask value for security
                k.CreatedDate,
                k.ModifiedDate,
                k.CreatedBy,
                k.ModifiedBy,
                k.IsActive
            });
            return Ok(maskedKeys);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> Get(int id)
        {
            var key = await _context.SystemKeys.AsNoTracking().FirstOrDefaultAsync(k => k.ID == id);
            if (key == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                key.ID,
                key.KeyName,
                KeyValue = "********", // Mask value for security
                key.CreatedDate,
                key.ModifiedDate,
                key.CreatedBy,
                key.ModifiedBy,
                key.IsActive
            });
        }

        [HttpPost]
        public async Task<ActionResult<SystemKey>> Post(KeyRequest request)
        {
            if (string.IsNullOrEmpty(request.KeyName) || string.IsNullOrEmpty(request.KeyValue))
            {
                return BadRequest("KeyName and KeyValue are required.");
            }

            // Check if key already exists
            var existingKey = await _context.SystemKeys.FirstOrDefaultAsync(k => k.KeyName == request.KeyName);
            if (existingKey != null)
            {
                // Update instead of insert
                existingKey.KeyValue = EncryptionHelper.Encrypt(request.KeyValue);
                existingKey.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(existingKey);
            }

            var systemKey = new SystemKey
            {
                KeyName = request.KeyName,
                KeyValue = EncryptionHelper.Encrypt(request.KeyValue),
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                IsActive = 1
            };

            _context.SystemKeys.Add(systemKey);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = systemKey.ID }, systemKey);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, KeyRequest request)
        {
            var existingKey = await _context.SystemKeys.FindAsync(id);
            if (existingKey == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.KeyName))
            {
                existingKey.KeyName = request.KeyName;
            }

            if (!string.IsNullOrEmpty(request.KeyValue))
            {
                existingKey.KeyValue = EncryptionHelper.Encrypt(request.KeyValue);
            }

            existingKey.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var key = await _context.SystemKeys.FindAsync(id);
            if (key == null)
            {
                return NotFound();
            }

            _context.SystemKeys.Remove(key);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
