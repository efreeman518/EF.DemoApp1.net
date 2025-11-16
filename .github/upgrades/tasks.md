# Upgrade solution to `net10.0` (Bottom‑Up, tiered)

## Overview

Upgrade multi-project solution from `net9.0` → `net10.0` using the Bottom‑Up (dependency‑first) strategy defined in the Plan. Tasks follow the Plan phases/tiers; each upgrade task batches project file updates + package updates + restore + build+fix for a tier, then a separate test/validation task runs bounded verifications before advancing. Commits are performed per phase per Plan §Source Control Strategy.

**Progress**: 2/14 tasks complete (14%) ![14%](https://progress-bar.xyz/14)

## Tasks

### [✓] TASK-001: Verify prerequisites (SDK & `global.json`) *(Completed: 2025-11-15 21:03)*
**References**: Plan §Phase 0 (Prerequisites), Plan §Source Control Strategy

- [✓] (1) Verify .NET 10 SDK is installed and available on PATH (per Plan §Phase 0).
- [✓] (2) Update `global.json` to target SDK `10.x` if Plan requires and commit the file as part of the subsequent phase tasks (see Plan §Phase 0).
- [✓] (3) Verify required tool versions (build tools, CLI extensions) per Plan §Phase 0 (**Verify**: `dotnet --version` shows a 10.x preview SDK and `dotnet --info` shows expected workloads).

---

### [✓] TASK-002: Upgrade Tier 1 (Domain & foundational libraries) — project & package edits *(Completed: 2025-11-15 21:12)*
**References**: Plan §Phase 1 (Tier 1), Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [✓] (1) Update `<TargetFramework>` to `net10.0` in all projects listed in Plan §Phase 1.
- [✓] (2) Update package references for Tier 1 projects per Plan §Package Update Reference (batch package updates for the tier).
- [✓] (3) Restore dependencies (`dotnet restore`) for the solution/tier.
- [✓] (4) Build Tier 1 projects and fix compilation errors per Plan §Breaking Changes Catalog (single pass of fixes scoped to upgrade changes).
- [✓] (5) Solution/Tier 1 projects build with 0 errors (**Verify**).
- [✓] (6) Commit changes: `chore(net10): upgrade Tier 1 (domain & foundational) to net10 + package updates` (**Verify** commit success).

### [⊘] TASK-003: Validate Tier 1 (bounded tests & smoke)
**References**: Plan §Phase 1 Validation, Plan §Breaking Changes Catalog

- [⊘] (1) Run specified consumer/regression tests that depend on Tier 1 per Plan §Phase 1 Validation (run explicit test projects listed in Plan §Phase 1; example: `dotnet test path/to/Test.Domain.Unit.csproj`) — run only the named projects referenced in the Plan.
- [⊘] (2) Record test failures; fix deterministic failures limited to upgrade scope (one fix pass).
- [⊘] (3) Re-run the same specified test projects once to verify fixes.
- [⊘] (4) All specified tests passed with 0 failures (**Verify**).

---

### [▶] TASK-004: Upgrade Tier 2 (Data & EF Core projects)
**References**: Plan §Phase 2 (Tier 2), Plan §Breaking Changes Catalog (EF Core 10 notes), Plan §Package Update Reference

- [ ] (1) Update `<TargetFramework>` to `net10.0` for all Tier 2 projects listed in Plan §Phase 2.
- [ ] (2) Update EF Core and related package references to target versions per Plan §Package Update Reference (batch updates across Tier 2).
- [ ] (3) Restore dependencies (`dotnet restore`) for the solution/tier.
- [ ] (4) Build Tier 2 projects and fix compilation errors per Plan §Breaking Changes Catalog (single pass; include EF migration API adjustments).
- [ ] (5) Tier 2 projects build with 0 errors (**Verify**).
- [ ] (6) Commit changes: `chore(net10): upgrade Tier 2 (data & EF) to net10 + package updates` (**Verify** commit success).

### [ ] TASK-005: Validate Tier 2 (data integration smoke)
**References**: Plan §Phase 2 Validation, Plan §Breaking Changes Catalog

- [ ] (1) Run targeted data integration tests or in-memory DB checks listed in Plan §Phase 2 Validation (explicit project names from Plan; e.g., `dotnet test path/to/Infrastructure.Data.Tests.csproj`).
- [ ] (2) Fix deterministic failures limited to upgrade scope (one pass).
- [ ] (3) Re-run the same specified tests once.
- [ ] (4) All specified tests passed with 0 failures (**Verify**).

---

### [ ] TASK-006: Upgrade Tier 3 (Service integration libraries)
**References**: Plan §Phase 3 (Tier 3), Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [ ] (1) Update `<TargetFramework>` to `net10.0` for all Tier 3 projects per Plan §Phase 3.
- [ ] (2) Batch-update Microsoft.Extensions.* and other integration packages per Plan §Package Update Reference; remove redundant packages now provided by framework (per Plan §NuGet.0003).
- [ ] (3) Restore dependencies.
- [ ] (4) Build Tier 3 projects and fix compilation errors per Plan §Breaking Changes Catalog.
- [ ] (5) Tier 3 projects build with 0 errors (**Verify**).
- [ ] (6) Commit changes: `chore(net10): upgrade Tier 3 (service integrations) to net10 + package updates` (**Verify**).

### [ ] TASK-007: Validate Tier 3 (integration smoke)
**References**: Plan §Phase 3 Validation

- [ ] (1) Run targeted integration tests and minimal service stubs listed in Plan §Phase 3 Validation (explicit test project names referenced in the Plan).
- [ ] (2) Fix deterministic failures limited to upgrade impacts (one pass).
- [ ] (3) Re-run the same specified tests once.
- [ ] (4) All specified tests passed with 0 failures (**Verify**).

---

### [ ] TASK-008: Upgrade Tier 4 (Composition & configuration hosts)
**References**: Plan §Phase 4 (Tier 4), Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [ ] (1) Update `<TargetFramework>` to `net10.0` for Tier 4 projects listed in Plan §Phase 4 (`SampleApp.Bootstrapper`, `Package.Infrastructure.Host`, `Infrastructure.Configuration`, etc.).
- [ ] (2) Batch-update packages per Plan §Package Update Reference (watch configuration/binder packages).
- [ ] (3) Restore dependencies.
- [ ] (4) Build Tier 4 projects and fix compilation errors per Plan §Breaking Changes Catalog.
- [ ] (5) Tier 4 projects build with 0 errors (**Verify**).
- [ ] (6) Commit changes: `chore(net10): upgrade Tier 4 (composition & config) to net10 + package updates` (**Verify**).

### [ ] TASK-009: Validate Tier 4 (composition start)
**References**: Plan §Phase 4 Validation

- [ ] (1) Start bootstrapper/host in a minimal configuration (no manual UI checks) and verify DI container builds and health checks load (use automated health probes defined in Plan §Phase 4 Validation).
- [ ] (2) Fix deterministic startup errors (one pass).
- [ ] (3) Re-run automated startup checks once.
- [ ] (4) Startup/health checks succeed (**Verify**).

---

### [ ] TASK-010: Upgrade Tier 5 (Application hosts: API, Gateway, gRPC, UI, Functions, Workers)
**References**: Plan §Phase 5 (Tier 5), Plan §Package Update Reference, Assessment #NuGet.0001 (SampleApp.Gateway incompatible package), Plan §Breaking Changes Catalog

- [ ] (1) Update `<TargetFramework>` to `net10.0` for all Tier 5 projects listed in Plan §Phase 5.
- [ ] (2) Batch-update ASP.NET Core, Blazor, Functions, Aspire/AppHost, and hosting packages per Plan §Package Update Reference.
- [ ] (3) Address Assessment #NuGet.0001: remove or replace `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` in `SampleApp.Gateway` per Plan guidance (document removal in commit).
- [ ] (4) Restore dependencies.
- [ ] (5) Build Tier 5 projects and fix compilation errors per Plan §Breaking Changes Catalog.
- [ ] (6) Tier 5 projects build with 0 errors (**Verify**).
- [ ] (7) Commit changes: `chore(net10): upgrade Tier 5 (hosts) to net10 + package updates; note: remove incompatible packaging tool where applicable` (**Verify** commit success).

### [ ] TASK-011: Validate Tier 5 (host smoke tests)
**References**: Plan §Phase 5 Validation, Assessment #NuGet.0001

- [ ] (1) Run bounded automated smoke checks for each host as listed in Plan §Phase 5 Validation (examples in Plan: `API /health`, gRPC ping, Blazor build verification, Functions local invocation) — run only the explicit automated checks referenced in the Plan.
- [ ] (2) Fix deterministic failures limited to upgrade scope (one pass).
- [ ] (3) Re-run same automated checks once.
- [ ] (4) All specified smoke checks pass (**Verify**).

---

### [ ] TASK-012: Upgrade Tier 6 (Test projects) — bring test projects to `net10.0`
**References**: Plan §Phase 6 (Tier 6), Plan §Package Update Reference

- [ ] (1) Update `<TargetFramework>` to `net10.0` for all test projects listed in Plan §Phase 6 (unit, integration, endpoint, UI automation projects).
- [ ] (2) Update package references in test projects per Plan §Package Update Reference (batch updates).
- [ ] (3) Restore dependencies.
- [ ] (4) Build test projects and fix compilation errors per Plan §Breaking Changes Catalog.
- [ ] (5) Test projects build with 0 errors (**Verify**).
- [ ] (6) Commit changes: `chore(net10): upgrade test projects to net10 + package updates` (**Verify**).

### [ ] TASK-013: Run full test suite (bounded, explicit)
**References**: Plan §Phase 6 Validation (Full test suite)

- [ ] (1) Run explicit test projects listed in Plan §Phase 6 using `dotnet test <project>` for each listed project (examples from Plan: `Test.Unit`, `Test.Integration`, `Test.Endpoints`, `Test.PlaywrightUI`, `Test.SeleniumUI` — run only projects named in Plan).
- [ ] (2) Fix deterministic failures limited to upgrade impacts (one pass, reference Plan §Breaking Changes for common fixes).
- [ ] (3) Re-run the same explicit test projects once to verify fixes.
- [ ] (4) All specified tests passed with 0 failures (**Verify**).

---

### [ ] TASK-014: Finalize upgrade deliverables (docs, tags, checklist)
**References**: Plan §Commit Strategy, Plan §Phase Completion Checklists, Plan §Success Criteria

- [ ] (1) Update upgrade documentation / `README` / upgrade notes summarizing changes, removed packages (e.g., per Assessment #NuGet.0003), and known caveats.
- [ ] (2) Run the Plan §Success Criteria checklist and verify: all projects target `net10.0`, recommended packages updated, redundant packages removed, incompatible packages addressed, builds succeed without warnings, tests green (**Verify**).
- [ ] (3) Commit documentation changes: `docs(upgrade): record net10 migration notes` (**Verify**).
- [ ] (4) Create a tag for the completed upgrade branch (optional per Plan §Source Control Strategy): `net10-upgrade-complete` (verify tag created) — do not merge branches automatically.

---

Generation checklist (applied):
- Strategy batching rules (Bottom‑Up tiered) applied: upgrade + package + compilation per tier combined; tests separated per tier.
- Large lists referenced from Plan §Phase N and Plan §Package Update Reference (no long duplication).
- Verifications deterministic and bounded (no unbounded retry loops).
- Non-automatable/manual UI checks excluded; only automated probes and test projects included.
- Commit actions included per phase per Plan §Source Control Strategy.

Notes:
- When running tasks, use explicit project paths/names from Plan §Phase N and Assessment files for deterministic execution. Example test project names are provided above only as examples; the executor must use the exact names referenced in Plan §Phase 6 and `assessment.md`.