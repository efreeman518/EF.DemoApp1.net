# Package Greenfield Migration Guide (2026-02-18)

## Purpose
This guide documents the recent package and host cleanup focused on **greenfield development**. Compatibility shims and legacy Swagger-era artifacts were removed to simplify the API surface.

## Scope
Changes were applied primarily in:
- `Package/Package.*`
- `SampleApp.Api/.well-known/ai-plugin.json`
- Minor source/doc wording updates for Scalar/OpenAPI alignment

---

## 1) Breaking API Changes

### 1.1 Http resilience extension (typo shim removed)
**Removed** typo-compatible overload:
- `AddCustomResilience(..., List<int> excludedStatusCodes, int atteptTimeoutInSeconds, ...)`

**Use now**:
- `AddCustomResilience(..., IReadOnlyCollection<int> excludedStatusCodes, int attemptTimeoutInSeconds, ...)`

**Before**
```csharp
builder.AddCustomResilience(
    "pipeline",
    excludedStatusCodes: new List<int> { 400, 404 },
    atteptTimeoutInSeconds: 20);
```

**After**
```csharp
IReadOnlyCollection<int> excluded = new[] { 400, 404 };
builder.AddCustomResilience(
    "pipeline",
    excludedStatusCodes: excluded,
    attemptTimeoutInSeconds: 20);
```

### 1.2 String matching extension (legacy typo overload removed)
**Removed** overload:
- `FindTopMatches(..., bool prirotizeStartMatch = true, ...)`

**Use now**:
- `FindTopMatches(..., bool prioritizeStartMatch = true, ...)`
- Signature expects `IReadOnlyCollection<string>`.

**Before**
```csharp
var matches = "Alpha".FindTopMatches(values, prirotizeStartMatch: true);
```

**After**
```csharp
IReadOnlyCollection<string> valuesReadOnly = values;
var matches = "Alpha".FindTopMatches(valuesReadOnly, prioritizeStartMatch: true);
```

### 1.3 Chaos API naming cleanup
**Removed** typo members:
- `IChaosManager.OutcomHttpStatusCode()`
- `ChaosManager.OutcomHttpStatusCode()`
- `ChaosManagerSettings.OutcomHttpStatusCode`

**Use now**:
- `IChaosManager.OutcomeHttpStatusCode()`
- `ChaosManager.OutcomeHttpStatusCode()`
- `ChaosManagerSettings.OutcomeHttpStatusCode`

---

## 2) Swagger Elimination / Scalar Alignment

### 2.1 Runtime/package layer
- Legacy commented Swagger stubs were removed from package infrastructure.
- No Swagger runtime registration remains in package layer.

### 2.2 OpenAPI plugin manifest
Updated ChatGPT plugin manifest from Swagger YAML URL to OpenAPI JSON URL:
- **From**: `http://localhost:44318/swagger/v1.0/swagger.yaml`
- **To**: `http://localhost:44318/openapi/v1.json`

### 2.3 Terminology cleanup
Source/docs were updated from “swagger” wording to “scalar/openapi” where applicable.

---

## 3) Internal Consolidation (Non-breaking for public callers)

### 3.1 Refit call helpers
Shared logic was centralized in:
- `Package/Package.Infrastructure.Utility.UI/RefitCallHelperShared.cs`

This consolidates:
- API exception mapping
- ProblemDetails parsing and validation shaping
- Auth token-revocation detection data extraction
- Timeout/network/socket and WASM streaming error classification

Public helpers remain:
- `RefitCallHelperFull`
- `RefitCallHelperSlim`

### 3.2 Query/predicate hardening
- Removed query composition side effect (`ChangeTracker.Clear`) from `ComposeIQueryable`.
- Removed reflection-based DbContext accessor helper.
- Replaced `Expression.Invoke` predicate composition with EF-translatable parameter replacement.

---

## 4) Deleted Legacy Files
The following dead/commented files were removed:
- `Package/Package.Infrastructure.Common/PollyRetry.cs`
- `Package/Package.Infrastructure.Data/SqlRetry.cs`
- `Package/Package.Infrastructure.BackgroundService/ScopedServiceSettings.cs`
- `Package/Package.Infrastructure.AspNetCore/Swagger/SwaggerSettings.cs`
- `Package/Package.Infrastructure.AspNetCore/Swagger/SwaggerGenConfigurationOptions.cs`
- `Package/Package.Infrastructure.AspNetCore/Swagger/SwaggerDefaultValues.cs`

---

## 5) Migration Checklist
- [ ] Replace `atteptTimeoutInSeconds` callsites with `attemptTimeoutInSeconds`.
- [ ] Replace `prirotizeStartMatch` callsites with `prioritizeStartMatch`.
- [ ] Replace `OutcomHttpStatusCode` members with `OutcomeHttpStatusCode`.
- [ ] Ensure OpenAPI consumers use `/openapi/{document}.json` instead of `/swagger/...`.
- [ ] Validate endpoint docs via Scalar route(s) in host app.

---

## 6) Validation Performed
- Targeted unit tests for updated behaviors passed.
- Changed package projects build successfully after greenfield cleanup.
