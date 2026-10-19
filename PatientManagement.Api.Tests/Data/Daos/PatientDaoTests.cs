using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PatientManagement.Api.Data;
using PatientManagement.Api.Data.Daos;
using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Tests.Data.Daos;

public sealed class PatientDaoTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext? dbContext;
    private PatientDao? patientDao;

    private AppDbContext DbContext => dbContext
        ?? throw new InvalidOperationException("The test database has not been initialized.");

    private PatientDao PatientDao => patientDao
        ?? throw new InvalidOperationException("The DAO has not been initialized.");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new AppDbContext(options);
        patientDao = new PatientDao(dbContext);

        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (dbContext is not null)
        {
            await dbContext.DisposeAsync();
        }

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPatientBelongsToTenant_ReturnsPatient()
    {
        var tenantId = Guid.NewGuid();
        var patient = CreateSyntheticPatient(tenantId);

        await AddPatientAsync(patient);

        var result = await PatientDao.GetByIdAsync(
            tenantId,
            patient.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(patient.Id, result.Id);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(patient.FullName, result.FullName);
        Assert.Equal(patient.DateOfBirth, result.DateOfBirth);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPatientDoesNotExist_ReturnsNull()
    {
        var result = await PatientDao.GetByIdAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPatientBelongsToAnotherTenant_ReturnsNull()
    {
        var ownerTenantId = Guid.NewGuid();
        var requestingTenantId = Guid.NewGuid();
        var patient = CreateSyntheticPatient(ownerTenantId);

        await AddPatientAsync(patient);

        var result = await PatientDao.GetByIdAsync(
            requestingTenantId,
            patient.Id,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PatientDao.GetByIdAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetByIdAsync_WhenPatientFound_DoesNotTrackPatient()
    {
        var tenantId = Guid.NewGuid();
        var patient = CreateSyntheticPatient(tenantId);

        await AddPatientAsync(patient);

        var result = await PatientDao.GetByIdAsync(
            tenantId,
            patient.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(EntityState.Detached, DbContext.Entry(result).State);
    }

    [Fact]
    public async Task Database_WhenPatientIdIsEmpty_RejectsPatient()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertPatientAsync(
            Guid.Empty,
            Guid.NewGuid(),
            "Synthetic Patient"));
    }

    [Fact]
    public async Task Database_WhenTenantIdIsEmpty_RejectsPatient()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertPatientAsync(
            Guid.NewGuid(),
            Guid.Empty,
            "Synthetic Patient"));
    }

    [Fact]
    public async Task Database_WhenFullNameIsBlank_RejectsPatient()
    {
        await Assert.ThrowsAsync<SqliteException>(() => InsertPatientAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "   "));
    }

    private async Task AddPatientAsync(Patient patient)
    {
        DbContext.Patients.Add(patient);
        await DbContext.SaveChangesAsync(CancellationToken.None);
        DbContext.ChangeTracker.Clear();
    }

    private Task<int> InsertPatientAsync(
        Guid patientId,
        Guid tenantId,
        string fullName)
    {
        return DbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO patients (patient_id, tenant_id, full_name, date_of_birth)
            VALUES ({patientId}, {tenantId}, {fullName}, {new DateOnly(1990, 1, 1)});
            """);
    }

    private static Patient CreateSyntheticPatient(Guid tenantId)
    {
        return new Patient(
            Guid.NewGuid(),
            tenantId,
            "Synthetic Patient",
            new DateOnly(1990, 1, 1));
    }
}
