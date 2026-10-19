# PatientManagement Repository Instructions

These instructions apply to the entire repository. For DAO-specific implementation steps, also use `.agents/skills/dotnet-dao-development/SKILL.md`; keep its specialized workflow in the skill rather than duplicating it here.

## Stack and architecture

- The application uses C#, .NET 9, ASP.NET Core, Entity Framework Core, and PostgreSQL through Npgsql.
- Preserve the existing solution structure, dependency-injection approach, EF Core configurations, DAO abstractions, naming, formatting, and test conventions. Reuse established patterns before introducing new abstractions.
- Do not create migrations or public endpoints, change authentication behavior, or expand public contracts unless the task requires that work.

## Data access and tenant safety

- Use asynchronous EF Core operations and propagate `CancellationToken` through relevant interfaces and call chains.
- Preserve tenant isolation in every patient query and mutation. Apply tenant predicates in the database query rather than filtering materialized data.
- Never assume a client-supplied tenant ID is trusted. Obtain or validate tenant identity through the application's trusted server-side authentication or tenant context.
- Use `AsNoTracking()` for read-only EF Core queries when tracked state is not required.
- Do not run destructive database commands, including drop, truncate, reset, or recreate operations, without explicit authorization and verification of the exact target.

## Security and test data

- Never commit credentials, connection-string secrets, tokens, or real patient information.
- Use only synthetic, non-sensitive patient data in tests, fixtures, examples, and logs.
- SQLite tests provide useful behavioral coverage but do not prove PostgreSQL-specific query translation, types, constraints, indexes, casing, transaction behavior, or performance.

## Change discipline and verification

- Review Git status and diffs before finishing. Preserve pre-existing changes and avoid unrelated edits, formatting churn, package changes, or generated files.
- After code changes, run the relevant restore, build, and test commands. Report exactly which commands ran and which passed, failed, or were blocked; never infer or invent results.
- Use these commands from the repository root in Windows Command Prompt:

```cmd
dotnet restore PatientManagement.sln
dotnet build PatientManagement.sln --no-restore
dotnet test PatientManagement.sln --no-build
```

- When PostgreSQL-specific behavior is affected, add or run appropriate PostgreSQL verification when available. If it cannot be run, state the remaining verification gap explicitly.
