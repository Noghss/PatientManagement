using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data.Daos;

public interface IPatientDao
{
    Task<Patient?> GetByIdAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken);

    Task<PatientPage> GetPatientsByTenantAsync(
        Guid tenantId,
        Guid? afterPatientId,
        int pageSize,
        CancellationToken cancellationToken);
}
