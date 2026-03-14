# RotoGLBridge.UI .NET 10 Upgrade Tasks

## Overview

This document tracks the execution of upgrading RotoGLBridge.UI from .NET 8.0 to .NET 10.0 (LTS). The single project will be upgraded atomically, followed by comprehensive testing and validation.

**Progress**: 1/3 tasks complete (33%) ![0%](https://progress-bar.xyz/33)

---

## Tasks

### [✓] TASK-001: Atomic framework and dependency upgrade *(Completed: 2026-03-11 04:25)*
**References**: Plan §Phase 1, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [✓] (1) Update TargetFramework in `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj`: net8.0-windows → net10.0-windows
- [✓] (2) TargetFramework updated to net10.0-windows (**Verify**)
- [✓] (3) Update package references in `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj` per Plan §Package Update Reference (Microsoft.Extensions.DependencyInjection 10.0.4, Microsoft.Extensions.Logging 10.0.4, Microsoft.Extensions.DependencyInjection.Abstractions 10.0.4, Microsoft.Extensions.Logging.Abstractions 10.0.4)
- [✓] (4) All package references updated (**Verify**)
- [✓] (5) Validate MaterialDesignThemes 5.3.0 and ScottPlot.WPF 5.1.57 remain unchanged (both flagged but likely compatible)
- [✓] (6) Restore all dependencies
- [✓] (7) All dependencies restored successfully (**Verify**)
- [✓] (8) Build solution and fix all compilation errors per Plan §Breaking Changes Catalog (none expected)
- [✓] (9) Solution builds with 0 errors (**Verify**)

---

### [▶] TASK-002: Run test suite and validate upgrade
**References**: Plan §Phase 2 Testing

- [ ] (1) Run tests in RotoGLBridge.Tests project
- [ ] (2) Fix any test failures (reference Plan §Breaking Changes for common issues)
- [ ] (3) Re-run tests after fixes
- [ ] (4) All tests pass with 0 failures (**Verify**)

---

### [ ] TASK-003: Final commit
**References**: Plan §Source Control Strategy

- [ ] (1) Commit all changes with message: "Upgrade RotoGLBridge.UI to .NET 10 LTS - Framework: net8.0-windows → net10.0-windows - Packages: Microsoft.Extensions.* 10.0.3 → 10.0.4 - All tests pass, application validated"

---



