using PatientManagement.Api.Data.Entities;

namespace PatientManagement.Api.Data.Daos;

public sealed record PatientPage(
    IReadOnlyList<Patient> Items,
    Guid? NextCursor);
