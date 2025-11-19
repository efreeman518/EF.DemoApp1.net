# 1. Executive Summary

**Scenario**: Upgrade entire multi-project solution from .NET 9.0 to .NET 10.0 (Preview) including all dependent class libraries, application hosts (API, Gateway, gRPC, Workers, Functions, UI), infrastructure libraries, Aspire AppHost/ServiceDefaults, and comprehensive test suites.

**Scope**:  Fifty+ projects targeting net9.0 today; all recommended to move to net10.0. Numerous Microsoft.Extensions.*, EF Core, ASP.NET Core, System.* package upgrades; some packages now provided by the framework (NuGet.0003). One incompatibility (NuGet.0001) detected.

**Target State**: All projects target `net10.0`; obsolete package references removed where framework now includes functionality; all upgrade-recommended packages updated to exact suggested versions; incompatible or unsupported packages remediated; build succeeds without warnings; tests green.

**Selected Strategy**: Bottom-Up Strategy (Dependency-First, tiered). Chosen due to large number of projects, layered domain/application/infrastructure architecture, multiple application types, and need to limit risk in preview framework adoption.

**Complexity Assessment**: High overall. Reasoning: Large solution size (>50 projects), cross-cutting infrastructure abstractions, multiple hosting models (API, gRPC, WebAssembly, Functions, Workers, Aspire), EF Core data access layers, extensive test surface. Individual leaf domain libraries are low complexity; infrastructure/data medium; top-level hosts higher due to configuration changes and package breadth.

**Critical Issues**:
- Incompatible package: `Microsoft.VisualStudio.Azure.Containers.Tools.Targets (1.22.1)` in `SampleApp.Gateway` (NuGet.0001) – no supported version for net10.0. Must remove or replace containerization approach (use native `dotnet publish` with container support or update later when net10 version released).
- Several packages now redundant (NuGet.0003) e.g. `System.Linq`, `Microsoft.AspNetCore.WebUtilities`, `System.Net.Http`. Need removal to avoid redundancy.
- Preview framework (net10.0) increases potential for late breaking changes; enforce stricter validation.

**Recommended Approach**: Incremental Bottom-Up migration in tiers (leaf → infrastructure → application hosts → tests). Big Bang is rejected due to high project count and risk; staged approach improves isolation and learning.

# 2. Migration Strategy

## 2.1 Approach Selection
- **Chosen Strategy**: Bottom-Up Strategy.
- **Strategy Rationale**: Solution exhibits clear layering (Domain → Application Contracts & Services → Infrastructure (Data/Auth/Config/etc.) → Host Applications → Tests). Upgrading from the leaves prevents higher-tier projects from referencing older framework libraries. Minimizes need for multi-targeting.
- **Strategy-Specific Considerations**:
  - Each tier upgraded as a batch (single set of project file/package edits, single test cycle) before advancing.
  - Validation gates between tiers to ensure stability.
  - Lessons learned (e.g., EF Core 10 changes) applied downstream.

**Justification**:
- Project count > 5 (≈55) => incremental required.
- Mixed concerns (data, messaging, UI, workers) => isolate risk.
- Preview target => need rollback checkpoints.

## 2.2 Dependency-Based Ordering
We categorize into tiers using assumed architectural layering (explicit dependencies not fully listed in assessment; assumptions documented). Rule: Tier N depends only on tiers < N.
- Leaf domain & shared contracts first (no internal project dependencies).
- Then cross-cutting infrastructure abstractions without EF or host-specific coupling.
- Then data/access/EF infrastructure (requires domain).
- Then higher infrastructure wrappers (Auth, Grpc, Messaging, Storage, Cloud service wrappers) depending on prior tiers.
- Then application composition/bootstrap (Bootstrapper, Host, Configuration).
- Finally host applications (API, Gateway, gRPC, UI, Functions, Workers, Aspire) and test projects.

No circular dependencies known (assumption). Must verify during execution.

## 2.3 Parallel vs Sequential Execution
- Inside a tier: parallel edits permissible (batch commit) because they share dependency posture.
- Between tiers: strictly sequential; do not start next tier until prior passes validation.
- High-risk items (Gateway with incompatible package, EF Core migrations) handled with focused review in their tier.

# 3. Detailed Dependency Analysis

## 3.1 Dependency Graph Summary (Abstracted Tiers)
```
Tier 6: [Test.*] [Package.Infrastructure.Test.*] [PlaywrightUI] [SeleniumUI]
          ↓
Tier 5: [SampleApp.Api] [SampleApp.Gateway] [SampleApp.Grpc] [SampleApp.UI1] [Functions] [SampleApp.BackgroundServices] [Worker.Scheduler] [Console.AI1] [SampleApp.AppHost] [SampleApp.ServiceDefaults]
          ↓
Tier 4: [SampleApp.Bootstrapper] [Package.Infrastructure.Host] [Infrastructure.Configuration]
          ↓
Tier 3: [Package.Infrastructure.Auth] [Package.Infrastructure.Grpc] [Package.Infrastructure.Messaging] [Package.Infrastructure.Storage] [Package.Infrastructure.Cache] [Package.Infrastructure.KeyVault] [Package.Infrastructure.OpenAI] [Package.Infrastructure.AzureOpenAI] [Package.Infrastructure.BlandAI] [Package.Infrastructure.MSGraph] [Infrastructure.RapidApi] [Infrastructure.JobsApi]
          ↓
Tier 2: [Package.Infrastructure.Data] [Package.Infrastructure.Data.Contracts] [Infrastructure.Data] [Infrastructure.Repositories] [Infrastructure.SampleApi] [Package.Infrastructure.CosmosDb] [Package.Infrastructure.Table]
          ↓
Tier 1: [Domain.Shared] [Domain.Model] [Domain.Rules] [Application.Contracts] [Package.Infrastructure.Common.Contracts] [Package.Infrastructure.Common] [Package.Infrastructure.BackgroundServices] [Package.Infrastructure.Utility] [Package.Infrastructure.Utility.UI]
          ↓
Tier 0 (Implicit): External NuGet dependencies
```
(Assumptions: Domain libraries are foundational; Application.Contracts may depend on Domain.*; Infrastructure.Data depends on Domain and Contracts; Higher infrastructure wrappers depend on data + common libs; Bootstrapper/Host aggregate infrastructure; Applications depend on all.)

## 3.2 Project Groupings / Phases
- **Phase 0**: Environment & SDK validation (ensure .NET 10 SDK installed); branch creation (`upgrade-to-NET10-1`) if not yet created.
- **Phase 1 (Tier 1)**: Domain & foundational infrastructure/common utility libraries.
- **Phase 2 (Tier 2)**: Data-related infrastructure (EF Core, repositories, storage abstractions with direct data access).
- **Phase 3 (Tier 3)**: Higher-level service integrations (Auth, Messaging, Grpc, Cache, Cloud service wrappers, External API integrations).
- **Phase 4 (Tier 4)**: Composition and configuration hosts (Bootstrapper, Host, Configuration aggregator).
- **Phase 5 (Tier 5)**: Executable / host applications (API, Gateway, gRPC service, UI (WebAssembly), Functions, Worker services, Aspire AppHost/ServiceDefaults, BackgroundServices console apps).
- **Phase 6 (Tier 6)**: Test projects (Unit, Integration, Endpoints, Architecture, Benchmark, Load, UI tests, Package.Infrastructure.Test.*).

## Strategy-Specific Grouping Notes
- Batch package upgrades per tier; do not split per project.
- Validation gate after each phase: build + targeted regression tests for consumers referencing upgraded tier.

# 4. Project-by-Project Migration Plans
(For brevity, similar patterns consolidated; executor must still address each project.)

Template below applied to each project; specific package table includes only packages flagged.

### Phase 1 / Tier 1 Projects
#### Project: Domain.Shared
Current State: net9.0; packages: (none flagged).
Target State: net10.0.
Migration Steps:
1. Prerequisites: SDK net10 installed.
2. Framework Update: Change `<TargetFramework>net9.0</TargetFramework>` to `net10.0`.
3. Package Updates: None.
4. Expected Breaking Changes: Minimal; possible minor BCL changes.
5. Code Modifications: Adjust obsolete APIs if compiler reports.
6. Testing: Run unit tests referencing Domain.Shared (later in Phase 6).
7. Validation Checklist: Build clean.

#### Project: Domain.Model
(Current target net9.0 → net10.0; no package upgrades.)
Same steps as Domain.Shared.

#### Project: Domain.Rules
Same as Domain.Shared.

#### Project: Application.Contracts
Same as Domain.Shared.

#### Project: Package.Infrastructure.Common.Contracts
Packages:
| Package | Current | Target | Reason |
| System.Text.Json | 9.0.10 | 10.0.0 | Upgrade recommended (NuGet.0002) |
Migration: Update package reference; ensure any serializer options unaffected (net10 may add features).

#### Project: Package.Infrastructure.Common
Packages:
| Package | Current | Target | Reason |
| Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions | 9.0.10 | 10.0.0 | Upgrade recommended |
| Microsoft.Extensions.Http.Polly | 9.0.10 | 10.0.0 | Upgrade recommended |
| Microsoft.Extensions.Logging | 9.0.10 | 10.0.0 | Upgrade recommended |

#### Project: Package.Infrastructure.BackgroundServices
Packages:
| Package | Current | Target | Reason |
| Microsoft.Extensions.Hosting | 9.0.10 | 10.0.0 | Upgrade recommended |
| Microsoft.Extensions.Logging.Abstractions | 9.0.10 | 10.0.0 | Upgrade recommended |

#### Project: Package.Infrastructure.Utility
No package upgrades.

#### Project: Package.Infrastructure.Utility.UI
Packages:
| Package | Current | Target | Reason |
| Microsoft.Extensions.Http | 9.0.10 | 10.0.0 | Upgrade recommended |

(Other Tier 1 projects follow same pattern; remove redundancy in execution by batching edits.)

Validation for Phase 1:
- Build all Tier 1 projects.
- Smoke compile of a representative consumer (e.g., Infrastructure.Data still net9 referencing net10 libs should work due to upward compatibility). If issues, hold Phase 2.

### Phase 2 / Tier 2 Projects (Data & Repositories)
Common Package Upgrades:
- EF Core packages: `Microsoft.EntityFrameworkCore`, `Relational`, `SqlServer`, `Tools`, `Design` → 10.0.0
- System.Formats.Asn1 → 10.0.0 (where present)
Projects: Package.Infrastructure.Data, Package.Infrastructure.Data.Contracts, Infrastructure.Data, Infrastructure.Repositories, Infrastructure.SampleApi, Package.Infrastructure.CosmosDb, Package.Infrastructure.Table.

Sample project specifics:
#### Project: Package.Infrastructure.Data
Packages:
| Package | Current | Target | Reason |
| Microsoft.EntityFrameworkCore | 9.0.10 | 10.0.0 | Upgrade recommended |
| Microsoft.EntityFrameworkCore.Relational | 9.0.10 | 10.0.0 | Upgrade recommended |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.10 | 10.0.0 | Upgrade recommended |
| System.Formats.Asn1 | 9.0.10 | 10.0.0 | Upgrade recommended |
Breaking Changes Expectations: EF Core 10 may adjust LINQ translation, query performance, provider behaviors. Review migration docs; run migrations recompile.
Code Modifications: Update obsolete EF APIs, re-run design-time scaffolding if needed.

#### Project: Infrastructure.Data
(Add Tools & Design packages upgrade.)
Packages add:
| Microsoft.EntityFrameworkCore.Tools | 9.0.10 | 10.0.0 |
| Microsoft.EntityFrameworkCore.Design | 9.0.10 | 10.0.0 |

Validation Phase 2:
- Build Tier 2.
- Run minimal integration tests against in-memory/database (if available) BEFORE upgrading higher tiers.
- Confirm EF migrations apply.

### Phase 3 / Tier 3 Projects (Service Integrations)
Common Package Upgrades:
- Microsoft.Extensions.* (Options, Logging.Abstractions, Http, Http.Polly, DependencyInjection, HealthChecks, etc.) → 10.0.0
- Remove packages now covered by framework (System.Linq, etc.).
Projects: Auth, Grpc, Messaging, Storage, Cache, KeyVault, OpenAI, AzureOpenAI, BlandAI, MSGraph, Infrastructure.RapidApi, Infrastructure.JobsApi.

Example: Package.Infrastructure.AzureOpenAI
Packages:
| Package | Current | Target | Reason |
| Microsoft.Extensions.Options | 9.0.10 | 10.0.0 | Upgrade recommended |
| System.Linq | 4.3.0 | (Remove) | Included in framework |
Breaking Changes: Options binding minor changes; remove explicit System.Linq reference.

Validation Phase 3:
- Build Tier 1-3.
- Run service integration smoke tests (e.g., authentication, basic gRPC call, messaging publish). Use test harness if exists (still net9). Ensure no runtime binding failures.

### Phase 4 / Tier 4 Projects (Composition)
Projects: SampleApp.Bootstrapper, Package.Infrastructure.Host, Infrastructure.Configuration.
Package Groups include extensive Microsoft.Extensions.* and EF InMemory in Bootstrapper / Configuration.
Key Steps:
- Update all Microsoft.Extensions.* packages as listed.
- Watch for configuration binding changes (Binder upgrade) and DI aggregated registration differences.
- Validate health checks assembly upgrades (EntityFrameworkCore health checks).

Validation Phase 4:
- Start Bootstrapper on net10 referencing net10 infrastructure tiers.
- Ensure DI container builds; configuration sources load; health checks endpoint (if any) works.

### Phase 5 / Tier 5 Projects (Applications & Hosts)
Projects: SampleApp.Api, SampleApp.Gateway, SampleApp.Grpc, SampleApp.UI1 (WebAssembly), Functions, SampleApp.BackgroundServices, Worker.Scheduler, Console.AI1, SampleApp.AppHost (Aspire), SampleApp.ServiceDefaults.

Common Upgrades:
- ASP.NET Core packages: Authentication.JwtBearer, HeaderPropagation, OpenApi → 10.0.0
- Service Discovery packages → 10.0.0
- Blazor WebAssembly packages → 10.0.0; remove WebUtilities (framework included).
- Aspire.Hosting.AppHost → 13.0.0
- OpenTelemetry instrumentation packages → 1.14.0

Special Case: SampleApp.Gateway
- Incompatible package `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` (1.22.1). Action: Remove reference; adopt `EnableDefaultItems` combined with `ContainerBaseImage` (if using .NET 8+ built-in publish containers) or external Dockerfile. Document removal in commit.

Functions Project:
- Azure Functions Worker packages updated to specified versions; confirm net10 compatibility. (Preview risk— run local function host tests.)

Validation Phase 5:
- Launch each host application minimally.
- API/Grpc: Start, respond to health/ping endpoint.
- UI: Build & load basic page.
- Gateway: Reverse proxy functionality test without container tools package.
- Functions: Run sample function invocation locally.
- Background services/workers: Start and process one cycle.
- Aspire AppHost + ServiceDefaults start orchestration.

### Phase 6 / Tier 6 Projects (Tests)
Projects: Test.Unit, Test.Integration, Test.Endpoints, Test.Support, Test.Architecture, Test.Console, Test.Benchmarks, Test.Load, Test.PlaywrightUI, Test.SeleniumUI, Package.Infrastructure.Test.Unit, Package.Infrastructure.Test.Integration, Package.Infrastructure.Test.Benchmarks.
Packages: Upgrade Microsoft.Extensions.* & EF Core where present; UI test projects update configuration packages; remove redundant packages now in framework.
Validation Phase 6:
- Run full test suite.
- Benchmark & Load tests comparison vs baseline (optional due to preview).
- Architecture tests confirm enforced layering (update if net10 changes reflection behaviors).

(Repeat per-project template where packages listed in assessment; executor references assessment.md for exact versions.)

# 5. Risk Management

## 5.1 High-Risk Changes
| Project | Risk | Mitigation |
|---------|------|------------|
| SampleApp.Gateway | Incompatible container tools package | Remove package; use built-in container publish or standard Docker; verify deployment pipeline adjustment |
| Data Layer (Infrastructure.Data, Package.Infrastructure.Data) | EF Core major upgrade (9→10) | Run migration diff tests; validate queries; enable detailed logs; fallback to EF 9 branch if critical issue |
| Functions | Azure Functions worker multiple version jumps | Isolated test host run; can keep on net9 until stable if blocking issues found |
| UI (SampleApp.UI1) | Blazor WebAssembly dependency upgrades | Build & smoke test; watch for breaking APIs (e.g. render lifecycle adjustments) |
| Aspire.AppHost / ServiceDefaults | Major version jump (AppHost 9.5.2 → 13.0.0) | Review Aspire release notes; run minimal orchestrations early in Phase 5 |
| Auth/Grpc/Messaging | Cross-cutting DI & protocol interactions | Incremental integration tests after Tier 3 |

## 5.2 Medium / Low risks
- Domain libraries: low.
- Utility libs: low.
- Health check & configuration libs: medium (binding changes).

## 5.3 Contingency Plans
- Rollback: Keep branch `upgrade-to-NET10-1`; if phase fails, revert to last tag/commit before phase start.
- If EF Core blocking: Pause upgrade; isolate data layer on net9 with multi-target? (Fallback plan) Document decision.
- If Functions incompatible: Defer Functions project to separate mini-phase after RTM.
- Gateway container build fallback: Use Dockerfile with `FROM mcr.microsoft.com/dotnet/aspnet:9.0` until net10 container image stable; later re-upgrade to net10.

# 6. Testing and Validation Strategy

## 6.1 Phase-by-Phase
Phase 1: Compile-only & static analysis.
Phase 2: Data integration tests (CRUD, migrations).
Phase 3: External service stubs; authentication token issuance; simple gRPC call.
Phase 4: End-to-end composition start (without full traffic).
Phase 5: Application startup smoke tests & selective endpoint tests; UI rendering; Functions invocation; Worker execution.
Phase 6: Full test suite including performance & UI automation.

## 6.2 Smoke Tests (per project class)
- Library: Build success.
- EF Data project: Migrate in-memory DB; run one query.
- API/Gateway/Grpc: /health or /swagger loads.
- UI: Home page renders without console errors.
- Worker/Background: Service starts and logs heartbeat.
- Functions: Local host executes a sample trigger.

## 6.3 Comprehensive Validation
- All unit & integration tests pass.
- No build warnings.
- Benchmark variance within acceptable threshold (<10% regression).
- Security scan with updated dependencies (no vulnerabilities flagged in assessment baseline).

# 7. Timeline and Effort Estimates (Indicative)
| Phase | Complexity | Estimated Time | Notes |
|-------|------------|----------------|-------|
| 0 | Low | 0.5 day | SDK & branch setup |
| 1 | Low | 1 day | Leaf batch simple edits |
| 2 | High | 2-3 days | EF Core adjustments & migration validation |
| 3 | Medium | 2 days | Multiple service packages upgrades |
| 4 | Medium | 1 day | Composition & config validation |
| 5 | High | 3-4 days | Hosts variety & Aspire/AppHost changes |
| 6 | Medium | 2 days | Full test battery, perf/UI |
| Buffer | - | 1-2 days | Preview instability contingency |
| Total | High | 12-15 days | Includes buffer |

Per-project (sample selection):
| Project | Complexity | Est. Time | Dependencies | Risk Level |
| Domain.Model | Low | <0.25d | None | Low |
| Infrastructure.Data | High | 1d | Domain.* | High |
| Package.Infrastructure.Auth | Medium | 0.5d | Domain, Data | Medium |
| SampleApp.Api | High | 1d | All lower tiers | High |
| SampleApp.Gateway | High | 1d | All lower tiers | High |
| Functions | Medium | 0.75d | Lower tiers | Medium |
| Test.Integration | Medium | 0.75d | All | Medium |

# 8. Source Control Strategy

## 8.1 Strategy Guidance
- Single upgrade branch `upgrade-to-NET10-1`. Each phase results in one or more commits; optional PR per phase for review.

## 8.2 Branching
- If `upgrade-to-NET10-1` not yet created: create from `upgrade-to-NET10` before Phase 1.
- Feature sub-branches optional for complex phases (e.g., `feat/net10-phase2-efcore`). Merge back after validation.

## 8.3 Commit Strategy
- Commit after completing batch edits + successful build per phase.
- Commit message template: `chore(net10): upgrade <phase name> to net10 + package updates`
- If removing packages: `refactor(net10): remove redundant package <name> (framework included)`
- Incompatible removal: `fix(net10): remove incompatible package Microsoft.VisualStudio.Azure.Containers.Tools.Targets`.

## 8.4 Review & Merge
- PR per phase to main (optional until RTM) for visibility.
- Review checklist: Build success, package versions correct, no warnings, minimal diff scope, rollback plan documented.

# 9. Success Criteria

## 9.1 Strategy-Specific
- All tiers upgraded sequentially; no higher-tier started before lower-tier validation.

## 10.2 Technical Success Criteria
- [ ] All projects target net10.0
- [ ] All recommended packages upgraded to specified versions
- [ ] Redundant packages removed
- [ ] Incompatible package replaced/removed
- [ ] Builds succeed without warnings
- [ ] All tests pass
- [ ] No security vulnerabilities (baseline maintained)

## 10.3 Quality Criteria
- [ ] Performance within baseline tolerance
- [ ] Documentation (README/upgrade notes) updated
- [ ] Logging & diagnostics intact

## 10.4 Process Criteria
- [ ] Bottom-Up tier order respected
- [ ] Phase validations executed & recorded
- [ ] Source control strategy applied

# 10. Breaking Changes Catalog (Expected)

Category | Potential Impact | Mitigation
-------- | ---------------- | ---------
EF Core 10 | Query translation or migration differences | Run migration generation & apply; review logs
ASP.NET Core 10 | Middleware behavior / configuration binding subtle changes | Regression tests on startup & auth flows
Blazor WebAssembly | Rendering lifecycle changes | Smoke test UI, update lifecycle methods if warnings
Azure Functions Worker | Binding extensions updated semantics | Re-test triggers; consult release notes
Microsoft.Extensions.Options / Configuration | Binding for complex objects may warn | Add explicit configuration binding or options validators
Removal of redundant packages | Build script/package restore differences | Clean restore after removal; update docs

# 11. Rollback Plan
- Tag branch before each phase: `net10-phaseN-start`.
- If phase fails: revert to tag, create fix branch to address blockers (or postpone project(s)).
- Maintain list of removed packages for quick re-add if needed.

# 12. Assumptions & Gaps
- Exact project dependency graph not fully enumerated in assessment; tiers inferred from naming conventions. Must confirm with actual project references before execution.
- No security vulnerabilities currently; if new ones appear post-upgrade, treat as high priority.
- Preview framework stability assumed adequate; if blocking preview bug emerges, consider deferring affected host projects.

# 13. Phase Completion Checklists
Phase 1:
- [ ] All Tier 1 projects build net10
- [ ] No new warnings
Phase 2:
- [ ] EF migrations succeed
- [ ] CRUD smoke tests pass
Phase 3:
- [ ] Auth / Messaging / Grpc basic flows validated
Phase 4:
- [ ] Bootstrapper & Host start successfully
Phase 5:
- [ ] All applications start & respond
- [ ] UI renders homepage
- [ ] Functions trigger executes
Phase 6:
- [ ] All tests green
- [ ] Benchmarks within tolerance

# 14. Execution Sequence Summary
1. Phase 0: Prepare environment & branch.
2. Phase 1: Upgrade foundational libraries.
3. Phase 2: Upgrade data layer (EF Core) & validate migrations.
4. Phase 3: Upgrade service integration libraries.
5. Phase 4: Upgrade composition/configuration hosts.
6. Phase 5: Upgrade all application/host projects.
7. Phase 6: Upgrade & run all tests.
8. Final validation & documentation.

"Done" when all success criteria checklists satisfied.
