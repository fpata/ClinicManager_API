namespace ClinicManager.Services
{
    public interface ITenantService
    {
        string? GetTenantId();
        string GetTenantConnectionString();
    }
}
