using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_patients_patient_id_not_empty",
                "patient_id <> '00000000-0000-0000-0000-000000000000'");

            tableBuilder.HasCheckConstraint(
                "ck_patients_tenant_id_not_empty",
                "tenant_id <> '00000000-0000-0000-0000-000000000000'");

            tableBuilder.HasCheckConstraint(
                "ck_patients_full_name_not_blank",
                "length(trim(full_name)) > 0");
        });

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
