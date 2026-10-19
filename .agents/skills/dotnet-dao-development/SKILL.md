---
name: dotnet-dao-development
description: Build or review .NET EF Core DAOs, persistence queries, tenant-scoped data access, and DAO tests. Use for DAO or repository-layer changes; exclude endpoints and migrations unless explicitly requested.
---

# .NET DAO Development

Follow this workflow for DAO implementation and review. Keep all work within the user's requested scope; a request to review or diagnose does not authorize code changes.

## Workflow

1. **Inspect architecture and existing DAOs.** Locate the solution and affected projects, then inspect entities, `DbContext` configuration, DAO interfaces and implementations, dependency injection, calling services, tests, and applicable repository instructions. Determine the actual database provider and supported target frameworks.
2. **Identify conventions and reusable code.** Record established patterns for namespaces, method names, return types, tenant context, query composition, error handling, transactions, pagination, test fixtures, and cancellation. Reuse existing abstractions and helpers when they fit the requested behavior.
3. **Perform impact analysis.** Trace callers and dependencies. Identify affected contracts, queries, mappings, tenant boundaries, transaction behavior, concurrency assumptions, provider-specific behavior, tests, and security exposure. Confirm whether schema or endpoint work would be required, but do not add either unless requested.
4. **Plan implementation and clarify blockers.** Form a focused implementation plan. Treat the user's request as authorization for changes within that scope. Ask for direction only when missing information, conflicting requirements, credentials, or an out-of-scope dependency would materially change the result.
5. **Implement approved changes only.** Modify only the DAO-related code and tests authorized by the request. Preserve public contracts and existing behavior unless changing them is required. For review-only or diagnostic requests, do not edit code.
6. **Preserve tenant isolation.** Obtain tenant identity from the repository's trusted server-side authentication or tenant context. Never trust a tenant identifier merely because it came from a client request. Apply the tenant boundary to every applicable read, update, and delete query, and prevent cross-tenant entity attachment or mutation.
7. **Add tests.** Cover successful behavior, missing records, cancellation, invalid inputs where applicable, and cross-tenant access. Use synthetic data only. Add regression tests for the requested change and avoid tests that depend on production data or secrets.
8. **Run restore, build, and test.** Run `dotnet restore`, then build, then execute the relevant test projects or solution. Use repository-specific commands when documented. Capture the commands, exit status, and meaningful failures. Never claim a command passed unless it actually completed successfully.
9. **Review correctness and security.** Re-read the diff and check tenant predicates, authorization assumptions, query translation, nullability, cancellation flow, tracking behavior, concurrency, transaction boundaries, sensitive-data exposure, and accidental scope expansion.
10. **Report verification evidence.** Summarize files changed, behavior implemented, tests added, and the exact restore/build/test results. Clearly identify anything not run, any environmental limitation, and any residual PostgreSQL or integration risk.

## Data-access rules

- Use asynchronous EF Core APIs for database work and propagate `CancellationToken` through DAO interfaces, implementations, and async query or save operations.
- Prefer `AsNoTracking()` for read-only queries. Keep tracking only when the operation intentionally updates an entity or relies on tracked state.
- Use server-side query predicates for tenant isolation. Do not load broader data and filter it in memory.
- Never invent credentials, connection strings, secrets, environmental facts, command output, or test results. Report missing prerequisites as blockers or verification gaps.
- Treat SQLite tests as useful behavioral tests, not proof of PostgreSQL compatibility. Check Npgsql translation, PostgreSQL types, constraints, indexes, casing, and transaction semantics when relevant. If PostgreSQL verification is unavailable, say so explicitly.
- Do not create or modify migrations unless the user requests migration work.
- Do not create or modify API endpoints unless the user requests endpoint work.
- Use synthetic, non-sensitive test data only.
- Avoid destructive database operations such as dropping, truncating, resetting, or recreating databases. Perform one only when the user explicitly requests it and the exact target has been verified.

## Completion standard

The task is complete only when the requested DAO behavior is implemented or reviewed, tenant isolation has been checked, appropriate tests exist, restore/build/test outcomes are reported truthfully, and remaining provider-specific or environmental risks are explicit.
