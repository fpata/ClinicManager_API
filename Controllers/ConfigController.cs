using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppConfigController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<AppConfigController> _logger;

        public AppConfigController(ClinicDbContext context, ILogger<AppConfigController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<AppConfig>> Get()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE config MODIFY COLUMN ClinicLogo LONGTEXT NULL;");
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "Could not run self-healing DB alter for ClinicLogo. It might not be MySQL or the column/table is not ready.");
            }

            var config = await _context.AppConfigs.AsNoTracking().FirstOrDefaultAsync();
            if (config == null)
            {
                config = new AppConfig
                {
                    ClinicName = "Relief Dental Clinic",
                    ClinicOpenTime = "09:00",
                    ClinicEndTime = "18:00",
                    PerPatientSlotInMinutes = 15,
                    LunchTime = "13:00",
                    pageSize = 10,
                    DateFormat = "MM/dd/yyyy",
                    Currency = "USD",
                    ClinicLogo = "",
                    ClinicAddress = "123 Main Street, Suite A"
                };
                _context.AppConfigs.Add(config);
                await _context.SaveChangesAsync();
            }
            return Ok(config);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppConfig>> Get(int id)
        {
            var config = await _context.AppConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.ID == id);
            if (config == null)
                return NotFound();
            return Ok(config);
        }

        [HttpPost]
        public async Task<ActionResult<AppConfig>> Post(AppConfig config)
        {
            _context.AppConfigs.Add(config);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = config.ID }, config);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, AppConfig config)
        {
            if (id != config.ID)
                return BadRequest();

            _context.Entry(config).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var config = await _context.AppConfigs.FindAsync(id);
            if (config == null)
                return NotFound();

            _context.AppConfigs.Remove(config);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}