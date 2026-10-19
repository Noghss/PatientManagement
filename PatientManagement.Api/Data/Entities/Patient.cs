namespace PatientManagement.Api.Data.Entities;

public sealed class Patient
{
    public const int FullNameMaximumLength = 200;

    public Patient(Guid id, Guid tenantId, string fullName, DateOnly dateOfBirth)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Patient ID cannot be empty.", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        if (fullName.Length > FullNameMaximumLength)
        {
            throw new ArgumentException(
                $"Full name cannot exceed {FullNameMaximumLength} characters.",
                nameof(fullName));
        }

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dateOfBirth),
                "Date of birth cannot be in the future.");
        }

        Id = id;
        TenantId = tenantId;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string FullName { get; private set; }

    public DateOnly DateOfBirth { get; private set; }
}
