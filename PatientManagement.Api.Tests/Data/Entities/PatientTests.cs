using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Tests.Data.Entities;

public sealed class PatientTests
{
    private static readonly DateTimeOffset CurrentUtcDateTime =
        new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly CurrentUtcDate =
        DateOnly.FromDateTime(CurrentUtcDateTime.UtcDateTime);

    private static readonly TimeProvider FixedTimeProvider =
        new FixedUtcTimeProvider(CurrentUtcDateTime);

    [Fact]
    public void Constructor_WhenPatientIdIsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreatePatient(id: Guid.Empty));
    }

    [Fact]
    public void Constructor_WhenTenantIdIsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreatePatient(tenantId: Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WhenFullNameIsBlank_ThrowsArgumentException(string fullName)
    {
        Assert.Throws<ArgumentException>(() => CreatePatient(fullName: fullName));
    }

    [Fact]
    public void Constructor_WhenFullNameIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CreatePatient(fullName: null!));
    }

    [Fact]
    public void Constructor_WhenFullNameHasMaximumLength_CreatesPatient()
    {
        var fullName = new string('A', Patient.FullNameMaximumLength);

        var patient = CreatePatient(fullName: fullName);

        Assert.Equal(fullName, patient.FullName);
    }

    [Fact]
    public void Constructor_WhenFullNameExceedsMaximumLength_ThrowsArgumentException()
    {
        var fullName = new string('A', Patient.FullNameMaximumLength + 1);

        Assert.Throws<ArgumentException>(() => CreatePatient(fullName: fullName));
    }

    [Fact]
    public void Constructor_WhenDateOfBirthIsCurrentUtcDate_CreatesPatient()
    {
        var patient = CreatePatient(dateOfBirth: CurrentUtcDate);

        Assert.Equal(CurrentUtcDate, patient.DateOfBirth);
    }

    [Fact]
    public void Constructor_WhenDateOfBirthIsAfterCurrentUtcDate_ThrowsArgumentOutOfRangeException()
    {
        var futureDate = CurrentUtcDate.AddDays(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreatePatient(dateOfBirth: futureDate));
    }

    private static Patient CreatePatient(
        Guid? id = null,
        Guid? tenantId = null,
        string fullName = "Synthetic Patient",
        DateOnly? dateOfBirth = null)
    {
        return new Patient(
            id ?? Guid.NewGuid(),
            tenantId ?? Guid.NewGuid(),
            fullName,
            dateOfBirth ?? new DateOnly(1990, 1, 1),
            FixedTimeProvider);
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
