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

    public async Task<PatientPage> GetPatientsByTenantAsync(
        Guid tenantId,
        Guid? afterPatientId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);

        var query = dbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.TenantId == tenantId);

        if (afterPatientId.HasValue)
        {
            query = query.Where(
                patient => patient.Id.CompareTo(afterPatientId.Value) > 0);
        }

        var patients = await query
            .OrderBy(patient => patient.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = patients.Count > pageSize;

        if (hasNextPage)
        {
            patients.RemoveAt(pageSize);
        }

        var nextCursor = hasNextPage
            ? patients[^1].Id
            : (Guid?)null;

        return new PatientPage(patients, nextCursor);
    }
}
