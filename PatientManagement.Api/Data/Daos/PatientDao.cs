using Microsoft.EntityFrameworkCore;
using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data.Daos;

public sealed class PatientDao(AppDbContext dbContext) : IPatientDao
{
    public Task<Patient?> GetByIdAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        return dbContext.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(
                patient => patient.TenantId == tenantId && patient.Id == patientId,
                cancellationToken);
    }
}
