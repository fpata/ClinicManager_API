using ClinicManager.DAL;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ClinicDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsAdminOrStaff()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("usertype")?.Value;
            return role == "Administrator" || role == "Doctor" || role == "Nurse" || role == "Accountant";
        }

        private bool IsAuthorizedForPatient(int? patientUserId)
        {
            if (IsAdminOrStaff()) return true;
            var userIdClaim = User.FindFirst("userid")?.Value;
            return userIdClaim != null && patientUserId != null && userIdClaim == patientUserId.ToString();
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            _logger.LogInformation($"Generating Revenue Report. Start: {startDate}, End: {endDate}");
            if (!IsAdminOrStaff()) return Forbid();

            var query = from p in _context.Payments.AsNoTracking()
                        join b in _context.BillingRecords.AsNoTracking() on p.BillingID equals b.ID into billingJoin
                        from b in billingJoin.DefaultIfEmpty()
                        select new
                        {
                            PaymentId = p.ID,
                            TransactionDateStr = p.TransactionDate,
                            Amount = p.Amount,
                            PaymentMethod = p.PaymentMethod,
                            Reference = p.Reference,
                            Notes = p.Notes,
                            PatientName = b != null ? b.PatientName : "N/A",
                            DoctorName = b != null ? b.DoctorName : "N/A",
                            TreatmentName = b != null ? b.TreatmentName : "N/A",
                            BillingTotal = b != null ? b.Total : 0
                        };

            var results = await query.ToListAsync();

            if (startDate.HasValue)
            {
                results = results.Where(r => {
                    if (DateTime.TryParse(r.TransactionDateStr, out DateTime dt))
                        return dt.Date >= startDate.Value.Date;
                    return false;
                }).ToList();
            }
            if (endDate.HasValue)
            {
                results = results.Where(r => {
                    if (DateTime.TryParse(r.TransactionDateStr, out DateTime dt))
                        return dt.Date <= endDate.Value.Date;
                    return false;
                }).ToList();
            }

            var csv = new StringBuilder();
            csv.AppendLine("Payment ID,Transaction Date,Patient Name,Doctor Name,Treatment Name,Billing Total,Amount Paid,Payment Method,Reference,Notes");
            
            foreach (var r in results)
            {
                var dateFormatted = "";
                if (DateTime.TryParse(r.TransactionDateStr, out DateTime dt))
                {
                    dateFormatted = dt.ToString("yyyy-MM-dd HH:mm");
                }
                else
                {
                    dateFormatted = r.TransactionDateStr ?? "";
                }

                csv.AppendLine($"{r.PaymentId}," +
                               $"\"{EscapeCsv(dateFormatted)}\"," +
                               $"\"{EscapeCsv(r.PatientName)}\"," +
                               $"\"{EscapeCsv(r.DoctorName)}\"," +
                               $"\"{EscapeCsv(r.TreatmentName)}\"," +
                               $"{r.BillingTotal}," +
                               $"{r.Amount}," +
                               $"\"{r.PaymentMethod}\"," +
                               $"\"{EscapeCsv(r.Reference)}\"," +
                               $"\"{EscapeCsv(r.Notes)}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"revenue_report_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet("outstanding-balances")]
        public async Task<IActionResult> GetOutstandingBalancesReport()
        {
            _logger.LogInformation("Generating Outstanding Balances Report");
            if (!IsAdminOrStaff()) return Forbid();

            var billings = await _context.BillingRecords
                .AsNoTracking()
                .Where(b => b.BalanceDue > 0 && b.IsActive == 1 && b.Status != BillingStatus.Voided)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Billing ID,Patient Name,Doctor Name,Treatment Name,Service Date,Status,Subtotal,Tax,Discount,Total Billing,Amount Paid,Balance Due,Days Outstanding");

            foreach (var b in billings)
            {
                var serviceDateFormatted = b.ServiceDate?.ToString("yyyy-MM-dd") ?? "";
                var createdDate = b.CreatedDate ?? DateTime.Now;
                var daysOutstanding = (int)(DateTime.Now - createdDate).TotalDays;

                csv.AppendLine($"{b.ID}," +
                               $"\"{EscapeCsv(b.PatientName)}\"," +
                               $"\"{EscapeCsv(b.DoctorName)}\"," +
                               $"\"{EscapeCsv(b.TreatmentName)}\"," +
                               $"\"{serviceDateFormatted}\"," +
                               $"\"{b.Status}\"," +
                               $"{b.Subtotal ?? 0}," +
                               $"{b.TaxTotal ?? 0}," +
                               $"{b.DiscountTotal ?? 0}," +
                               $"{b.Total ?? 0}," +
                               $"{b.AmountPaid ?? 0}," +
                               $"{b.BalanceDue ?? 0}," +
                               $"{daysOutstanding}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"outstanding_balances_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointmentsReport(
            [FromQuery] DateTime? startDate, 
            [FromQuery] DateTime? endDate, 
            [FromQuery] string? status, 
            [FromQuery] int? doctorId)
        {
            _logger.LogInformation($"Generating Appointments Report. Start: {startDate}, End: {endDate}, Status: {status}, DoctorId: {doctorId}");
            if (!IsAdminOrStaff()) return Forbid();

            var query = _context.PatientAppointments.AsNoTracking().Where(a => a.IsActive == 1);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.StartDateTime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                var endOfEndDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.StartDateTime <= endOfEndDate);
            }
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(a => a.AppointmentStatus == status);
            }
            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorID == doctorId.Value);
            }

            var appointments = await query.OrderBy(a => a.StartDateTime).ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Appointment ID,Date & Time,Patient Name,Doctor Name,Treatment Name,Status,Check-In Time,Check-Out Time,Cancellation Reason,Notes");

            foreach (var a in appointments)
            {
                var dateTimeStr = a.StartDateTime?.ToString("yyyy-MM-dd HH:mm") ?? "";
                var checkInStr = a.CheckInTime?.ToString("yyyy-MM-dd HH:mm") ?? "";
                var checkOutStr = a.CheckOutTime?.ToString("yyyy-MM-dd HH:mm") ?? "";

                csv.AppendLine($"{a.ID}," +
                               $"\"{dateTimeStr}\"," +
                               $"\"{EscapeCsv(a.PatientName)}\"," +
                               $"\"{EscapeCsv(a.DoctorName)}\"," +
                               $"\"{EscapeCsv(a.TreatmentName)}\"," +
                               $"\"{a.AppointmentStatus}\"," +
                               $"\"{checkInStr}\"," +
                               $"\"{checkOutStr}\"," +
                               $"\"{EscapeCsv(a.CancellationReason)}\"," +
                               $"\"{EscapeCsv(a.Notes)}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"appointments_report_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet("patient-medical-history/{patientId}")]
        public async Task<IActionResult> GetPatientMedicalHistory(int patientId)
        {
            _logger.LogInformation($"Generating Medical History RTF for Patient ID: {patientId}");

            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.PatientAppointments)
                .Include(p => p.PatientVitals)
                .Include(p => p.PatientTreatment)
                    .ThenInclude(pt => pt!.PatientTreatmentDetails)
                .FirstOrDefaultAsync(p => p.ID == patientId && p.IsActive == 1);

            if (patient == null)
            {
                return NotFound($"Patient with ID {patientId} not found.");
            }

            if (!IsAuthorizedForPatient(patient.UserID))
            {
                return Forbid();
            }

            var patientUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Contact)
                .Include(u => u.Address)
                .FirstOrDefaultAsync(u => u.ID == patient.UserID && u.IsActive == 1);

            var clinicConfig = await _context.AppConfigs.FirstOrDefaultAsync(c => c.IsActive == 1);
            string clinicName = clinicConfig?.ClinicName ?? "Clinic Manager";
            string clinicProp = clinicConfig?.ClinicProp ?? "Premium Oral Care & Dental Clinic";

            string patientName = patientUser?.FullName ?? "N/A";
            string patientAge = patientUser?.Age?.ToString() ?? "N/A";
            string patientGender = patientUser?.Gender?.ToString() ?? "N/A";
            string patientPhone = patientUser?.Contact?.PrimaryPhone ?? "N/A";
            string patientEmail = patientUser?.Contact?.PrimaryEmail ?? "N/A";
            string patientAllergies = patient.Allergies ?? "No known allergies";
            string patientMedications = patient.Medications ?? "None";
            string personalHistory = patient.PersonalMedicalHistory ?? "None";
            string familyHistory = $"Father: {patient.FatherMedicalHistory ?? "None"}, Mother: {patient.MotherMedicalHistory ?? "None"}";

            var rtf = new StringBuilder();
            rtf.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            rtf.AppendLine(@"{\fonttbl{\f0\fnil\fcharset0 Calibri;}{\f1\fnil\fcharset0 Calibri-Bold;}}");
            rtf.AppendLine(@"{\colortbl ;\red26\green54\blue93;\red45\green55\blue72;\red113\green128\blue150;\red226\green232\blue240;}");
            rtf.AppendLine(@"\margl1440\margr1440\margt1440\margb1440");
            rtf.AppendLine(@"\viewkind4\uc1\pard\plain\f0\fs22\cf2");

            if (clinicConfig != null && !string.IsNullOrEmpty(clinicConfig.ClinicLogo))
            {
                string logoRtf = ClinicManager.Services.PrescriptionService.GetImageRtfCode(clinicConfig.ClinicLogo);
                if (!string.IsNullOrEmpty(logoRtf))
                {
                    rtf.AppendLine(logoRtf);
                }
            }
            rtf.AppendLine(@"\qc\fs36\b\cf1 " + EscapeRtf(clinicName) + @"\b0\par");
            if (!string.IsNullOrEmpty(clinicProp))
            {
                rtf.AppendLine(@"\fs20\cf3 " + EscapeRtf(clinicProp) + @"\par");
            }
            if (clinicConfig != null && !string.IsNullOrEmpty(clinicConfig.ClinicAddress))
            {
                rtf.AppendLine(@"\fs16\cf3 " + EscapeRtf(clinicConfig.ClinicAddress) + @"\par");
            }
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw15\brsp80\cf4\par\par");

            rtf.AppendLine(@"\qc\fs28\b\cf1 PATIENT COMPREHENSIVE MEDICAL HISTORY\b0\fs22\cf2\par\par");

            rtf.AppendLine(@"\pard\plain\f0\fs24\b\cf1 1. Patient Profile\b0\fs22\cf2\par");
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw10\brsp40\cf4\par");
            rtf.AppendLine(@"\tx4000\tx4500\tx8000");
            rtf.AppendLine(@"\b Patient ID:\b0  " + patient.ID + @"\tab\b Phone:\b0  " + EscapeRtf(patientPhone) + @"\par");
            rtf.AppendLine(@"\b Full Name:\b0  " + EscapeRtf(patientName) + @"\tab\b Email:\b0  " + EscapeRtf(patientEmail) + @"\par");
            rtf.AppendLine(@"\b Age / Gender:\b0  " + EscapeRtf($"{patientAge} / {patientGender}") + @"\tab\b Insurance:\b0  " + EscapeRtf($"{patient.InsuranceProvider ?? "N/A"} ({patient.InsurancePolicyNumber ?? "N/A"})") + @"\par\par");

            rtf.AppendLine(@"\pard\plain\f0\fs24\b\cf1 2. Medical Background & Allergies\b0\fs22\cf2\par");
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw10\brsp40\cf4\par");
            rtf.AppendLine(@"\b Drug/Food Allergies:\b0  " + EscapeRtf(patientAllergies) + @"\par");
            rtf.AppendLine(@"\b Current Medications:\b0  " + EscapeRtf(patientMedications) + @"\par");
            rtf.AppendLine(@"\b Personal Medical History:\b0  " + EscapeRtf(personalHistory) + @"\par");
            rtf.AppendLine(@"\b Family Medical History:\b0  " + EscapeRtf(familyHistory) + @"\par\par");

            rtf.AppendLine(@"\pard\plain\f0\fs24\b\cf1 3. Recorded Patient Vitals Log\b0\fs22\cf2\par");
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw10\brsp40\cf4\par");
            var vitals = patient.PatientVitals?.Where(v => v.IsActive == 1).OrderByDescending(v => v.CreatedDate).ToList();
            if (vitals != null && vitals.Any())
            {
                rtf.AppendLine(@"\tx1500\tx3000\tx4500\tx6000\tx7500");
                rtf.AppendLine(@"\b Date\tab BP (mmHg)\tab Pulse (bpm)\tab Temp (F)\tab Weight (kg)\tab Height (cm)\b0\par");
                rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw5\brsp40\cf4\par");
                foreach (var v in vitals)
                {
                    var dateStr = v.CreatedDate?.ToString("yyyy-MM-dd") ?? "-";
                    var bpStr = (v.BloodPressureSystolic.HasValue && v.BloodPressureDiastolic.HasValue) 
                        ? $"{v.BloodPressureSystolic.Value}/{v.BloodPressureDiastolic.Value}" 
                        : "-";
                    rtf.AppendLine(@"\pard\plain\f0\fs20\cf2\tx1500\tx3000\tx4500\tx6000\tx7500 " +
                        EscapeRtf(dateStr) + @"\tab " +
                        EscapeRtf(bpStr) + @"\tab " +
                        (v.HeartRate.HasValue ? v.HeartRate.Value.ToString() : "-") + @"\tab " +
                        EscapeRtf(v.Temperature ?? "-") + @"\tab " +
                        EscapeRtf(v.Weight ?? "-") + @"\tab " +
                        EscapeRtf(v.Height ?? "-") + @"\par");
                }
            }
            else
            {
                rtf.AppendLine(@"No vitals recorded.\par");
            }
            rtf.AppendLine(@"\par");

            rtf.AppendLine(@"\pard\plain\f0\fs24\b\cf1 4. Treatment and Procedures History\b0\fs22\cf2\par");
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw10\brsp40\cf4\par");
            if (patient.PatientTreatment != null && patient.PatientTreatment.IsActive == 1)
            {
                var pt = patient.PatientTreatment;
                rtf.AppendLine(@"\b Chief Complaint:\b0  " + EscapeRtf(pt.ChiefComplaint) + @"\par");
                rtf.AppendLine(@"\b Diagnosis:\b0  " + EscapeRtf(pt.Diagnosis) + @"\par");
                rtf.AppendLine(@"\b Treatment Plan:\b0  " + EscapeRtf(pt.TreatmentPlan) + @"\par");
                rtf.AppendLine(@"\b Clinical Findings:\b0  " + EscapeRtf(pt.ClinicalFindings) + @"\par");
                rtf.AppendLine(@"\b General Prescriptions:\b0  " + EscapeRtf(pt.Prescription) + @"\par\par");

                var details = pt.PatientTreatmentDetails?.Where(d => d.IsActive == 1).OrderBy(d => d.TreatmentDate).ToList();
                if (details != null && details.Any())
                {
                    rtf.AppendLine(@"\tx1500\tx2500\tx5500");
                    rtf.AppendLine(@"\b Date\tab Tooth\tab Procedure Performed\tab Detail Prescription\b0\par");
                    rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw5\brsp40\cf4\par");
                    foreach (var d in details)
                    {
                        var dDate = d.TreatmentDate?.ToString("yyyy-MM-dd") ?? "-";
                        rtf.AppendLine(@"\pard\plain\f0\fs20\cf2\tx1500\tx2500\tx5500 " +
                            EscapeRtf(dDate) + @"\tab " +
                            EscapeRtf(d.Tooth ?? "-") + @"\tab " +
                            EscapeRtf(d.Procedure ?? "-") + @"\tab " +
                            EscapeRtf(d.Prescription ?? "-") + @"\par");
                    }
                }
            }
            else
            {
                rtf.AppendLine(@"No active treatment records found.\par");
            }
            rtf.AppendLine(@"\par");

            rtf.AppendLine(@"\pard\plain\f0\fs24\b\cf1 5. Appointment Attendance Log\b0\fs22\cf2\par");
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw10\brsp40\cf4\par");
            var appointments = patient.PatientAppointments?.Where(a => a.IsActive == 1).OrderByDescending(a => a.StartDateTime).ToList();
            if (appointments != null && appointments.Any())
            {
                rtf.AppendLine(@"\tx2500\tx4500\tx6000");
                rtf.AppendLine(@"\b Date & Time\tab Doctor Name\tab Treatment Type\tab Status\b0\par");
                rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw5\brsp40\cf4\par");
                foreach (var a in appointments)
                {
                    var aDate = a.StartDateTime?.ToString("yyyy-MM-dd HH:mm") ?? "-";
                    rtf.AppendLine(@"\pard\plain\f0\fs20\cf2\tx2500\tx4500\tx6000 " +
                        EscapeRtf(aDate) + @"\tab " +
                        EscapeRtf(a.DoctorName ?? "-") + @"\tab " +
                        EscapeRtf(a.TreatmentName ?? "-") + @"\tab " +
                        EscapeRtf(a.AppointmentStatus ?? "-") + @"\par");
                }
            }
            else
            {
                rtf.AppendLine(@"No appointment history found.\par");
            }

            rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par\par\par\par");
            rtf.AppendLine(@"\qr\b Report Generated Date:\b0  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + @"\par");
            rtf.AppendLine(@"\qr\fs16\cf3 Clinic Records Department\par");

            rtf.AppendLine(@"}");

            var bytes = Encoding.ASCII.GetBytes(rtf.ToString());
            return File(bytes, "application/rtf", $"medical_history_{patientId}_{DateTime.Now:yyyyMMdd}.rtf");
        }

        [HttpGet("referral-letter")]
        public async Task<IActionResult> GetReferralLetter(
            [FromQuery] int patientId,
            [FromQuery] string referredToDoctor,
            [FromQuery] string referredToClinic,
            [FromQuery] string reason)
        {
            _logger.LogInformation($"Generating Referral Letter RTF for Patient ID: {patientId}");
            if (!IsAdminOrStaff()) return Forbid();

            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ID == patientId && p.IsActive == 1);

            if (patient == null)
            {
                return NotFound($"Patient with ID {patientId} not found.");
            }

            var patientUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID == patient.UserID && u.IsActive == 1);

            var clinicConfig = await _context.AppConfigs.FirstOrDefaultAsync(c => c.IsActive == 1);
            string clinicName = clinicConfig?.ClinicName ?? "Clinic Manager";
            string clinicProp = clinicConfig?.ClinicProp ?? "Premium Oral Care & Dental Clinic";

            string patientName = patientUser?.FullName ?? "N/A";
            string patientAge = patientUser?.Age?.ToString() ?? "N/A";
            string patientGender = patientUser?.Gender?.ToString() ?? "N/A";

            var rtf = new StringBuilder();
            rtf.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            rtf.AppendLine(@"{\fonttbl{\f0\fnil\fcharset0 Calibri;}{\f1\fnil\fcharset0 Calibri-Bold;}}");
            rtf.AppendLine(@"{\colortbl ;\red26\green54\blue93;\red45\green55\blue72;\red113\green128\blue150;\red226\green232\blue240;}");
            rtf.AppendLine(@"\margl1440\margr1440\margt1440\margb1440");
            rtf.AppendLine(@"\viewkind4\uc1\pard\plain\f0\fs22\cf2");

            if (clinicConfig != null && !string.IsNullOrEmpty(clinicConfig.ClinicLogo))
            {
                string logoRtf = ClinicManager.Services.PrescriptionService.GetImageRtfCode(clinicConfig.ClinicLogo);
                if (!string.IsNullOrEmpty(logoRtf))
                {
                    rtf.AppendLine(logoRtf);
                }
            }
            rtf.AppendLine(@"\qc\fs36\b\cf1 " + EscapeRtf(clinicName) + @"\b0\par");
            if (!string.IsNullOrEmpty(clinicProp))
            {
                rtf.AppendLine(@"\fs20\cf3 " + EscapeRtf(clinicProp) + @"\par");
            }
            if (clinicConfig != null && !string.IsNullOrEmpty(clinicConfig.ClinicAddress))
            {
                rtf.AppendLine(@"\fs16\cf3 " + EscapeRtf(clinicConfig.ClinicAddress) + @"\par");
            }
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw15\brsp80\cf4\par\par");

            rtf.AppendLine(@"\pard\plain\f0\fs22\cf2");
            rtf.AppendLine(@"\b Date:\b0  " + DateTime.Now.ToString("yyyy-MM-dd") + @"\par\par");
            rtf.AppendLine(@"\b To,\b0\par");
            rtf.AppendLine(@"\b Dr. " + EscapeRtf(referredToDoctor) + @"\b0\par");
            rtf.AppendLine(@"Department of Dental / Medical Specialties\par");
            rtf.AppendLine(@"\b " + EscapeRtf(referredToClinic) + @"\b0\par\par");

            rtf.AppendLine(@"\b Subject: Clinical Referral for Patient: " + EscapeRtf(patientName) + @"\b0\par\par");

            rtf.AppendLine(@"Dear Doctor,\par\par");
            rtf.AppendLine(@"I am writing to refer patient \b " + EscapeRtf(patientName) + @"\b0 , age \b " + EscapeRtf(patientAge) + @"\b0 , gender \b " + EscapeRtf(patientGender) + @"\b0  for further clinical assessment and specialized management under your care.\par\par");

            rtf.AppendLine(@"\b Reason for Referral:\b0\par");
            rtf.AppendLine(EscapeRtf(reason) + @"\par\par");

            rtf.AppendLine(@"Please find their clinical history and dental logs attached to this referral letter. Kindly evaluate the patient and advise on the next course of action.\par\par");
            rtf.AppendLine(@"Thank you for your valuable clinical collaboration.\par\par");

            rtf.AppendLine(@"Warm regards,\par\par\par\par");
            rtf.AppendLine(@"_________________________\par");
            rtf.AppendLine(@"\b Referring Clinician\b0\par");
            rtf.AppendLine(EscapeRtf(clinicName) + @"\par");

            rtf.AppendLine(@"}");

            var bytes = Encoding.ASCII.GetBytes(rtf.ToString());
            return File(bytes, "application/rtf", $"referral_letter_{patientId}_{DateTime.Now:yyyyMMdd}.rtf");
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"");
        }

        private static string EscapeRtf(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            var sb = new StringBuilder();
            foreach (var c in input)
            {
                if (c == '\\' || c == '{' || c == '}')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c == '\n')
                {
                    sb.Append(@"\par ");
                }
                else if (c == '\r')
                {
                    // Skip carriage return
                }
                else if (c > 127)
                {
                    sb.Append(@"\u").Append((int)c).Append('?');
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
