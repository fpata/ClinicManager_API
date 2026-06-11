using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ClinicDbContext _context;

        public PrescriptionService(ClinicDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GeneratePrescriptionRtfAsync(int treatmentId, int? treatmentDetailId, bool includeHeader, string? doctorNotes)
        {
            // 1. Fetch the PatientTreatment
            var treatment = await _context.PatientTreatments
                .Include(pt => pt.PatientTreatmentDetails)
                .FirstOrDefaultAsync(pt => pt.ID == treatmentId && pt.IsActive == 1);

            if (treatment == null)
            {
                throw new ArgumentException($"Treatment with ID {treatmentId} not found or is inactive.");
            }

            // 2. Fetch the Patient and Patient User details
            Patient? patient = null;
            User? patientUser = null;
            if (treatment.PatientID.HasValue)
            {
                patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.ID == treatment.PatientID.Value && p.IsActive == 1);
                
                if (patient != null)
                {
                    patientUser = await _context.Users
                        .Include(u => u.Contact)
                        .Include(u => u.Address)
                        .FirstOrDefaultAsync(u => u.ID == patient.UserID && u.IsActive == 1);
                }
            }

            // 3. Fetch Doctor details
            User? doctorUser = null;
            if (treatment.DoctorID.HasValue)
            {
                doctorUser = await _context.Users
                    .Include(u => u.Contact)
                    .Include(u => u.Address)
                    .FirstOrDefaultAsync(u => u.ID == treatment.DoctorID.Value && u.IsActive == 1);
            }

            // 4. Fetch Clinic Configuration
            var clinicConfig = await _context.AppConfigs
                .FirstOrDefaultAsync(c => c.IsActive == 1);
            string clinicName = clinicConfig?.ClinicName ?? "Clinic Manager";
            string clinicProp = clinicConfig?.ClinicProp ?? "Premium Oral Care & Dental Clinic";

            // 5. Gather demographic strings
            string patientName = patientUser?.FullName ?? "N/A";
            string patientAge = patientUser?.Age.HasValue == true ? patientUser.Age.Value.ToString() : "N/A";
            string patientGender = patientUser?.Gender.HasValue == true ? patientUser.Gender.Value.ToString() : "N/A";
            string patientPhone = patientUser?.Contact?.PrimaryPhone ?? "N/A";
            string patientAllergies = patient?.Allergies ?? "No known allergies";

            string doctorName = doctorUser != null ? $"Dr. {doctorUser.FullName}" : "N/A";
            string doctorSpecialization = doctorUser?.Specialization ?? "General Dentist";
            string doctorLicense = doctorUser?.LicenseNumber ?? "N/A";

            string printDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // 6. Build the RTF Document
            var rtf = new StringBuilder();

            // RTF Header, Font Table, and Color Table
            rtf.AppendLine(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            rtf.AppendLine(@"{\fonttbl{\f0\fnil\fcharset0 Calibri;}{\f1\fnil\fcharset0 Calibri-Bold;}}");
            // Colors: index 1: Navy Blue (#1A365D), index 2: Charcoal/Dark Gray (#2D3748), index 3: Muted Gray (#718096), index 4: Light Gray Border (#E2E8F0)
            rtf.AppendLine(@"{\colortbl ;\red26\green54\blue93;\red45\green55\blue72;\red113\green128\blue150;\red226\green232\blue240;}");
            // Set margins (1 inch = 1440 twips)
            rtf.AppendLine(@"\margl1440\margr1440\margt1440\margb1440");
            rtf.AppendLine(@"\viewkind4\uc1\pard\plain\f0\fs22\cf2");

            // Clinic Header Banner
            if (includeHeader)
            {
                if (clinicConfig != null && !string.IsNullOrEmpty(clinicConfig.ClinicLogo))
                {
                    string logoRtf = GetImageRtfCode(clinicConfig.ClinicLogo);
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
                // Clinic header bottom line
                rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw15\brsp80\cf4\par");
            }

            rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par");

            // Two-column Info Section (Patient info on left, Doctor info on right)
            rtf.AppendLine(@"\tx4000\tx4500\tx8000"); // Tab stops: left starts at 0, right labels start at 4500
            rtf.AppendLine(@"\b Patient Name:\b0  " + EscapeRtf(patientName) + @"\tab\b Doctor Name:\b0  " + EscapeRtf(doctorName) + @"\par");
            rtf.AppendLine(@"\b Age / Gender:\b0  " + EscapeRtf($"{patientAge} / {patientGender}") + @"\tab\b Specialization:\b0  " + EscapeRtf(doctorSpecialization) + @"\par");
            rtf.AppendLine(@"\b Contact No:\b0  " + EscapeRtf(patientPhone) + @"\tab\b License No:\b0  " + EscapeRtf(doctorLicense) + @"\par");
            rtf.AppendLine(@"\b Date / Time:\b0  " + EscapeRtf(printDate) + @"\tab\b Allergies:\b0  " + EscapeRtf(patientAllergies) + @"\par");
            rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw10\brsp80\cf4\par");

            rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par");

            // The RX Symbol
            rtf.AppendLine(@"\fs36\b\cf1 Rx\b0\fs22\cf2\par\par");

            // Content logic based on optional treatmentDetailId
            if (treatmentDetailId.HasValue)
            {
                // Print specific detail record
                var detail = treatment.PatientTreatmentDetails?
                    .FirstOrDefault(d => d.ID == treatmentDetailId.Value && d.IsActive == 1);

                if (detail == null)
                {
                    throw new ArgumentException($"Treatment detail with ID {treatmentDetailId.Value} under Treatment {treatmentId} not found or is inactive.");
                }

                var dateStr = detail.TreatmentDate?.ToString("yyyy-MM-dd") ?? "N/A";
                rtf.AppendLine(@"\b Treatment Date:\b0  " + EscapeRtf(dateStr) + @"\par");
                rtf.AppendLine(@"\b Tooth / Area:\b0  " + EscapeRtf(detail.Tooth ?? "N/A") + @"\par");
                rtf.AppendLine(@"\b Procedure:\b0  " + EscapeRtf(detail.Procedure ?? "N/A") + @"\par");
                rtf.AppendLine(@"\b Prescription Medication:\b0\par");
                rtf.AppendLine(@"\pard\fi-360\li720 " + FormatPrescriptionText(detail.Prescription) + @"\par");

                if (!string.IsNullOrEmpty(detail.FollowUpInstructions))
                {
                    rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par\b Follow Up Instructions:\b0\par");
                    rtf.AppendLine(@"\pard\fi-360\li720 " + EscapeRtf(detail.FollowUpInstructions) + @"\par");
                }
            }
            else
            {
                // Print general prescription
                if (!string.IsNullOrEmpty(treatment.Prescription))
                {
                    rtf.AppendLine(@"\b General Prescription:\b0\par");
                    rtf.AppendLine(@"\pard\fi-360\li720 " + FormatPrescriptionText(treatment.Prescription) + @"\par");
                }
                else
                {
                    rtf.AppendLine(@"\b General Prescription:\b0  No general prescription medication recorded.\par");
                }

                // Append active detail prescriptions if they exist
                var activeDetails = treatment.PatientTreatmentDetails?
                    .Where(d => d.IsActive == 1)
                    .OrderBy(d => d.TreatmentDate)
                    .ToList();

                if (activeDetails != null && activeDetails.Any())
                {
                    rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par\b Treatment Details & Prescriptions:\b0\par\par");
                    
                    // Table tab stops: Date (0 to 1500), Tooth (1500 to 2500), Procedure (2500 to 5500), Prescription (5500+)
                    rtf.AppendLine(@"\tx1500\tx2500\tx5500");
                    rtf.AppendLine(@"\b Date\tab Tooth\tab Procedure\tab Prescription / Medication\b0\par");
                    rtf.AppendLine(@"\pard\brdrb\brdrs\brdrw5\brsp40\cf4\par");

                    foreach (var detail in activeDetails)
                    {
                        var detailDate = detail.TreatmentDate?.ToString("yyyy-MM-dd") ?? "-";
                        var tooth = detail.Tooth ?? "-";
                        var proc = detail.Procedure ?? "-";
                        var presc = !string.IsNullOrEmpty(detail.Prescription) ? detail.Prescription : "No medication";

                        rtf.AppendLine(@"\pard\plain\f0\fs20\cf2\tx1500\tx2500\tx5500 " + 
                            EscapeRtf(detailDate) + @"\tab " + 
                            EscapeRtf(tooth) + @"\tab " + 
                            EscapeRtf(proc) + @"\tab " + 
                            EscapeRtf(presc) + @"\par");
                    }
                }
            }

            // Append optional Doctor Notes
            if (!string.IsNullOrEmpty(doctorNotes))
            {
                rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par\b Additional Doctor Notes:\b0\par");
                rtf.AppendLine(@"\pard\fi-360\li720 " + EscapeRtf(doctorNotes) + @"\par");
            }

            // Add Sign-off Section at the bottom right
            rtf.AppendLine(@"\pard\plain\f0\fs22\cf2\par\par\par\par");
            rtf.AppendLine(@"\qr\b Signature:\b0\par\par");
            rtf.AppendLine(@"\qr _________________________\par");
            rtf.AppendLine(@"\qr\fs16\cf3 " + EscapeRtf(doctorName) + @"\par");
            if (doctorUser != null && !string.IsNullOrEmpty(doctorUser.Specialization))
            {
                rtf.AppendLine(@"\qr\fs16\cf3 " + EscapeRtf(doctorUser.Specialization) + @"\par");
            }

            rtf.AppendLine(@"}");

            return Encoding.ASCII.GetBytes(rtf.ToString());
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
                    // Encode unicode characters as \uNNNN?
                    sb.Append(@"\u").Append((int)c).Append('?');
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string FormatPrescriptionText(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "No prescription medication recorded.";
            
            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append(@"\bullet\tab " + EscapeRtf(lines[i]));
                if (i < lines.Length - 1)
                {
                    sb.Append(@"\par ");
                }
            }
            return sb.ToString();
        }

        public static string GetImageRtfCode(string base64DataUri)
        {
            if (string.IsNullOrWhiteSpace(base64DataUri)) return string.Empty;

            try
            {
                string base64String = base64DataUri;
                string imageType = "pngblip";

                if (base64DataUri.Contains(","))
                {
                    int commaIndex = base64DataUri.IndexOf(',');
                    string header = base64DataUri.Substring(0, commaIndex);
                    if (header.Contains("image/jpeg") || header.Contains("image/jpg"))
                    {
                        imageType = "jpegblip";
                    }
                    base64String = base64DataUri.Substring(commaIndex + 1);
                }

                byte[] bytes = Convert.FromBase64String(base64String.Trim());
                var hex = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    hex.AppendFormat("{0:x2}", b);
                }

                // Centered picture inside a paragraph, with picwgoal and pichgoal set to 1440 twips (1 inch)
                return @"\pard\qc{\pict\" + imageType + @"\picwgoal1440\pichgoal1440 " + hex.ToString() + @"}\par";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
