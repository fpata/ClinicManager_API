using ClinicManager.DAL;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,Doctor,Accountant")]
    public class PaymentController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ClinicDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> Get(int? billingId = null, int pageNumber = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Fetching payments: billingId={billingId}, page={pageNumber}, size={pageSize}");
            
            IQueryable<Payment> query = _context.Payments;
            
            if (billingId.HasValue && billingId.Value != 0)
            {
                query = query.Where(p => p.BillingID == billingId.Value);
            }

            var payments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return payments;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> Get(int id)
        {
            _logger.LogInformation($"Fetching payment with ID: {id}");
            var entity = await _context.Payments.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Payment with ID: {id} not found");
                return NotFound();
            }
            return entity;
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> Post(Payment payment)
        {
            _logger.LogInformation($"Creating payment for BillingID: {payment.BillingID}, Amount: {payment.Amount}");
            
            payment.CreatedDate = DateTime.Now;
            payment.ModifiedDate = DateTime.Now;
            payment.IsActive = 1;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await UpdateBillingRecordAsync(payment.BillingID);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created payment with ID: {payment.ID} and updated billing record");
            return CreatedAtAction(nameof(Get), new { id = payment.ID }, payment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, Payment payment)
        {
            if (id != payment.ID)
            {
                _logger.LogWarning($"Payment ID mismatch: {id} != {payment.ID}");
                return BadRequest();
            }

            payment.ModifiedDate = DateTime.Now;
            _context.Entry(payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            await UpdateBillingRecordAsync(payment.BillingID);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated payment with ID: {id} and updated billing record");
            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, JsonPatchDocument<Payment> patchDoc)
        {
            var entity = await _context.Payments.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Payment with ID: {id} not found for patch");
                return NotFound();
            }

            patchDoc.ApplyTo(entity);
            entity.ModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            await UpdateBillingRecordAsync(entity.BillingID);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Patched payment with ID: {id} and updated billing record");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Payments.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning($"Payment with ID: {id} not found for delete");
                return NotFound();
            }

            _context.Payments.Remove(entity);
            await _context.SaveChangesAsync();

            await UpdateBillingRecordAsync(entity.BillingID);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted payment with ID: {id} and updated billing record");
            return NoContent();
        }

        private async Task UpdateBillingRecordAsync(int? billingId)
        {
            if (billingId == null || billingId == 0) return;

            var billing = await _context.BillingRecords.FindAsync(billingId);
            if (billing != null)
            {
                var payments = await _context.Payments
                    .Where(p => p.BillingID == billingId && p.IsActive != 0)
                    .ToListAsync();

                float amountPaid = payments.Sum(p => p.Amount ?? 0);
                billing.AmountPaid = amountPaid;
                billing.BalanceDue = (billing.Total ?? 0) - amountPaid;

                if (billing.BalanceDue <= 0)
                {
                    billing.Status = BillingStatus.Paid;
                }
                else if (billing.AmountPaid > 0)
                {
                    billing.Status = BillingStatus.PartiallyPaid;
                }
                else
                {
                    billing.Status = BillingStatus.Submitted;
                }
                _context.Entry(billing).State = EntityState.Modified;
            }
        }
    }
}
