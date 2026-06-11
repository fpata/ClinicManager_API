using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicManager.DAL;
using Castle.Core.Smtp;
using ClinicManager.Models.Enums;
using ClinicManager.Services;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<LoginController> _logger;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public LoginController(ClinicDbContext context, ILogger<LoginController> logger, IEmailService emailService, ISmsService smsService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _smsService = smsService;
        }

        public class LoginRequest
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for user: {UserName}", request.UserName);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found: {UserName}", request.UserName);
                return Unauthorized("Invalid username or password.");
            }

            if (user.Password != request.Password)
            {
                _logger.LogWarning("Login failed: Incorrect password for user: {UserName}", request.UserName);
                return Unauthorized("Invalid username or password.");
            }

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                _logger.LogError("User record has null/empty UserName. User ID: {UserId}", user.ID);
                return StatusCode(500, "User record invalid.");
            }

            _logger.LogInformation("Login successful for user: {UserName}", request.UserName);

            var userRole = user?.UserType?.ToString() ?? UserType.Patient.ToString();
            var roleAccess = await _context.RoleAccesses.AsNoTracking().FirstOrDefaultAsync(ra => ra.RoleName == userRole);
            
            bool canAccessPatient = roleAccess?.CanAccessPatient ?? (userRole == "Administrator" || userRole == "Doctor" || userRole == "Nurse" || userRole == "Accountant" || userRole == "Patient");
            bool canAccessDashboard = roleAccess?.CanAccessDashboard ?? (userRole == "Administrator" || userRole == "Doctor" || userRole == "Nurse");
            bool canAccessBilling = roleAccess?.CanAccessBilling ?? (userRole == "Administrator" || userRole == "Doctor" || userRole == "Accountant");
            bool canAccessConfig = roleAccess?.CanAccessConfig ?? (userRole == "Administrator" || userRole == "Doctor");

            var configuration = HttpContext.RequestServices.GetService<IConfiguration>();
            var jwtKey = configuration?["Jwt:Key"] ?? "ClinicManagerJwtTokenForEncryption";
            var jwtIssuer = configuration?["Jwt:Issuer"] ?? "ClinicManagerIssuer";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user!.UserName!), // userName validated above
                new Claim("userid", user.ID.ToString()),
                new Claim("usertype", userRole),
                new Claim(ClaimTypes.Role, userRole),
                new Claim("canAccessPatient", canAccessPatient.ToString().ToLower()),
                new Claim("canAccessDashboard", canAccessDashboard.ToString().ToLower()),
                new Claim("canAccessBilling", canAccessBilling.ToString().ToLower()),
                new Claim("canAccessConfig", canAccessConfig.ToString().ToLower()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    ID=user?.ID,
                    UserName=user?.UserName,
                    UserType=user?.UserType,
                    FirstName=user?.FirstName,
                    LastName=user?.LastName,
                    Gender=user?.Gender,
                    Age=user?.Age,
                    LastLoginDate=user?.LastLoginDate
                },
                allowedAccess = new
                {
                    canAccessPatient = canAccessPatient,
                    canAccessDashboard = canAccessDashboard,
                    canAccessBilling = canAccessBilling,
                    canAccessConfig = canAccessConfig
                }
            });
        }

        [HttpGet]
        [Route("forgotpassword")]
        public async Task<IActionResult> forgotpassword(string sendTo, bool isMobile = false)
        {
            _logger.LogInformation("Forgot password requested for {SendTo} (isMobile: {IsMobile})", sendTo, isMobile);

            if (string.IsNullOrWhiteSpace(sendTo))
            {
                return BadRequest("Contact information cannot be empty.");
            }

            var user = await _context.Users
                .Include(u => u.Contact)
                .FirstOrDefaultAsync(u => u.IsActive == 1 && (isMobile
                    ? (u.Contact != null && (u.Contact.PrimaryPhone == sendTo || u.Contact.SecondaryPhone == sendTo))
                    : (u.Contact != null && (u.Contact.PrimaryEmail == sendTo || u.Contact.SecondaryEmail == sendTo))
                ));

            if (user == null)
            {
                _logger.LogWarning("Forgot password failed: No active user found for contact {SendTo}", sendTo);
                return NotFound("No active user found with the provided contact information.");
            }

            // Generate a secure temporary password/OTP
            string tempPassword = GenerateTemporaryPassword();

            // Update user password in database
            user.Password = tempPassword;
            user.ModifiedDate = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            if (isMobile)
            {
                string message = $"Your Clinic Manager temporary password/OTP is: {tempPassword}. Please use this to log in.";
                await _smsService.SendSmsAsync(sendTo, message);
                return Ok($"OTP sent to mobile number {sendTo}");
            }
            else
            {
                string subject = "Clinic Manager Password Reset";
                string body = $@"
                    <h3>Clinic Manager Password Reset</h3>
                    <p>Hello {user.FirstName},</p>
                    <p>Your password has been reset. Please use the following temporary password to log in:</p>
                    <p><strong>{tempPassword}</strong></p>
                    <p>For security, we recommend changing your password after logging in.</p>
                    <br>
                    <p>Regards,<br>Relief Dental Clinic Team</p>";

                await _emailService.SendEmailAsync(sendTo, subject, body);
                return Ok($"Reset link sent to email {sendTo}");
            }
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }
    }
}
