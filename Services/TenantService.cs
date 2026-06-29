using System;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ClinicManager.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public TenantService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string? GetTenantId()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // 1. Try to get tenant from authenticated JWT claims
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = context.User.FindFirst("tenantid")?.Value;
                if (!string.IsNullOrEmpty(tenantClaim))
                {
                    return tenantClaim;
                }
            }

            // 2. Try to get tenant from HTTP header (X-Tenant-ID)
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader) && !string.IsNullOrEmpty(tenantHeader))
            {
                return tenantHeader.ToString();
            }

            // 3. Try to get tenant from query string
            if (context.Request.Query.TryGetValue("tenantId", out var tenantQuery) && !string.IsNullOrEmpty(tenantQuery))
            {
                return tenantQuery.ToString();
            }

            return null;
        }

        public string GetTenantConnectionString()
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return DecodeConnectionString(GetDefaultConnectionString());
            }

            // Look up the connection string in configuration:
            // 1. Check Tenants:TenantId:ConnectionString (JSON or Azure App Setting)
            // 2. Check Tenants:TenantId (JSON flat string or Azure App Setting)
            // 3. Check ConnectionStrings:TenantId (JSON connection string or Azure Connection String)
            var connectionString = _configuration[$"Tenants:{tenantId}:ConnectionString"]
                ?? _configuration[$"Tenants:{tenantId}"]
                ?? _configuration.GetConnectionString(tenantId);

            if (string.IsNullOrEmpty(connectionString))
            {
                return DecodeConnectionString(GetDefaultConnectionString());
            }

            return DecodeConnectionString(connectionString);
        }

        private string GetDefaultConnectionString()
        {
            var dbProvider = _configuration["DatabaseProvider"] ?? "MySql";
            string connStrName = dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) 
                ? "SqlServerConnection" 
                : "MySqlConnection";

            return _configuration.GetConnectionString(connStrName)
                ?? _configuration[$"ConnectionStrings:{connStrName}"]
                ?? _configuration.GetConnectionString("DefaultConnection")
                ?? _configuration["ConnectionStrings:DefaultConnection"]
                ?? string.Empty;
        }

        private string DecodeConnectionString(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            try
            {
                string paddedInput = input.Trim();
                int mod = paddedInput.Length % 4;
                if (mod == 2) paddedInput += "==";
                else if (mod == 3) paddedInput += "=";

                var bytes = Convert.FromBase64String(paddedInput);
                foreach (var b in bytes)
                {
                    if ((b < 32 && b != 9 && b != 10 && b != 13) || b >= 127)
                    {
                        return input;
                    }
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return input;
            }
        }
    }
}
