using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data.Daos;

public interface IPatientDao
{
    Task<Patient?> GetByIdAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken);
}
