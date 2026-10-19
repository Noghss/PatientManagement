using Microsoft.EntityFrameworkCore;
using PatientManagement.Api.Data.Configurations;
using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
    }
}
