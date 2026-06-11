using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.DAL
{
    public class ClinicDbContext : DbContext
    {
        public ClinicDbContext(DbContextOptions<ClinicDbContext> options) : base(options) { }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientAppointment> PatientAppointments { get; set; }
        public DbSet<PatientReport> PatientReports { get; set; }
        public DbSet<PatientTreatment> PatientTreatments { get; set; }
        public DbSet<PatientTreatmentDetail> PatientTreatmentDetails { get; set; }
        public DbSet<PatientVitals> PatientVitals { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<BillingRecord> BillingRecords { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<AppConfig> AppConfigs { get; set; }
        public DbSet<RoleAccess> RoleAccesses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PatientTreatment -> PatientTreatmentDetail relationship
            // This ensures that when you add a PatientTreatment with PatientTreatmentDetails,
            // EF Core will automatically cascade the insert to child records
            modelBuilder.Entity<PatientTreatment>()
                .HasMany(pt => pt.PatientTreatmentDetails)
                .WithOne(ptd => ptd.PatientTreatment)
                .HasForeignKey(ptd => ptd.PatientTreatmentID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient -> PatientAppointment relationship
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.PatientAppointments)
                .WithOne()
                .HasForeignKey(pa => pa.PatientID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient -> PatientReport relationship
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.PatientReports)
                .WithOne()
                .HasForeignKey(pr => pr.PatientID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient -> PatientVitals relationship
            modelBuilder.Entity<Patient>()
                .HasMany(p => p.PatientVitals)
                .WithOne()
                .HasForeignKey(pv => pv.PatientID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Patient -> PatientTreatment relationship (one-to-one)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.PatientTreatment)
                .WithOne()
                .HasForeignKey<PatientTreatment>(pt => pt.PatientID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure BillingRecord -> Payment relationship
            modelBuilder.Entity<BillingRecord>()
                .HasMany(br => br.Payments)
                .WithOne()
                .HasForeignKey(p => p.BillingID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

