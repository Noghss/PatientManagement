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
    public async Task GetPatientsByTenantAsync_WhenFirstPageRequested_ReturnsExpectedNumberOfPatients()
    {
        var tenantId = Guid.NewGuid();
        var patients = CreateSyntheticPatients(tenantId, 5);

        await AddPatientsAsync(patients);

        var result = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            afterPatientId: null,
            pageSize: 2,
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenReadingMultiplePages_ReturnsEveryPatientExactlyOnce()
    {
        var tenantId = Guid.NewGuid();
        var patients = CreateSyntheticPatients(tenantId, 7);
        var expectedPatientIds = patients
            .Select(patient => patient.Id)
            .OrderBy(patientId => patientId)
            .ToArray();

        await AddPatientsAsync(patients);

        var returnedPatientIds = new List<Guid>();
        Guid? afterPatientId = null;
        var pageCount = 0;

        do
        {
            Assert.True(pageCount < 3, "Pagination did not reach a final page.");

            var page = await PatientDao.GetPatientsByTenantAsync(
                tenantId,
                afterPatientId,
                pageSize: 3,
                CancellationToken.None);

            returnedPatientIds.AddRange(page.Items.Select(patient => patient.Id));
            afterPatientId = page.NextCursor;
            pageCount++;
        }
        while (afterPatientId.HasValue);

        Assert.Equal(expectedPatientIds, returnedPatientIds);
        Assert.Equal(returnedPatientIds.Count, returnedPatientIds.Distinct().Count());
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenMorePatientsExist_SetsNextCursorToLastReturnedPatientId()
    {
        var tenantId = Guid.NewGuid();
        var patients = CreateSyntheticPatients(tenantId, 3);

        await AddPatientsAsync(patients);

        var result = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            afterPatientId: null,
            pageSize: 2,
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(result.Items[^1].Id, result.NextCursor);
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenFinalPageReturned_SetsNextCursorToNull()
    {
        var tenantId = Guid.NewGuid();
        var patients = CreateSyntheticPatients(tenantId, 4);

        await AddPatientsAsync(patients);

        var firstPage = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            afterPatientId: null,
            pageSize: 2,
            CancellationToken.None);

        var finalPage = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            firstPage.NextCursor,
            pageSize: 2,
            CancellationToken.None);

        Assert.Equal(2, finalPage.Items.Count);
        Assert.Null(finalPage.NextCursor);
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenReadingEveryPage_PreservesTenantIsolation()
    {
        var requestedTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var requestedTenantPatients = CreateSyntheticPatients(requestedTenantId, 5);
        var otherTenantPatients = CreateSyntheticPatients(
            otherTenantId,
            patientCount: 6,
            firstPatientNumber: 101);

        await AddPatientsAsync(requestedTenantPatients.Concat(otherTenantPatients));

        var returnedPatients = new List<Patient>();
        Guid? afterPatientId = null;
        var pageCount = 0;

        do
        {
            Assert.True(pageCount < 3, "Pagination did not reach a final page.");

            var page = await PatientDao.GetPatientsByTenantAsync(
                requestedTenantId,
                afterPatientId,
                pageSize: 2,
                CancellationToken.None);

            returnedPatients.AddRange(page.Items);
            afterPatientId = page.NextCursor;
            pageCount++;
        }
        while (afterPatientId.HasValue);

        Assert.Equal(requestedTenantPatients.Count, returnedPatients.Count);
        Assert.All(
            returnedPatients,
            patient => Assert.Equal(requestedTenantId, patient.TenantId));
        Assert.Equal(
            requestedTenantPatients.Select(patient => patient.Id).OrderBy(patientId => patientId),
            returnedPatients.Select(patient => patient.Id));
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenDatabaseIsEmpty_ReturnsEmptyPage()
    {
        var result = await PatientDao.GetPatientsByTenantAsync(
            Guid.NewGuid(),
            afterPatientId: null,
            pageSize: 10,
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenTenantHasNoPatients_ReturnsEmptyPage()
    {
        await AddPatientsAsync(CreateSyntheticPatients(Guid.NewGuid(), 3));

        var result = await PatientDao.GetPatientsByTenantAsync(
            Guid.NewGuid(),
            afterPatientId: null,
            pageSize: 10,
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task GetPatientsByTenantAsync_WhenPageSizeIsValid_ReturnsRequestedPageSize(
        int pageSize)
    {
        var tenantId = Guid.NewGuid();
        var patients = CreateSyntheticPatients(tenantId, pageSize + 1);

        await AddPatientsAsync(patients);

        var result = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            afterPatientId: null,
            pageSize,
            CancellationToken.None);

        Assert.Equal(pageSize, result.Items.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetPatientsByTenantAsync_WhenPageSizeIsInvalid_ThrowsArgumentOutOfRangeException(
        int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            PatientDao.GetPatientsByTenantAsync(
                Guid.NewGuid(),
                afterPatientId: null,
                pageSize,
                CancellationToken.None));
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PatientDao.GetPatientsByTenantAsync(
                Guid.NewGuid(),
                afterPatientId: null,
                pageSize: 10,
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenCancellationRequested_PropagatesCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PatientDao.GetPatientsByTenantAsync(
                Guid.NewGuid(),
                afterPatientId: null,
                pageSize: 10,
                cancellationTokenSource.Token));

        Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenPatientsReturned_DoesNotTrackPatients()
    {
        var tenantId = Guid.NewGuid();
        var patients = CreateSyntheticPatients(tenantId, 3);

        await AddPatientsAsync(patients);

        var result = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            afterPatientId: null,
            pageSize: 3,
            CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
        Assert.Empty(DbContext.ChangeTracker.Entries<Patient>());
        Assert.All(
            result.Items,
            patient => Assert.Equal(EntityState.Detached, DbContext.Entry(patient).State));
    }

    [Fact]
    public async Task GetPatientsByTenantAsync_WhenPatientsReturned_OrdersPatientsById()
    {
        var tenantId = Guid.NewGuid();
        var patients = new[]
        {
            CreateSyntheticPatient(
                tenantId,
                Guid.Parse("00000000-0000-0000-0000-000000000003")),
            CreateSyntheticPatient(
                tenantId,
                Guid.Parse("00000000-0000-0000-0000-000000000001")),
            CreateSyntheticPatient(
                tenantId,
                Guid.Parse("00000000-0000-0000-0000-000000000004")),
            CreateSyntheticPatient(
                tenantId,
                Guid.Parse("00000000-0000-0000-0000-000000000002"))
        };

        await AddPatientsAsync(patients);

        var result = await PatientDao.GetPatientsByTenantAsync(
            tenantId,
            afterPatientId: null,
            pageSize: 10,
            CancellationToken.None);

        Assert.Equal(
            patients.Select(patient => patient.Id).OrderBy(patientId => patientId),
            result.Items.Select(patient => patient.Id));
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

    private async Task AddPatientsAsync(IEnumerable<Patient> patients)
    {
        DbContext.Patients.AddRange(patients);
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

    private static IReadOnlyList<Patient> CreateSyntheticPatients(
        Guid tenantId,
        int patientCount,
        int firstPatientNumber = 1)
    {
        return Enumerable.Range(firstPatientNumber, patientCount)
            .Select(index => CreateSyntheticPatient(
                tenantId,
                Guid.Parse($"00000000-0000-0000-0000-{index:D12}")))
            .ToArray();
    }

    private static Patient CreateSyntheticPatient(
        Guid tenantId,
        Guid? patientId = null)
    {
        return new Patient(
            patientId ?? Guid.NewGuid(),
            tenantId,
            "Synthetic Patient",
            new DateOnly(1990, 1, 1));
    }
}
