using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable(
            "patients",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "ck_patients_date_of_birth_not_future",
                "date_of_birth <= CURRENT_DATE"));

        builder.HasKey(patient => patient.Id)
            .HasName("pk_patients");

        builder.Property(patient => patient.Id)
            .HasColumnName("patient_id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(patient => patient.TenantId)
            .HasColumnName("tenant_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(patient => patient.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(Patient.FullNameMaximumLength)
            .IsRequired();

        builder.Property(patient => patient.DateOfBirth)
            .HasColumnName("date_of_birth")
            .HasColumnType("date")
            .IsRequired();
    }
}
