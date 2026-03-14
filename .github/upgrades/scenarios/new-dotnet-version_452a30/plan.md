# .NET 10 Upgrade Plan

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Risk Management](#risk-management)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description

This plan details the upgrade of **RotoGLBridge.UI** from .NET 8.0 to .NET 10.0 (LTS). The solution contains 12 projects, but only one project requires a framework upgrade. All other projects in the solution are already targeting .NET 10.0 or .NET 10.0-windows.

### Scope

**Projects Requiring Upgrade:** 1 of 12
- `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj` (net8.0-windows → net10.0-windows)

**Projects Already on Target Framework:** 11 projects
- All dependency projects already on net10.0 or net10.0-windows
- No cascading framework updates needed

### Key Metrics

| Metric | Value | Assessment |
|--------|-------|------------|
| **Total Projects** | 12 | Only 1 needs upgrade |
| **Projects to Upgrade** | 1 | Very focused scope |
| **Package Updates Required** | 6 | 4 recommended, 2 incompatible flags |
| **API Breaking Changes** | 0 | None identified |
| **Lines of Code (UI Project)** | 895 | Small codebase |
| **Total Solution LOC** | 9,200 | Minimal impact |
| **Estimated LOC to Modify** | 0+ | No code changes expected |
| **Complexity Rating** | ✅ **Simple** | Single project, minimal dependencies |

### Target State

- **RotoGLBridge.UI**: net10.0-windows
- **All Dependencies**: Already on net10.0-windows (no changes needed)
- **Package Versions**: Update Microsoft.Extensions.* packages from 10.0.3 → 10.0.4
- **Special Attention**: MaterialDesignThemes and ScottPlot.WPF flagged as incompatible (requires validation)

### Critical Issues

**⚠️ Package Compatibility Concerns:**
1. **MaterialDesignThemes 5.3.0**: Flagged as incompatible with suggested version 5.2.1 (downgrade indicator - requires investigation)
2. **ScottPlot.WPF 5.1.57**: Flagged as incompatible with suggested version 4.1.73 (downgrade indicator - requires investigation)

Both packages may actually be compatible with .NET 10, as they are currently working with dependencies already on .NET 10. These flags may be false positives from the analysis tool.

### Selected Strategy

**All-At-Once Strategy** - Single atomic upgrade operation

**Rationale:**
- ✅ Only 1 project requires framework change
- ✅ All dependencies already on target framework (net10.0-windows)
- ✅ Simple, clear dependency structure (UI → RotoGLBridge → plugins/helpers)
- ✅ Small codebase (895 LOC)
- ✅ No API breaking changes identified
- ✅ Low complexity, low risk
- ✅ Fastest path to completion

**Approach:**
This is effectively a single-project upgrade. All project file updates and package updates will be performed in one coordinated operation, followed by build validation and testing.

### Complexity Classification

**Simple** - Single project upgrade with minimal complexity:
- 1 project requiring framework update
- 0 dependency projects needing updates (all already on target)
- 6 package updates (mostly minor version bumps)
- 0 API breaking changes identified
- Well-defined dependency graph (leaf node in solution)

### Expected Iterations

**Phase 1**: Discovery & Classification ✓ (Complete)
**Phase 2**: Foundation (3 iterations)
**Phase 3**: Detail Generation (2 iterations - single project, package details, final sections)

**Total Expected**: 6 iterations

---

## Migration Strategy

### Selected Strategy: All-At-Once

**Strategy Type:** All-At-Once (Single Atomic Operation)

Given the unique characteristics of this upgrade scenario, the All-At-Once strategy is not just preferred—it's the natural fit:

**Why All-At-Once:**

1. **Single Project Scope**
   - Only RotoGLBridge.UI requires framework upgrade
   - No other projects need modification
   - No incremental phasing needed

2. **Dependencies Already Upgraded**
   - All 7 direct and transitive dependencies already on .NET 10
   - No coordination across projects required
   - Zero risk of version conflicts

3. **Minimal Complexity**
   - 895 lines of code
   - 6 package updates (4 minor bumps, 2 to validate)
   - No API breaking changes identified
   - WPF application (stable framework)

4. **Fast Completion**
   - Single project file edit
   - One restore/build cycle
   - Immediate validation possible

5. **Low Risk Profile**
   - Leaf node in dependency graph (no consumers)
   - Clear rollback path (revert single project file)
   - All tests already on target framework

### Implementation Timeline

#### Phase 1: Atomic Upgrade

**Operations** (performed as single coordinated batch):
1. Update RotoGLBridge.UI.csproj TargetFramework: net8.0-windows → net10.0-windows
2. Update all package references in RotoGLBridge.UI.csproj
3. Restore dependencies (`dotnet restore`)
4. Build solution and fix any compilation errors (none expected)
5. Validate: Solution builds with 0 errors

**Deliverables:**
- ✅ RotoGLBridge.UI targeting net10.0-windows
- ✅ All packages updated to compatible versions
- ✅ Solution builds successfully
- ✅ No compiler warnings related to framework/packages

**Estimated Complexity:** Low

#### Phase 2: Test Validation

**Operations:**
1. Execute RotoGLBridge.Tests test project (xUnit tests)
2. Manual validation of RotoGLBridge.UI application startup
3. Functional smoke testing of UI features
4. Validation of plugin loading (SharpDX, Speech, UsbWatcher)

**Deliverables:**
- ✅ All automated tests pass
- ✅ UI application launches successfully
- ✅ No runtime exceptions during basic operations
- ✅ Plugins load and function correctly

**Estimated Complexity:** Low

### Dependency-Based Ordering

**Not Applicable** - This upgrade involves a single project that is a leaf node (no dependents). All of its dependencies are already on the target framework.

**Execution Order:**
1. RotoGLBridge.UI (only project requiring changes)

### Parallel vs Sequential Execution

**Not Applicable** - Single project upgrade requires no parallelization decisions.

### Rollback Strategy

If issues are encountered, rollback is straightforward:

**Rollback Steps:**
1. Revert `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj` to previous version
2. Run `dotnet restore`
3. Rebuild solution

**Alternative:** Git branch allows complete rollback via:
```bash
git checkout net10  # Return to source branch
```

### Risk Mitigation During Execution

**Build Phase:**
- If MaterialDesignThemes or ScottPlot.WPF cause build errors, validate compatibility individually
- Check package release notes for breaking changes
- Consider keeping current versions if compatible (don't force downgrade)

**Test Phase:**
- Run automated tests first (fast feedback)
- Then proceed to manual UI validation
- Document any unexpected behavior

**Dependencies:**
- Since all dependencies already on .NET 10, no integration issues expected
- Any issues isolated to RotoGLBridge.UI project only

---

## Detailed Dependency Analysis

### Solution Structure

The RotoGLBridge solution consists of 12 projects organized in a clear hierarchical structure:

**Application Layer:**
- `RotoGLBridge.UI` (WPF Application) - **Requires Upgrade**
- `RotoGLBridge.ConsoleApp` (Console Application) - Already on net10.0-windows ✅

**Core Library:**
- `RotoGLBridge` (Class Library) - Already on net10.0-windows ✅

**Plugin System:**
- `Sharpie.Plugins.SharpDX` - Already on net10.0-windows ✅
- `Sharpie.Plugins.Speech` - Already on net10.0-windows ✅
- `Sharpie.Plugins.UsbWatcher` - Already on net10.0-windows ✅

**Sharpie Engine:**
- `Sharpie.Engine` - Already on net10.0-windows ✅
- `Sharpie.Engine.Contracts` - Already on net10.0-windows ✅

**Helper Libraries:**
- `Sharpie.Helpers.Core` - Already on net10.0-windows ✅
- `Sharpie.Helpers.Telemetry` - Already on net10.0-windows ✅

**USB Library:**
- `rotoUSB` - Already on net10.0 ✅

**Testing:**
- `RotoGLBridge.Tests` - Already on net10.0-windows ✅

### Dependency Graph for RotoGLBridge.UI

```
RotoGLBridge.UI (net8.0-windows) ← TO UPGRADE
    └─→ RotoGLBridge (net10.0-windows) ✅
            ├─→ Sharpie.Helpers.Core (net10.0-windows) ✅
            ├─→ rotoUSB (net10.0) ✅
            ├─→ Sharpie.Helpers.Telemetry (net10.0-windows) ✅
            │       └─→ Sharpie.Engine.Contracts (net10.0-windows) ✅
            ├─→ Sharpie.Plugins.Speech (net10.0-windows) ✅
            │       └─→ Sharpie.Engine.Contracts (net10.0-windows) ✅
            ├─→ Sharpie.Plugins.UsbWatcher (net10.0-windows) ✅
            │       └─→ Sharpie.Engine.Contracts (net10.0-windows) ✅
            ├─→ Sharpie.Plugins.SharpDX (net10.0-windows) ✅
            │       └─→ Sharpie.Engine.Contracts (net10.0-windows) ✅
            └─→ Sharpie.Engine (net10.0-windows) ✅
                    ├─→ Sharpie.Helpers.Core (net10.0-windows) ✅
                    └─→ Sharpie.Engine.Contracts (net10.0-windows) ✅
```

### Key Observations

**✅ Ideal Upgrade Scenario:**
1. **All dependencies already upgraded**: Every project that RotoGLBridge.UI depends on is already on .NET 10.0-windows
2. **No circular dependencies**: Clean hierarchical structure
3. **Leaf node upgrade**: RotoGLBridge.UI is a consumer (no other projects depend on it)
4. **No transitive impacts**: Upgrading UI won't affect any other project

**Migration Order:**
Since all dependencies are already on the target framework, there is no ordering constraint. This is a single-project atomic upgrade.

**Phase Definition:**
- **Phase 0**: Not needed (no SDK/global.json prerequisites identified)
- **Phase 1**: Atomic upgrade of RotoGLBridge.UI
  - Update TargetFramework: net8.0-windows → net10.0-windows
  - Update package references (6 packages)
  - Build and verify
  - Fix any compilation errors (none expected)
- **Phase 2**: Test validation
  - Run RotoGLBridge.Tests (already on net10.0-windows)
  - Manual verification of UI application

### Risk Assessment from Dependencies

**Low Risk Factors:**
- ✅ No dependency chain upgrades required
- ✅ No version conflicts possible (dependencies already compatible)
- ✅ Simple, well-understood dependency structure
- ✅ All project references are compatible

**Potential Risk:**
- ⚠️ Third-party UI packages (MaterialDesignThemes, ScottPlot.WPF) flagged as incompatible
  - However, they're currently working with .NET 10 dependencies
  - Likely false positives from analysis tool
  - Will validate during build phase

---

## Project-by-Project Plans

### Project: src\RotoGLBridge.UI\RotoGLBridge.UI.csproj

**Project Type:** WPF Application  
**Complexity:** 🟢 Low  
**Risk Level:** 🟢 Low  

#### Current State

- **Target Framework:** net8.0-windows
- **SDK Style:** True (modern project format)
- **Project Kind:** Wpf
- **Lines of Code:** 895
- **Number of Files:** 16
- **Dependencies:** 1 project reference (RotoGLBridge.csproj - already on net10.0-windows)
- **Package Count:** 8 packages + 3 centrally managed

**Current Packages:**
- CommunityToolkit.Mvvm 8.4.0
- HelixToolkit.Core.Wpf 2.27.3
- HelixToolkit.SharpDX.Core 2.27.3
- MaterialDesignThemes 5.3.0
- Microsoft.Extensions.DependencyInjection 10.0.3
- Microsoft.Extensions.Logging 10.0.3
- NLog.Web.AspNetCore 6.1.2
- ScottPlot.WPF 5.1.57

**Centrally Managed (PackageReference Update):**
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.3
- Microsoft.Extensions.Logging.Abstractions 10.0.3
- MinVer 7.0.0

#### Target State

- **Target Framework:** net10.0-windows
- **All packages updated to compatible versions** (see Package Update Reference section)

#### Migration Steps

**1. Prerequisites**
- ✅ RotoGLBridge.csproj already on net10.0-windows (no prerequisite work needed)
- ✅ All transitive dependencies on compatible frameworks

**2. Framework Update**

Update `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj`:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
```

**3. Package Updates**

Update package references in `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj`:

| Package | Current | Target | Update Type |
|---------|---------|--------|-------------|
| Microsoft.Extensions.DependencyInjection | 10.0.3 | 10.0.4 | Minor bump |
| Microsoft.Extensions.Logging | 10.0.3 | 10.0.4 | Minor bump |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.3 | 10.0.4 | Minor bump (central) |
| Microsoft.Extensions.Logging.Abstractions | 10.0.3 | 10.0.4 | Minor bump (central) |
| MaterialDesignThemes | 5.3.0 | Validate compatibility first ⚠️ | See notes |
| ScottPlot.WPF | 5.1.57 | Validate compatibility first ⚠️ | See notes |

**Keep Unchanged:**
- CommunityToolkit.Mvvm 8.4.0 ✅ Compatible
- HelixToolkit.Core.Wpf 2.27.3 ✅ Compatible
- HelixToolkit.SharpDX.Core 2.27.3 ✅ Compatible
- NLog.Web.AspNetCore 6.1.2 ✅ Compatible
- MinVer 7.0.0 ✅ Compatible

**4. Expected Breaking Changes**

**Framework-Level:** None expected
- .NET 8 → .NET 10 is a standard LTS → LTS upgrade
- WPF APIs are stable across these versions
- No obsolete API usage identified in assessment

**Package-Level:** Low risk
- Microsoft.Extensions.* packages: Patch version bump (10.0.3 → 10.0.4) - no breaking changes expected
- UI framework packages: Currently flagged as incompatible but likely false positives

**Special Attention Required:**

⚠️ **MaterialDesignThemes 5.3.0**
- Assessment suggests version 5.2.1 (appears to be downgrade indicator)
- Current 5.3.0 is working with .NET 10 dependencies
- **Action:** Validate build first; if build succeeds, keep 5.3.0
- **If issues occur:** Check MaterialDesignThemes release notes for .NET 10 compatibility

⚠️ **ScottPlot.WPF 5.1.57**
- Assessment suggests version 4.1.73 (major version downgrade indicator)
- Current 5.1.57 is likely compatible (newer version)
- **Action:** Validate build first; if build succeeds, keep 5.1.57
- **If issues occur:** Check ScottPlot.WPF release notes; version 5.x is the modern branch

**5. Code Modifications**

**Expected:** None

Based on assessment:
- 0 API breaking changes identified
- 0 estimated LOC to modify
- Project uses stable WPF APIs
- Dependency injection patterns remain unchanged

**Potential Areas (if issues arise):**
- WPF XAML rendering (test visual appearance)
- MaterialDesignThemes styling (validate themes render correctly)
- ScottPlot chart rendering (validate plotting works)
- Dependency injection container setup (should be unchanged)

**6. Testing Strategy**

**Build Validation:**
1. `dotnet restore` on RotoGLBridge.UI.csproj
2. `dotnet build` on RotoGLBridge.UI.csproj
3. Verify 0 errors, 0 warnings
4. Confirm all dependencies resolved correctly

**Automated Tests:**
1. Run RotoGLBridge.Tests project (xUnit test suite)
2. Expected: All tests pass (tests already on net10.0-windows)

**Manual Testing:**
1. Launch RotoGLBridge.UI application
2. Verify application starts without exceptions
3. Validate UI renders correctly (MaterialDesign themes)
4. Test key features:
   - 3D model rendering (HelixToolkit)
   - Plotting functionality (ScottPlot)
   - USB device detection (plugins)
   - Speech functionality (plugins)
   - SharpDX input handling (plugins)
5. Check logging (NLog) works correctly

**Performance Testing:**
1. Verify application startup time similar to .NET 8 version
2. Validate 3D rendering performance
3. Check memory usage patterns

**7. Validation Checklist**

#### Build Phase
- [ ] Project file updated to net10.0-windows
- [ ] All package versions updated per plan
- [ ] `dotnet restore` completes successfully
- [ ] `dotnet build` completes with 0 errors
- [ ] No new compiler warnings introduced
- [ ] All dependencies resolved correctly

#### Test Phase
- [ ] All xUnit tests in RotoGLBridge.Tests pass
- [ ] Application launches without exceptions
- [ ] Main window renders correctly
- [ ] MaterialDesignThemes styles applied correctly
- [ ] HelixToolkit 3D models render (base.obj, chair.obj for rotovr and roto2)
- [ ] ScottPlot charts functional
- [ ] Plugin system loads all plugins (SharpDX, Speech, UsbWatcher)
- [ ] Logging functionality works (NLog)
- [ ] No runtime errors in normal operation
- [ ] No memory leaks detected

#### Integration Phase
- [ ] Works correctly with RotoGLBridge.csproj (net10.0-windows)
- [ ] All plugin interfaces function correctly
- [ ] USB device communication works
- [ ] Speech recognition/synthesis works (if applicable)
- [ ] XInput controller handling works (SharpDX plugin)

#### Final Validation
- [ ] Solution builds cleanly
- [ ] All tests pass
- [ ] Application performs as expected
- [ ] No regressions identified
- [ ] Ready for commit

---

## Package Update Reference

### Overview

The RotoGLBridge.UI project uses 11 NuGet packages (8 direct + 3 centrally managed). Of these:
- ✅ **5 packages** are compatible (no change needed)
- 🔄 **4 packages** require minor version updates (10.0.3 → 10.0.4)
- ⚠️ **2 packages** flagged as incompatible (likely false positives)

### Package Update Matrix

| Package | Current | Target | Reason | Projects | Action |
|---------|---------|--------|--------|----------|--------|
| **Microsoft.Extensions.DependencyInjection** | 10.0.3 | 10.0.4 | Patch update recommended | RotoGLBridge.UI | Update |
| **Microsoft.Extensions.Logging** | 10.0.3 | 10.0.4 | Patch update recommended | RotoGLBridge.UI | Update |
| **Microsoft.Extensions.DependencyInjection.Abstractions** | 10.0.3 | 10.0.4 | Patch update recommended | RotoGLBridge.UI (central) | Update |
| **Microsoft.Extensions.Logging.Abstractions** | 10.0.3 | 10.0.4 | Patch update recommended | RotoGLBridge.UI (central) | Update |
| **MaterialDesignThemes** | 5.3.0 | Keep 5.3.0 | Assessment suggests 5.2.1 (likely false positive) | RotoGLBridge.UI | Validate, keep current |
| **ScottPlot.WPF** | 5.1.57 | Keep 5.1.57 | Assessment suggests 4.1.73 (likely false positive) | RotoGLBridge.UI | Validate, keep current |
| **CommunityToolkit.Mvvm** | 8.4.0 | 8.4.0 | Compatible, no change | RotoGLBridge.UI | No change |
| **HelixToolkit.Core.Wpf** | 2.27.3 | 2.27.3 | Compatible, no change | RotoGLBridge.UI | No change |
| **HelixToolkit.SharpDX.Core** | 2.27.3 | 2.27.3 | Compatible, no change | RotoGLBridge.UI | No change |
| **NLog.Web.AspNetCore** | 6.1.2 | 6.1.2 | Compatible, no change | RotoGLBridge.UI | No change |
| **MinVer** | 7.0.0 | 7.0.0 | Compatible, no change | RotoGLBridge.UI (central) | No change |

### Detailed Package Analysis

#### Microsoft.Extensions.DependencyInjection (10.0.3 → 10.0.4)

**Update Type:** Patch  
**Risk Level:** 🟢 Very Low  
**Breaking Changes:** None expected (patch version)

**Affected Code Areas:**
- Service registration in App.xaml.cs or startup code
- DI container configuration

**Expected Impact:** None - patch releases maintain full API compatibility

**Validation:**
- Verify DI container initializes successfully
- Confirm all services resolve correctly
- No changes to registration patterns needed

---

#### Microsoft.Extensions.Logging (10.0.3 → 10.0.4)

**Update Type:** Patch  
**Risk Level:** 🟢 Very Low  
**Breaking Changes:** None expected (patch version)

**Affected Code Areas:**
- ILogger<T> injection points
- Logging configuration
- Log level settings

**Expected Impact:** None - patch releases maintain full API compatibility

**Validation:**
- Verify logging still writes to configured targets
- Confirm log levels respected
- NLog integration still functions

---

#### Microsoft.Extensions.DependencyInjection.Abstractions (10.0.3 → 10.0.4)

**Update Type:** Patch (Centrally Managed)  
**Risk Level:** 🟢 Very Low  
**Breaking Changes:** None expected

**Note:** This package is referenced via `<PackageReference Update>` in RotoGLBridge.UI.csproj, indicating central package management.

**Update Location:** `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj` - Update element near bottom of file

**Expected Impact:** None

---

#### Microsoft.Extensions.Logging.Abstractions (10.0.3 → 10.0.4)

**Update Type:** Patch (Centrally Managed)  
**Risk Level:** 🟢 Very Low  
**Breaking Changes:** None expected

**Note:** This package is referenced via `<PackageReference Update>` in RotoGLBridge.UI.csproj, indicating central package management.

**Update Location:** `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj` - Update element near bottom of file

**Expected Impact:** None

---

#### MaterialDesignThemes (5.3.0 - Keep Current)

**Current Version:** 5.3.0  
**Assessment Suggestion:** 5.2.1 (appears to be downgrade indicator)  
**Risk Level:** 🟡 Low  
**Recommendation:** **Keep 5.3.0 unless build fails**

**Analysis:**
- MaterialDesignThemes 5.3.0 is already working with projects on .NET 10 (RotoGLBridge dependencies)
- Version 5.3.0 is newer than suggested 5.2.1 - likely false positive from analysis tool
- MaterialDesignThemes 5.x branch supports .NET 6+ (includes .NET 10)

**Validation Strategy:**
1. First: Try build with 5.3.0 (current version)
2. If build succeeds → Keep 5.3.0
3. If build fails → Investigate error, check MaterialDesignThemes docs
4. Only downgrade if explicitly required by package documentation

**Affected Code Areas:**
- XAML styles and theme resources
- MaterialDesign control usage (buttons, cards, etc.)
- Theme switching logic (if any)

**Potential Issues (unlikely):**
- Theme rendering differences
- Control styling changes
- Resource dictionary loading

---

#### ScottPlot.WPF (5.1.57 - Keep Current)

**Current Version:** 5.1.57  
**Assessment Suggestion:** 4.1.73 (major version downgrade indicator)  
**Risk Level:** 🟡 Low  
**Recommendation:** **Keep 5.1.57 unless build fails**

**Analysis:**
- ScottPlot.WPF 5.1.57 is significantly newer than suggested 4.1.73
- Version 5.x is the modern branch with better .NET support
- Downgrading from 5.x to 4.x would be counterproductive
- Likely false positive from analysis tool

**Validation Strategy:**
1. First: Try build with 5.1.57 (current version)
2. If build succeeds → Keep 5.1.57
3. If build fails → Check ScottPlot.WPF release notes for .NET 10 compatibility
4. Version 5.x should support .NET 10 (verify on NuGet.org if needed)

**Affected Code Areas:**
- WPF plot controls in XAML
- Plot data binding and rendering
- Plot configuration and styling

**Potential Issues (unlikely):**
- Plot rendering differences
- Control initialization changes
- Data binding behavior

---

#### Packages Requiring No Changes

The following packages are confirmed compatible with .NET 10 and require no version updates:

**CommunityToolkit.Mvvm 8.4.0**
- MVVM toolkit for WPF applications
- Supports .NET 6+ (includes .NET 10)
- No changes needed

**HelixToolkit.Core.Wpf 2.27.3**
- 3D visualization for WPF
- Used for rendering .obj models (base.obj, chair.obj)
- Confirmed compatible

**HelixToolkit.SharpDX.Core 2.27.3**
- DirectX-based rendering support
- Works with HelixToolkit.Core.Wpf
- Confirmed compatible

**NLog.Web.AspNetCore 6.1.2**
- Logging framework
- Supports .NET 6+ (includes .NET 10)
- No changes needed

**MinVer 7.0.0**
- Build-time versioning tool
- Framework-agnostic (MSBuild target)
- No changes needed

### Package Update Execution Order

All package updates can be performed simultaneously as they have no interdependencies:

1. Update Microsoft.Extensions.DependencyInjection to 10.0.4
2. Update Microsoft.Extensions.Logging to 10.0.4
3. Update Microsoft.Extensions.DependencyInjection.Abstractions to 10.0.4 (Update element)
4. Update Microsoft.Extensions.Logging.Abstractions to 10.0.4 (Update element)

**MaterialDesignThemes and ScottPlot.WPF:** No changes unless build validation identifies specific issues.

### Central Package Management Note

RotoGLBridge.UI uses both direct `<PackageReference>` and `<PackageReference Update>` patterns:

**Direct References** (in main ItemGroup):
- CommunityToolkit.Mvvm
- HelixToolkit.Core.Wpf
- HelixToolkit.SharpDX.Core
- MaterialDesignThemes
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- NLog.Web.AspNetCore
- ScottPlot.WPF

**Central Management Updates** (in separate ItemGroup):
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Logging.Abstractions
- MinVer

Ensure updates are applied to the correct ItemGroup when editing the project file.

---

## Breaking Changes Catalog

### Framework Breaking Changes (.NET 8 → .NET 10)

#### Overview

.NET 10 is an LTS (Long Term Support) release that succeeds .NET 8 (also LTS). The upgrade path from .NET 8 to .NET 10 is generally smooth for WPF applications, with most breaking changes affecting niche scenarios.

**Assessment Finding:** 0 API breaking changes identified in RotoGLBridge.UI project.

#### Known .NET 10 Breaking Changes (General)

While no breaking changes were detected in this specific codebase, the following are known breaking changes in .NET 10 that could affect WPF applications:

**1. Serialization Changes**
- **Area:** BinaryFormatter removal enforcement
- **Impact:** Very Low (WPF typically doesn't use BinaryFormatter)
- **Mitigation:** RotoGLBridge.UI does not appear to use serialization

**2. Runtime Performance Optimizations**
- **Area:** JIT compiler improvements
- **Impact:** Very Low (performance enhancements, not breaking)
- **Mitigation:** Monitor application performance post-upgrade

**3. API Obsoletion**
- **Area:** Continued deprecation of legacy APIs
- **Impact:** Very Low (compile-time warnings if used)
- **Mitigation:** Address any new compiler warnings

**RotoGLBridge.UI Assessment:** None of the above affect the current codebase based on static analysis.

### Package Breaking Changes

#### Microsoft.Extensions.DependencyInjection 10.0.3 → 10.0.4

**Change Type:** Patch Release  
**Breaking Changes:** ✅ None

Patch releases (x.y.Z) maintain full backward compatibility. No code changes required.

**Areas Using This Package:**
- Dependency injection container setup
- Service registration (ViewModels, services)

**Validation:** Verify DI container initializes and resolves services correctly.

---

#### Microsoft.Extensions.Logging 10.0.3 → 10.0.4

**Change Type:** Patch Release  
**Breaking Changes:** ✅ None

Patch releases maintain full backward compatibility. No code changes required.

**Areas Using This Package:**
- ILogger<T> injection
- Logging configuration
- Integration with NLog

**Validation:** Verify logging output still writes to expected targets.

---

#### Microsoft.Extensions.*.Abstractions 10.0.3 → 10.0.4

**Change Type:** Patch Release  
**Breaking Changes:** ✅ None

Abstractions packages define interfaces and base types. Patch updates are fully compatible.

**Validation:** No specific validation needed beyond compilation.

---

#### MaterialDesignThemes 5.3.0 (No Change Planned)

**Current Version:** 5.3.0  
**Assessment Flag:** Incompatible (likely false positive)  
**Breaking Changes:** N/A (no change planned)

**If Downgrade to 5.2.1 Were Required:**
- Review release notes between 5.2.1 and 5.3.0
- Potential for theme/styling regressions
- Control behavior changes possible

**Validation Strategy:**
- First build with 5.3.0
- If successful, no changes needed
- If issues occur, investigate MaterialDesignThemes 5.3.0 release notes

**Areas to Test If Issues Occur:**
- Theme application (light/dark themes)
- MaterialDesign control rendering (Cards, Buttons, etc.)
- Resource dictionary loading
- Color scheme application

---

#### ScottPlot.WPF 5.1.57 (No Change Planned)

**Current Version:** 5.1.57  
**Assessment Flag:** Incompatible (likely false positive)  
**Breaking Changes:** N/A (no change planned)

**If Downgrade to 4.1.73 Were Required:**
- Major version downgrade (5.x → 4.x) would be significant
- API differences between major versions
- Not recommended without strong justification

**Validation Strategy:**
- First build with 5.1.57
- If successful, no changes needed
- ScottPlot 5.x is the modern branch with better API

**Areas to Test If Issues Occur:**
- Plot control instantiation in XAML
- Data binding to plots
- Plot rendering and interaction
- Plot styling and customization

---

### WPF-Specific Considerations (.NET 8 → .NET 10)

#### 1. Rendering Changes

**Area:** WPF rendering pipeline  
**Impact:** Minimal  
**Description:** .NET 10 includes performance improvements to WPF rendering, but no breaking changes to rendering APIs.

**Validation:**
- Visual inspection of UI after upgrade
- Check for rendering artifacts
- Verify 3D models render correctly (HelixToolkit)

---

#### 2. Input Handling

**Area:** Keyboard, mouse, and touch input  
**Impact:** None expected  
**Description:** WPF input handling remains stable across .NET 8 → .NET 10.

**Validation:**
- Test keyboard shortcuts
- Verify mouse interactions
- Check XInput controller handling (SharpDX plugin)

---

#### 3. Data Binding

**Area:** XAML data binding engine  
**Impact:** None expected  
**Description:** Data binding APIs unchanged.

**Validation:**
- Verify ViewModel to View bindings work
- Check two-way binding scenarios
- Validate command bindings (MVVM pattern with CommunityToolkit.Mvvm)

---

### Code Patterns Requiring Attention

Based on the project structure and dependencies, the following code patterns should be validated post-upgrade:

#### 1. Dependency Injection Setup

**Location:** Likely in `App.xaml.cs` or startup code  
**Risk:** 🟢 Very Low  
**Reason:** Patch update to Microsoft.Extensions.DependencyInjection

**Validation Steps:**
1. Application starts without DI exceptions
2. All ViewModels resolve correctly
3. Singleton/Scoped/Transient lifetimes work as expected

**Expected Outcome:** No changes needed

---

#### 2. MVVM Pattern with CommunityToolkit

**Location:** ViewModels throughout project  
**Risk:** ✅ None  
**Reason:** CommunityToolkit.Mvvm 8.4.0 fully compatible

**Validation Steps:**
1. ObservableObject base class works
2. RelayCommand and AsyncRelayCommand function
3. Property change notifications fire correctly

**Expected Outcome:** No changes needed

---

#### 3. 3D Model Rendering (HelixToolkit)

**Location:** XAML views with Helix3D controls  
**Risk:** 🟢 Very Low  
**Reason:** HelixToolkit packages confirmed compatible

**Validation Steps:**
1. 3D viewport initializes
2. .obj models load (base.obj, chair.obj for rotovr and roto2)
3. Camera navigation works
4. DirectX rendering (SharpDX.Core) functions

**Expected Outcome:** No changes needed

---

#### 4. Charting (ScottPlot.WPF)

**Location:** XAML views with ScottPlot controls  
**Risk:** 🟡 Low (flagged by assessment, likely false positive)

**Validation Steps:**
1. Plot controls initialize in XAML
2. Data displays correctly
3. Interactive features work (zoom, pan)
4. Plot styling applies correctly

**Expected Outcome:** Should work with 5.1.57; validate during build phase

---

#### 5. Logging with NLog

**Location:** Throughout application (ILogger<T> injection)  
**Risk:** 🟢 Very Low

**Validation Steps:**
1. NLog configuration loads
2. Logs write to expected targets (file, console, etc.)
3. Log levels filter correctly
4. Structured logging data captured

**Expected Outcome:** No changes needed

---

#### 6. Plugin System Integration

**Location:** Integration with Sharpie plugins  
**Risk:** ✅ None  
**Reason:** All plugin projects already on .NET 10

**Validation Steps:**
1. Plugins load at runtime (SharpDX, Speech, UsbWatcher)
2. Plugin interfaces resolve correctly
3. Plugin functionality works (USB detection, speech, XInput)

**Expected Outcome:** No changes needed

---

### Expected Code Changes Summary

**Total Expected Code Changes:** ✅ **ZERO**

**Rationale:**
- Assessment identified 0 API breaking changes
- All package updates are patch versions (fully compatible)
- WPF APIs stable across .NET 8 → .NET 10
- All dependencies already compatible with .NET 10

**If Issues Arise:**

Only if build or runtime errors occur should code changes be considered. Potential areas (in order of likelihood):

1. **MaterialDesignThemes** - If theme application fails, check resource dictionary loading
2. **ScottPlot.WPF** - If plot controls fail to initialize, check XAML control syntax
3. **Platform APIs** - Unlikely, but check for any Windows API P/Invoke calls if errors occur

**Recommended Approach:**
1. Make no preemptive code changes
2. Update project file and packages only
3. Build and test
4. Address any issues discovered during testing
5. Document any unexpected changes required

---

## Risk Management

### Risk Assessment Summary

**Overall Risk Level:** 🟢 **Low**

This upgrade presents minimal risk due to:
- Single project scope (1 of 12 projects)
- All dependencies already on target framework
- No API breaking changes identified
- Small codebase (895 LOC)
- Clear rollback path

### Risk Matrix

| Risk Category | Risk Level | Probability | Impact | Mitigation |
|---------------|------------|-------------|---------|------------|
| **Build Failure** | 🟢 Low | Low | Medium | All dependencies compatible; rollback via Git |
| **Package Incompatibility** | 🟡 Medium | Medium | Low | Two packages flagged; validate before commit |
| **Runtime Errors** | 🟢 Low | Low | Medium | All tests on .NET 10; gradual testing approach |
| **UI Rendering Issues** | 🟡 Medium | Low | Medium | WPF stable; validate themes and 3D rendering |
| **Plugin Integration** | 🟢 Low | Very Low | Low | Plugins already on .NET 10; no changes needed |
| **Performance Regression** | 🟢 Low | Very Low | Low | .NET 10 performance improvements expected |
| **Data Loss** | ✅ None | N/A | N/A | No database or data migration involved |

### Detailed Risk Analysis

#### Risk 1: Package Compatibility Issues

**Risk Level:** 🟡 Medium  
**Probability:** Medium  
**Impact:** Low

**Description:**
- MaterialDesignThemes 5.3.0 flagged as incompatible (suggests 5.2.1)
- ScottPlot.WPF 5.1.57 flagged as incompatible (suggests 4.1.73)
- Both likely false positives from analysis tool

**Indicators:**
- Current versions working with .NET 10 dependencies
- Suggested versions are downgrades (unusual)
- Packages support .NET 6+ (includes .NET 10)

**Mitigation:**
1. **Validate First:** Build with current versions before changing
2. **Research:** Check NuGet.org and GitHub releases for .NET 10 compatibility
3. **Incremental:** If issues occur, address one package at a time
4. **Fallback:** Only downgrade if explicitly required by package documentation

**Contingency Plan:**
- If MaterialDesignThemes fails: Check 5.2.x changelog, consider 5.2.1 or latest 5.x
- If ScottPlot.WPF fails: Verify 5.1.x supports .NET 10 (should); do NOT downgrade to 4.x without investigation

---

#### Risk 2: UI Rendering or Visual Regressions

**Risk Level:** 🟡 Medium  
**Probability:** Low  
**Impact:** Medium

**Description:**
- MaterialDesignThemes controls visual appearance
- HelixToolkit 3D rendering
- ScottPlot chart rendering

**Indicators:**
- WPF rendering engine stable across .NET 8 → .NET 10
- No rendering API changes in .NET 10
- UI packages confirmed compatible or likely compatible

**Mitigation:**
1. **Visual Testing:** Thoroughly test UI after upgrade
2. **3D Validation:** Load all .obj models (base.obj, chair.obj for rotovr/roto2)
3. **Chart Validation:** Display various plot types
4. **Theme Testing:** Test light/dark themes if applicable

**Contingency Plan:**
- If theme issues: Check MaterialDesignThemes resource dictionaries, verify theme switching logic
- If 3D issues: Verify HelixToolkit viewport initialization, check model file paths
- If chart issues: Validate ScottPlot control bindings, check data source connections

---

#### Risk 3: Plugin System Integration

**Risk Level:** 🟢 Low  
**Probability:** Very Low  
**Impact:** Low

**Description:**
- RotoGLBridge.UI loads plugins (SharpDX, Speech, UsbWatcher)
- Plugin discovery and loading mechanism

**Indicators:**
- All plugin projects already on .NET 10-windows
- Plugin interfaces (Sharpie.Engine.Contracts) on .NET 10
- No framework mismatch possible

**Mitigation:**
1. **Test Plugin Loading:** Verify all plugins discovered and loaded
2. **Test Plugin Functionality:** USB detection, speech, XInput controller
3. **Error Handling:** Ensure plugin load failures handled gracefully

**Contingency Plan:**
- If plugin load fails: Check assembly loading paths, verify no framework mismatch errors
- Review plugin discovery mechanism for framework-specific code

---

#### Risk 4: Dependency Injection Container Issues

**Risk Level:** 🟢 Low  
**Probability:** Low  
**Impact:** Low

**Description:**
- Microsoft.Extensions.DependencyInjection 10.0.3 → 10.0.4
- Service registration and resolution

**Indicators:**
- Patch version update (no breaking changes)
- DI patterns well-established and stable
- No changes to registration APIs

**Mitigation:**
1. **Test Application Startup:** Verify DI container initializes
2. **Test Service Resolution:** All ViewModels and services resolve
3. **Test Lifetimes:** Singleton, Scoped, Transient work correctly

**Contingency Plan:**
- If DI fails: Roll back to 10.0.3, investigate specific error
- Check for any conditional compilation targeting specific versions

---

#### Risk 5: Build or Compilation Failures

**Risk Level:** 🟢 Low  
**Probability:** Low  
**Impact:** Medium

**Description:**
- Project file changes
- Package restore
- Compilation

**Indicators:**
- Simple project file changes (TargetFramework, 4 version numbers)
- SDK-style project (modern, well-supported)
- All dependencies compatible

**Mitigation:**
1. **Incremental Build:** Build after framework change, then after package updates
2. **Clean Build:** Use `dotnet clean` before final build
3. **Restore Verification:** Ensure `dotnet restore` completes without errors

**Contingency Plan:**
- If build fails: Review error messages, check for missing dependencies
- If package restore fails: Clear NuGet cache, retry
- If compilation fails: Review for obsolete API usage (unlikely per assessment)

---

### Security Considerations

**No Security Vulnerabilities Identified**

The assessment found:
- ✅ No packages with security vulnerabilities
- ✅ No CVEs associated with current packages
- ✅ All packages at or near current versions

**Post-Upgrade Security:**
- .NET 10 LTS includes security improvements over .NET 8
- Updating Microsoft.Extensions.* to 10.0.4 includes latest patches
- No security-related code changes required

---

### Rollback Plan

#### Immediate Rollback (During Upgrade)

If critical issues are encountered during upgrade:

**Option 1: File-Level Rollback**
```bash
cd D:\source\mine\Roto
git checkout HEAD -- src\RotoGLBridge.UI\RotoGLBridge.UI.csproj
dotnet restore
dotnet build
```

**Option 2: Branch Rollback**
```bash
cd D:\source\mine\Roto
git checkout net10
```

#### Post-Commit Rollback

If issues discovered after commit:

**Option 1: Revert Commit**
```bash
git revert <commit-hash>
git push
```

**Option 2: Branch Restore**
```bash
git reset --hard <pre-upgrade-commit>
```

#### Rollback Validation
1. Verify application builds on .NET 8
2. Run tests to confirm functionality
3. Launch application to verify operation

---

### Contingency Plans by Scenario

#### Scenario A: MaterialDesignThemes Build Failure

**Symptoms:** Build errors related to MaterialDesignThemes types or resources

**Actions:**
1. Check MaterialDesignThemes 5.3.0 release notes on GitHub
2. Verify .NET 10 compatibility on NuGet.org
3. If incompatible, try version 5.2.1 (assessment suggestion)
4. If still failing, search GitHub issues for .NET 10 compatibility
5. Consider alternative: Downgrade to last known compatible version

**Workaround:** Temporarily remove MaterialDesignThemes, use standard WPF controls

---

#### Scenario B: ScottPlot.WPF Build Failure

**Symptoms:** Build errors related to ScottPlot controls or namespaces

**Actions:**
1. Verify ScottPlot.WPF 5.1.57 .NET 10 compatibility on NuGet.org
2. Check ScottPlot documentation for framework support
3. Review GitHub issues for .NET 10 compatibility
4. **Do NOT downgrade to 4.x** without investigation (major version change)
5. If needed, look for newer 5.x release

**Workaround:** Temporarily comment out ScottPlot controls, validate rest of application

---

#### Scenario C: Runtime Plugin Loading Failure

**Symptoms:** Plugins fail to load or initialize at runtime

**Actions:**
1. Check for detailed error messages in logs
2. Verify plugin assembly paths correct
3. Check for framework mismatch errors (should not occur)
4. Validate plugin dependencies resolved

**Workaround:** Disable plugin loading, run application without plugins to isolate issue

---

#### Scenario D: Unexpected Test Failures

**Symptoms:** Previously passing tests now fail

**Actions:**
1. Review test failure messages for framework-related issues
2. Check for test framework compatibility (xUnit confirmed compatible)
3. Isolate failing tests to specific areas
4. Validate test data and mocks still valid

**Workaround:** Skip failing tests temporarily, investigate separately

---

### Risk Acceptance

**Acceptable Risks:**
- Minor UI visual differences (if within acceptable tolerances)
- Non-critical warnings during build (if no functional impact)
- Performance variations within 5% (expected from runtime changes)

**Unacceptable Risks:**
- Application crash or instability
- Data corruption or loss (N/A for this project)
- Critical features non-functional
- Security vulnerabilities introduced

---

## Testing & Validation Strategy

### Testing Philosophy

A multi-layered testing approach ensures the upgrade is successful at every level:

1. **Build Validation** - Verify compilation succeeds
2. **Automated Testing** - Run existing test suite
3. **Manual Functional Testing** - Validate key application features
4. **Integration Testing** - Verify plugin system and dependencies
5. **Visual Validation** - Confirm UI renders correctly
6. **Performance Validation** - Check for regressions

### Phase 1: Build Validation

**Objective:** Confirm project builds successfully after framework and package updates

**Steps:**

1. **Clean Build Environment**
   ```bash
   cd D:\source\mine\Roto
   dotnet clean
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore src\RotoGLBridge.UI\RotoGLBridge.UI.csproj
   ```
   **Expected:** All packages restore successfully, no errors

3. **Build Project**
   ```bash
   dotnet build src\RotoGLBridge.UI\RotoGLBridge.UI.csproj --configuration Release
   ```
   **Expected:** 0 errors, 0 warnings (or warnings unchanged from .NET 8)

4. **Build Entire Solution**
   ```bash
   dotnet build RotoGLBridge.slnx --configuration Release
   ```
   **Expected:** All 12 projects build successfully

**Success Criteria:**
- ✅ No build errors
- ✅ No new warnings introduced
- ✅ All package dependencies resolved
- ✅ Output assemblies generated

**If Failures Occur:**
- Review build errors for package incompatibilities
- Check for missing dependencies
- Validate project file syntax
- See Risk Management section for contingency plans

---

### Phase 2: Automated Testing

**Objective:** Validate existing functionality through automated tests

#### Test Project: RotoGLBridge.Tests

**Framework:** xUnit 2.9.3  
**Coverage:** Unit tests for core functionality  
**Status:** Already on net10.0-windows ✅

**Execution:**

```bash
cd D:\source\mine\Roto
dotnet test src\RotoGLBridge.Tests\RotoGLBridge.Tests.csproj --configuration Release --logger "console;verbosity=detailed"
```

**Expected Results:**
- All tests pass (same pass rate as .NET 8)
- No new test failures introduced
- Test execution time similar to .NET 8

**Success Criteria:**
- ✅ 100% of previously passing tests still pass
- ✅ No new test failures
- ✅ No test infrastructure errors
- ✅ Test output logs show expected behavior

**If Failures Occur:**
- Review failed test details
- Determine if failures are upgrade-related or pre-existing
- Investigate framework-specific behavior changes
- Check test data and mocks for compatibility

---

### Phase 3: Manual Functional Testing

**Objective:** Validate key application features through hands-on testing

#### 3.1: Application Startup

**Test Steps:**
1. Launch RotoGLBridge.UI.exe
2. Observe startup sequence
3. Check for any initialization errors

**Expected Behavior:**
- Application starts without exceptions
- Main window displays correctly
- Splash screen (if any) shows properly
- Dependency injection container initializes

**Validation Checklist:**
- [ ] Application launches
- [ ] No startup exceptions in logs
- [ ] Main window renders
- [ ] UI responsive during startup

---

#### 3.2: User Interface Rendering

**Test Steps:**
1. Inspect main window layout
2. Check MaterialDesignThemes controls render correctly
3. Verify colors, fonts, and styling

**Expected Behavior:**
- UI layout matches .NET 8 version
- MaterialDesign themes applied correctly
- Controls (buttons, text boxes, etc.) render properly
- Icons and images display

**Validation Checklist:**
- [ ] Layout correct
- [ ] Theme applied (light/dark as configured)
- [ ] All controls visible
- [ ] No rendering artifacts
- [ ] Text readable and properly formatted

---

#### 3.3: 3D Model Rendering (HelixToolkit)

**Test Steps:**
1. Navigate to 3D view (if applicable)
2. Load 3D models:
   - `assets\rotovr\base.obj` and `base.mtl`
   - `assets\rotovr\chair.obj` and `chair.mtl`
   - `assets\roto2\base.obj` and `base.mtl`
   - `assets\roto2\chair.obj` and `chair.mtl`
3. Test camera navigation (rotate, zoom, pan)

**Expected Behavior:**
- 3D viewport initializes
- .obj models load and display correctly
- Materials (.mtl) apply correctly
- Camera controls responsive
- No DirectX errors

**Validation Checklist:**
- [ ] 3D viewport displays
- [ ] Models load without errors
- [ ] Materials/textures render correctly
- [ ] Camera navigation works (mouse drag, zoom)
- [ ] No performance issues or lag
- [ ] No DirectX/SharpDX errors in logs

---

#### 3.4: Charting (ScottPlot)

**Test Steps:**
1. Navigate to views with ScottPlot charts
2. Display various plot types (line, scatter, etc.)
3. Test interactive features (zoom, pan)

**Expected Behavior:**
- Plot controls initialize
- Data displays correctly
- Interactive features work
- Plot styling matches expectations

**Validation Checklist:**
- [ ] Plots render correctly
- [ ] Data displays accurately
- [ ] Zoom functionality works
- [ ] Pan functionality works
- [ ] Plot legends and axes display correctly
- [ ] No rendering errors

---

#### 3.5: Plugin System Integration

**Test Steps:**
1. Verify all plugins load at startup:
   - Sharpie.Plugins.SharpDX (XInput controller support)
   - Sharpie.Plugins.Speech (speech recognition/synthesis)
   - Sharpie.Plugins.UsbWatcher (USB device detection)
2. Test plugin functionality:
   - Connect XInput controller (if available)
   - Test speech commands (if applicable)
   - Plug/unplug USB device

**Expected Behavior:**
- All plugins discovered and loaded
- Plugin initialization succeeds
- Plugin functionality works as expected
- No plugin load errors in logs

**Validation Checklist:**
- [ ] SharpDX plugin loads
- [ ] Speech plugin loads
- [ ] UsbWatcher plugin loads
- [ ] XInput controller detected (if connected)
- [ ] Speech functionality works (if applicable)
- [ ] USB device events detected
- [ ] No plugin errors in logs

---

#### 3.6: Logging (NLog)

**Test Steps:**
1. Perform various application operations
2. Check log files for expected entries
3. Verify log levels work correctly

**Expected Behavior:**
- Logs write to configured targets
- Log format correct
- Log levels respected (Debug, Info, Warn, Error)

**Validation Checklist:**
- [ ] Log files created
- [ ] Log entries written
- [ ] Log format correct
- [ ] No logging errors
- [ ] Sensitive data not logged

---

#### 3.7: Dependency Injection

**Test Steps:**
1. Navigate through application views
2. Verify ViewModels instantiate correctly
3. Check services resolve properly

**Expected Behavior:**
- ViewModels resolve from DI container
- Services inject correctly
- No resolution errors

**Validation Checklist:**
- [ ] All views display (ViewModels resolve)
- [ ] No DI resolution errors
- [ ] Service lifetimes work (singleton, scoped, transient)

---

### Phase 4: Integration Testing

**Objective:** Validate integration between RotoGLBridge.UI and dependencies

#### 4.1: RotoGLBridge Core Integration

**Test Steps:**
1. Invoke functionality from RotoGLBridge.csproj
2. Verify communication with core library

**Expected Behavior:**
- Core library functions accessible
- Data flows correctly between UI and core
- No framework mismatch errors

**Validation Checklist:**
- [ ] Core functionality accessible from UI
- [ ] Data exchange works correctly
- [ ] No interop errors

---

#### 4.2: USB Communication (rotoUSB)

**Test Steps:**
1. Test USB device detection
2. Verify USB communication
3. Check device enumeration

**Expected Behavior:**
- USB devices detected correctly
- Communication protocols work
- No device errors

**Validation Checklist:**
- [ ] USB devices detected
- [ ] Communication successful
- [ ] No USB errors in logs

---

### Phase 5: Performance Validation

**Objective:** Ensure no performance regressions

#### Performance Metrics to Monitor

1. **Application Startup Time**
   - Measure time from launch to main window displayed
   - Compare with .NET 8 baseline
   - **Acceptable:** Within ±10% of baseline

2. **3D Rendering Performance**
   - Monitor frame rate during 3D model rendering
   - Test with complex models
   - **Acceptable:** FPS within ±5% of baseline

3. **Memory Usage**
   - Monitor memory consumption during typical usage
   - Check for memory leaks
   - **Acceptable:** Within ±10% of baseline

4. **UI Responsiveness**
   - Test UI interactions (button clicks, navigation)
   - Measure response times
   - **Acceptable:** No noticeable delays

**Performance Testing Steps:**
1. Launch application and note startup time
2. Perform typical operations for 15-30 minutes
3. Monitor memory usage via Task Manager or Performance Profiler
4. Test 3D rendering with complex models
5. Compare metrics with .NET 8 baseline

**Success Criteria:**
- ✅ Startup time within acceptable range
- ✅ No memory leaks detected
- ✅ Rendering performance maintained
- ✅ UI remains responsive

---

### Phase 6: Visual Validation

**Objective:** Confirm visual appearance matches expectations

**Test Steps:**
1. Take screenshots of key views
2. Compare with .NET 8 version screenshots (if available)
3. Check for visual differences

**Areas to Validate:**
- Main window layout
- MaterialDesign theme application
- Button styles and colors
- Text rendering and fonts
- Icon display
- 3D viewport appearance
- Chart rendering

**Success Criteria:**
- ✅ No unexpected visual changes
- ✅ Acceptable visual differences (if any)
- ✅ Theme consistency maintained

---

### Testing Timeline

**Estimated Testing Duration:**

| Phase | Duration | When |
|-------|----------|------|
| Build Validation | 5-10 minutes | Immediately after upgrade |
| Automated Testing | 5-10 minutes | After successful build |
| Manual Functional Testing | 30-60 minutes | After automated tests pass |
| Integration Testing | 15-30 minutes | After functional testing |
| Performance Validation | 30 minutes | After integration testing |
| Visual Validation | 15 minutes | Throughout manual testing |
| **Total** | **~2-3 hours** | **Single session** |

---

### Test Environment

**Requirements:**
- Windows 10/11 with latest updates
- .NET 10 SDK installed
- Visual Studio 2022 (if using IDE)
- Test hardware (USB devices, XInput controller) if available
- Network access (for NuGet restore if needed)

**Configuration:**
- Test in Release configuration (matches production)
- Test on clean build (dotnet clean before testing)
- Test with typical user permissions

---

### Regression Testing Checklist

Comprehensive checklist for final validation:

#### Build & Compilation
- [ ] Solution builds with 0 errors
- [ ] No new compiler warnings
- [ ] All projects build in correct order
- [ ] NuGet packages restore successfully

#### Automated Tests
- [ ] All xUnit tests pass
- [ ] Test coverage maintained
- [ ] No test infrastructure errors

#### Application Launch
- [ ] Application starts successfully
- [ ] No startup exceptions
- [ ] Main window displays correctly
- [ ] Splash screen works (if applicable)

#### User Interface
- [ ] Layout correct
- [ ] MaterialDesign theme applied
- [ ] All controls render properly
- [ ] Icons and images display
- [ ] Text readable

#### 3D Rendering
- [ ] 3D viewport initializes
- [ ] All .obj models load (rotovr and roto2)
- [ ] Materials apply correctly
- [ ] Camera navigation works
- [ ] No DirectX errors

#### Charting
- [ ] Plot controls render
- [ ] Data displays correctly
- [ ] Interactive features work

#### Plugins
- [ ] All plugins load
- [ ] SharpDX functionality works
- [ ] Speech functionality works
- [ ] UsbWatcher functionality works

#### Integration
- [ ] Core library integration works
- [ ] USB communication works
- [ ] Plugin interfaces function correctly

#### Logging
- [ ] Logs write to targets
- [ ] Log format correct
- [ ] No logging errors

#### Dependency Injection
- [ ] All ViewModels resolve
- [ ] Services inject correctly
- [ ] No DI errors

#### Performance
- [ ] Startup time acceptable
- [ ] Memory usage acceptable
- [ ] Rendering performance acceptable
- [ ] UI responsive

#### Visual Appearance
- [ ] No unexpected visual changes
- [ ] Theme consistency maintained
- [ ] Overall appearance acceptable

---

### Test Result Documentation

After testing, document results:

**Test Summary:**
- Total tests executed
- Tests passed
- Tests failed (with details)
- Tests skipped (with reasons)
- Performance metrics
- Visual differences noted

**Issues Found:**
- Description of each issue
- Severity (Critical, High, Medium, Low)
- Reproduction steps
- Proposed resolution

**Sign-off:**
- Tester name
- Date of testing
- Recommendation (Proceed / Investigate / Rollback)

---

## Complexity & Effort Assessment

### Overall Complexity Rating: ✅ **Simple**

This upgrade is classified as **Simple** based on objective metrics and established criteria.

### Complexity Classification Criteria

#### Scope Metrics
- **Projects Requiring Upgrade:** 1 of 12
- **Projects Already on Target:** 11 of 12 (91.7%)
- **Dependency Depth:** 1 level (UI → Core, all dependencies already upgraded)
- **Total Solution LOC:** 9,200
- **Affected Project LOC:** 895 (9.7% of solution)

**Assessment:** ✅ Minimal scope, isolated to single project

---

#### Dependency Complexity
- **Dependency Chain Length:** None (dependencies already upgraded)
- **Circular Dependencies:** None
- **Cross-Project Dependencies:** 1 (RotoGLBridge.csproj - already compatible)
- **External Package Dependencies:** 11 total, 6 requiring attention

**Assessment:** ✅ Simple, linear dependency structure

---

#### Change Complexity
- **Framework Version Jump:** .NET 8 → .NET 10 (LTS to LTS, 1 major version)
- **API Breaking Changes:** 0 identified
- **Package Updates Required:** 6 (4 patch updates, 2 to validate)
- **Code Changes Expected:** 0
- **Configuration Changes:** Minimal (project file only)

**Assessment:** ✅ Low change complexity, mostly administrative updates

---

#### Risk Indicators
- **Security Vulnerabilities:** 0
- **High-Risk Projects:** 0
- **Known Breaking Changes:** 0
- **Deprecation Warnings:** 0

**Assessment:** ✅ No high-risk factors identified

---

#### Technical Complexity
- **Project Type:** WPF Application (well-understood, stable)
- **SDK Style:** Modern (SDK-style project)
- **Build System:** MSBuild (standard)
- **Plugin Architecture:** Yes, but plugins already upgraded

**Assessment:** ✅ Standard project structure, no exotic technologies

---

### Complexity Rating: Simple

Based on the criteria above, this upgrade meets all characteristics of a **Simple** complexity rating:

| Criterion | Threshold for Simple | This Upgrade | ✓ |
|-----------|---------------------|--------------|---|
| Projects to Upgrade | ≤ 5 | 1 | ✅ |
| Dependency Depth | ≤ 2 levels | 0 levels (all deps ready) | ✅ |
| High-Risk Projects | 0 | 0 | ✅ |
| Security Vulnerabilities | 0 | 0 | ✅ |
| API Breaking Changes | 0 | 0 | ✅ |
| Affected LOC | < 1,000 | 895 | ✅ |

**Conclusion:** This upgrade qualifies as **Simple** complexity.

---

### Per-Project Complexity Assessment

#### RotoGLBridge.UI - 🟢 Low Complexity

| Factor | Rating | Justification |
|--------|--------|---------------|
| **Size** | Low | 895 LOC, 16 files |
| **Dependencies** | Low | 1 project ref + 11 packages (6 updates) |
| **Risk** | Low | No breaking changes, packages mostly compatible |
| **Technology** | Low | WPF (stable), standard patterns |
| **Testing** | Low | Test project exists, already on .NET 10 |

**Complexity Score:** 🟢 **Low** (1-2 on scale of 1-5)

**Rationale:**
- Single framework version change
- Minimal package updates (mostly patches)
- No code changes expected
- Well-defined testing approach
- Clear rollback path

---

### Effort Estimation (Relative)

**Important:** Time estimates are inherently unreliable and excluded. Complexity ratings use relative scale.

#### Effort by Phase

| Phase | Complexity | Relative Effort | Notes |
|-------|------------|-----------------|-------|
| **Assessment** | ✅ Complete | N/A | Already done |
| **Planning** | 🟢 Low | 1x baseline | Straightforward plan |
| **Execution: Framework Update** | 🟢 Low | 0.5x baseline | Single project file edit |
| **Execution: Package Updates** | 🟢 Low | 0.5x baseline | 4 version bumps, 2 validations |
| **Execution: Build & Fix** | 🟢 Low | 1x baseline | No fixes expected |
| **Testing: Automated** | 🟢 Low | 0.5x baseline | Tests already on .NET 10 |
| **Testing: Manual** | 🟡 Medium | 2x baseline | Thorough UI/3D/plugin testing |
| **Testing: Performance** | 🟢 Low | 1x baseline | Simple metrics |
| **Documentation** | 🟢 Low | 0.5x baseline | Minimal changes to document |

**Overall Relative Effort:** 🟢 **Low** (7x baseline units, where Medium ≈ 15-20x, Complex ≈ 40+x)

---

### Resource Requirements

#### Skills Required

**Primary Skills:**
- .NET project configuration (project files, package management)
- WPF application development
- NuGet package management
- Git version control

**Secondary Skills:**
- 3D rendering (HelixToolkit validation)
- Charting (ScottPlot validation)
- Plugin architecture understanding

**Skill Level:** Intermediate .NET developer with WPF experience

**Team Size:** 1 developer sufficient

---

#### Tools & Environment

**Required:**
- Visual Studio 2022 or JetBrains Rider (or VS Code + command line)
- .NET 10 SDK installed
- Git client
- Windows 10/11

**Optional:**
- Performance profiling tools (for validation)
- Test automation tools

---

### Execution Phases & Effort

#### Phase 1: Prerequisites (if needed)
**Complexity:** N/A  
**Effort:** None required (no SDK/global.json changes identified)

---

#### Phase 2: Atomic Upgrade
**Complexity:** 🟢 Low  
**Effort:** Low

**Activities:**
1. Update RotoGLBridge.UI.csproj TargetFramework
2. Update 4 package versions (Microsoft.Extensions.* packages)
3. Validate 2 packages (MaterialDesignThemes, ScottPlot.WPF)
4. Restore dependencies
5. Build solution
6. Fix any compilation errors (none expected)

**Relative Effort:** 2x baseline units

---

#### Phase 3: Testing & Validation
**Complexity:** 🟡 Medium  
**Effort:** Medium

**Activities:**
1. Run automated tests (xUnit suite)
2. Manual functional testing (UI, 3D, charts, plugins)
3. Integration testing (core library, USB, plugins)
4. Performance validation
5. Visual validation

**Relative Effort:** 4x baseline units (largest phase due to thorough testing)

---

#### Phase 4: Documentation & Commit
**Complexity:** 🟢 Low  
**Effort:** Low

**Activities:**
1. Document any findings
2. Update commit message
3. Commit changes
4. Update tracking documentation

**Relative Effort:** 1x baseline unit

---

### Comparison with Other Complexity Levels

To provide context, here's how this upgrade compares:

#### If This Were "Medium" Complexity:
- 5-15 projects requiring upgrade
- Some dependency chain upgrades needed
- 1-2 high-risk projects
- Some API breaking changes
- Code modifications required in multiple files
- **Effort:** 15-20x baseline units

#### If This Were "Complex" Complexity:
- 15+ projects requiring upgrade
- Deep dependency chains (3+ levels)
- Multiple high-risk projects
- Significant API breaking changes
- Extensive code modifications
- Circular dependencies to resolve
- **Effort:** 40+x baseline units

#### This Upgrade (Simple):
- 1 project requiring upgrade
- No dependency chain upgrades
- 0 high-risk projects
- 0 API breaking changes
- 0 code changes expected
- **Effort:** 7x baseline units

**Conclusion:** This upgrade is significantly simpler than typical upgrades.

---

### Confidence Level

**Confidence in Complexity Assessment:** 🟢 **High**

**Reasons for High Confidence:**
1. ✅ Assessment data comprehensive and clear
2. ✅ Objective metrics support "Simple" classification
3. ✅ No unknowns or gaps in assessment
4. ✅ All dependencies already upgraded (eliminates major uncertainty)
5. ✅ Package compatibility well-documented
6. ✅ Historical precedent (WPF .NET 8 → .NET 10 upgrades are routine)

**Uncertainty Factors (Low Impact):**
- ⚠️ MaterialDesignThemes and ScottPlot.WPF flagged (likely false positives)
- ⚠️ Manual testing effort depends on thoroughness desired

**Mitigation:**
- Validate flagged packages early in execution
- Allocate buffer time for manual testing

---

### Effort Distribution

Visual representation of effort across phases:

```
Assessment:     ████████████████████ (COMPLETE)
Planning:       ███████░░░░░░░░░░░░░ (14% - Low)
Execution:      ████████░░░░░░░░░░░░ (16% - Low)  
Testing:        ████████████████████████████░░░░ (56% - Medium, most thorough)
Documentation:  ███░░░░░░░░░░░░░░░░░ (6% - Low)
Contingency:    ████░░░░░░░░░░░░░░░░ (8% - Buffer)
```

**Key Insight:** Testing phase represents the bulk of effort due to thoroughness requirements, not complexity.

---

### Success Factors

**Factors Contributing to Low Complexity:**
1. ✅ All dependencies already upgraded (removes cascading effort)
2. ✅ Single project scope (no coordination needed)
3. ✅ No breaking changes (no code refactoring)
4. ✅ Modern SDK-style project (easy to modify)
5. ✅ Clear rollback path (reduces risk)
6. ✅ Test suite exists and is already compatible

**Factors to Monitor:**
- ⚠️ MaterialDesignThemes and ScottPlot.WPF validation results
- ⚠️ Thoroughness of manual testing (can extend timeline)

---

### Recommendations

**Based on Complexity Assessment:**

1. **Execution Approach:** All-At-Once (already selected) ✅ Correct choice
2. **Team Size:** 1 developer sufficient
3. **Execution Window:** Can be completed in single session
4. **Testing Rigor:** Comprehensive testing warranted (low risk of breaking, but verify thoroughly)
5. **Rollback Preparation:** Minimal (simple Git revert)
6. **Communication:** Minimal stakeholder coordination needed

**Anti-Recommendations:**
- ❌ Do NOT over-engineer the upgrade (it's simple)
- ❌ Do NOT split into multiple phases (unnecessary overhead)
- ❌ Do NOT allocate large team (one developer optimal)
- ❌ Do NOT add excessive process ceremony (lightweight process appropriate)

---

## Source Control Strategy

### Branch Structure

**Current Branch Setup:**
- **Source Branch:** `net10` (starting point, committed before upgrade)
- **Upgrade Branch:** `upgrade-to-NET10` (active branch for upgrade work)
- **Target Merge Branch:** `net10` (merge back after successful upgrade)

### Branching Strategy

This upgrade follows a **feature branch workflow**:

1. **Source Branch (`net10`)**: Stable baseline with committed changes
2. **Upgrade Branch (`upgrade-to-NET10`)**: Isolated workspace for upgrade
3. **Merge Back**: After successful validation, merge to `net10`

**Advantages:**
- ✅ Isolates upgrade work from main development
- ✅ Allows easy rollback (switch back to `net10` branch)
- ✅ Enables review before merging
- ✅ Maintains clean history

---

### Commit Strategy

#### All-At-Once Approach (Recommended)

**Single Commit for Entire Upgrade**

Given the simple nature of this upgrade (single project, minimal changes), a **single atomic commit** is recommended.

**Commit Structure:**
```
Upgrade RotoGLBridge.UI to .NET 10

- Update TargetFramework: net8.0-windows → net10.0-windows
- Update Microsoft.Extensions.DependencyInjection: 10.0.3 → 10.0.4
- Update Microsoft.Extensions.Logging: 10.0.3 → 10.0.4
- Update Microsoft.Extensions.DependencyInjection.Abstractions: 10.0.3 → 10.0.4
- Update Microsoft.Extensions.Logging.Abstractions: 10.0.3 → 10.0.4
- Validated MaterialDesignThemes 5.3.0 compatibility (no change)
- Validated ScottPlot.WPF 5.1.57 compatibility (no change)

Testing:
- All xUnit tests pass
- Application launches successfully
- UI rendering validated (MaterialDesign themes, 3D models, charts)
- Plugin system functional (SharpDX, Speech, UsbWatcher)
- Performance metrics acceptable

Related: #<issue-number> (if applicable)
```

**Rationale for Single Commit:**
- All changes interdependent (framework + packages must update together)
- Small changeset (project file edits only)
- No intermediate "working state" between updates
- Simplifies rollback (single commit to revert)
- Clear atomic unit of work

---

#### Alternative: Multi-Commit Approach (If Issues Arise)

If validation reveals unexpected issues requiring iterative fixes:

**Commit 1: Framework and Package Updates**
```
Upgrade RotoGLBridge.UI to .NET 10 - Framework and packages

- Update TargetFramework: net8.0-windows → net10.0-windows
- Update Microsoft.Extensions.* packages to 10.0.4
- Builds successfully
```

**Commit 2: Issue Fixes (if needed)**
```
Fix <specific issue> after .NET 10 upgrade

- [Description of fix]
- [Files modified]
- [Testing notes]
```

**Commit 3: Final Validation**
```
Complete .NET 10 upgrade - validation successful

- All tests pass
- Manual validation complete
- Performance metrics acceptable
```

---

### Commit Timing & Checkpoints

#### Checkpoint 1: After Successful Build

**When:** After framework and package updates, once solution builds with 0 errors

**Decision Point:**
- ✅ If build succeeds with no issues → Proceed to testing before commit
- ⚠️ If build fails → Fix issues, then commit once stable

**Recommended:** Do NOT commit until after testing (single commit approach)

---

#### Checkpoint 2: After Automated Tests Pass

**When:** After all xUnit tests in RotoGLBridge.Tests pass

**Decision Point:**
- ✅ If tests pass → Proceed to manual testing
- ⚠️ If tests fail → Investigate, fix, re-test

**Recommended:** Continue to manual testing before committing

---

#### Checkpoint 3: After Manual Validation

**When:** After comprehensive manual and integration testing complete

**Decision Point:**
- ✅ If all validation passes → **COMMIT** (single commit with full context)
- ⚠️ If validation finds issues → Fix, re-test, then commit

**Recommended:** This is the commit point for single-commit approach

---

### Commit Message Guidelines

**Format:**

```
<type>: <short summary> (max 72 chars)

<detailed description>

<testing notes>

<metadata>
```

**Example:**

```
chore: Upgrade RotoGLBridge.UI to .NET 10 LTS

Updated RotoGLBridge.UI project from .NET 8 to .NET 10 (LTS).

Changes:
- TargetFramework: net8.0-windows → net10.0-windows
- Microsoft.Extensions.DependencyInjection: 10.0.3 → 10.0.4
- Microsoft.Extensions.Logging: 10.0.3 → 10.0.4
- Microsoft.Extensions.DependencyInjection.Abstractions: 10.0.3 → 10.0.4
- Microsoft.Extensions.Logging.Abstractions: 10.0.3 → 10.0.4

Package Validation:
- MaterialDesignThemes 5.3.0: Compatible (no change)
- ScottPlot.WPF 5.1.57: Compatible (no change)

Testing:
- All 12 solution projects build successfully
- xUnit test suite: All tests pass
- Application launch: Successful
- UI rendering: MaterialDesign themes, 3D models (HelixToolkit), charts (ScottPlot) validated
- Plugin system: SharpDX, Speech, UsbWatcher all functional
- Performance: Startup time, memory usage, rendering FPS within acceptable ranges

All dependencies (11 projects) were already on .NET 10, making this an isolated upgrade.

Closes #<issue-number>
```

**Key Elements:**
1. **Type prefix:** `chore:` (infrastructure/tooling change)
2. **Short summary:** Concise description
3. **Detailed description:** What changed and why
4. **Testing notes:** Evidence of validation
5. **Metadata:** Issue references

---

### Review and Merge Process

#### Pre-Merge Checklist

Before merging `upgrade-to-NET10` → `net10`:

**Code Changes:**
- [ ] All changes in `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj` only
- [ ] TargetFramework updated to net10.0-windows
- [ ] Package versions updated per plan
- [ ] No unintended changes (verify with `git diff`)

**Build & Tests:**
- [ ] Solution builds with 0 errors
- [ ] All xUnit tests pass
- [ ] No new compiler warnings

**Functional Validation:**
- [ ] Application launches successfully
- [ ] UI rendering correct (themes, 3D, charts)
- [ ] Plugin system functional
- [ ] No runtime errors

**Performance:**
- [ ] Startup time acceptable
- [ ] Memory usage acceptable
- [ ] No performance regressions

**Documentation:**
- [ ] Commit message complete and accurate
- [ ] CHANGELOG updated (if applicable)
- [ ] Migration notes documented (if needed)

#### Merge Options

**Option 1: Direct Merge (Recommended)**

```bash
cd D:\source\mine\Roto
git checkout net10
git merge upgrade-to-NET10 --no-ff -m "Merge .NET 10 upgrade"
git push
```

**Rationale:**
- Simple upgrade with minimal risk
- Single commit or small commit set
- Preserves upgrade branch in history

---

**Option 2: Squash Merge (If Multiple Commits)**

```bash
git checkout net10
git merge upgrade-to-NET10 --squash
git commit -m "Upgrade RotoGLBridge.UI to .NET 10 LTS

[Full commit message from planning]"
git push
```

**Rationale:**
- Cleans up history if multiple fix commits occurred
- Creates single commit on target branch
- Easier to revert if needed

---

**Option 3: Rebase (Not Recommended)**

```bash
git checkout upgrade-to-NET10
git rebase net10
git checkout net10
git merge upgrade-to-NET10 --ff-only
```

**Not Recommended Because:**
- Unnecessary complexity for simple upgrade
- Changes commit history
- Direct merge or squash merge preferred

---

#### Pull Request (If Using)

If your workflow requires pull requests:

**PR Title:**
```
Upgrade RotoGLBridge.UI to .NET 10 LTS
```

**PR Description Template:**
```markdown
## Summary
Upgrades RotoGLBridge.UI project from .NET 8 to .NET 10 (LTS).

## Changes
- **Project:** src\RotoGLBridge.UI\RotoGLBridge.UI.csproj
- **TargetFramework:** net8.0-windows → net10.0-windows
- **Packages Updated:**
  - Microsoft.Extensions.DependencyInjection: 10.0.3 → 10.0.4
  - Microsoft.Extensions.Logging: 10.0.3 → 10.0.4
  - Microsoft.Extensions.DependencyInjection.Abstractions: 10.0.3 → 10.0.4
  - Microsoft.Extensions.Logging.Abstractions: 10.0.3 → 10.0.4
- **Packages Validated (No Change):**
  - MaterialDesignThemes 5.3.0 ✅
  - ScottPlot.WPF 5.1.57 ✅

## Testing
- [x] Solution builds with 0 errors
- [x] All xUnit tests pass
- [x] Application launches successfully
- [x] UI rendering validated (MaterialDesign, HelixToolkit, ScottPlot)
- [x] Plugin system functional (SharpDX, Speech, UsbWatcher)
- [x] Performance metrics acceptable

## Notes
All 11 dependency projects were already on .NET 10, making this an isolated single-project upgrade.

## Related Issues
Closes #<issue-number>
```

**PR Review Checklist:**
- [ ] Code changes reviewed (project file only)
- [ ] Build passes in CI (if applicable)
- [ ] Tests pass in CI
- [ ] Manual validation completed
- [ ] No breaking changes introduced

---

### Post-Merge Actions

After successful merge to `net10`:

1. **Delete Upgrade Branch (Optional)**
   ```bash
   git branch -d upgrade-to-NET10
   git push origin --delete upgrade-to-NET10
   ```

2. **Tag Release (If Applicable)**
   ```bash
   git tag -a v<version>-net10 -m "RotoGLBridge.UI upgraded to .NET 10"
   git push --tags
   ```

3. **Update Documentation**
   - Update README.md if it mentions framework version
   - Update build instructions if needed
   - Update deployment documentation

4. **Notify Stakeholders**
   - Inform team of upgrade completion
   - Share any observations or lessons learned
   - Update project status tracking

---

### Rollback Procedures

#### Before Merge (On upgrade-to-NET10 branch)

**Quick Rollback:**
```bash
git checkout net10
# Work continues on net10, upgrade-to-NET10 branch abandoned
```

**Modify and Retry:**
```bash
git checkout upgrade-to-NET10
git reset --hard <commit-before-upgrade>
# Start over with different approach
```

---

#### After Merge (On net10 branch)

**Revert Merge Commit:**
```bash
git revert -m 1 <merge-commit-hash>
git push
```

**Hard Reset (Use with Caution):**
```bash
git reset --hard <commit-before-merge>
git push --force
# Only if history rewrite is acceptable
```

---

### Best Practices

1. **Commit Atomically**
   - Single logical unit of work per commit
   - All related changes together
   - Makes rollback straightforward

2. **Write Descriptive Messages**
   - Explain *what* changed and *why*
   - Include testing notes
   - Reference issues/tickets

3. **Validate Before Committing**
   - Build successfully
   - Tests pass
   - Manual validation complete
   - Confidence in changes

4. **Preserve History**
   - Use merge commits (not rebasing) for feature branches
   - Maintain upgrade branch in history
   - Makes troubleshooting easier

5. **Clean Commits**
   - Don't commit commented-out code
   - Don't commit debug statements
   - Don't commit unrelated changes
   - Review `git diff` before committing

---

### Git Workflow Summary

**Full Workflow:**

```bash
# 1. Prepare (already done)
git checkout net10
git pull
# Commit any pending changes
git checkout -b upgrade-to-NET10

# 2. Execute Upgrade
# [Make changes to RotoGLBridge.UI.csproj]
# [Test thoroughly]

# 3. Commit (after successful validation)
git add src\RotoGLBridge.UI\RotoGLBridge.UI.csproj
git status  # Verify only intended files staged
git commit -m "Upgrade RotoGLBridge.UI to .NET 10 LTS

[Full commit message from template]"

# 4. Merge Back
git checkout net10
git pull  # Ensure up to date
git merge upgrade-to-NET10 --no-ff
git push

# 5. Clean Up (optional)
git branch -d upgrade-to-NET10
git push origin --delete upgrade-to-NET10
```

**Timeline:**
- Upgrade branch exists only during upgrade work
- Merged back to `net10` after validation
- Short-lived feature branch (hours to days, not weeks)

---

### Integration with CI/CD (If Applicable)

If continuous integration is configured:

**CI Validation on upgrade-to-NET10 Branch:**
- Automated build triggered on push
- Automated test execution
- Build artifact generation
- Status checks before merge

**Pre-Merge Requirements:**
- ✅ Build passes in CI
- ✅ All tests pass in CI
- ✅ Code quality checks pass
- ✅ Manual review approved

**Post-Merge Actions:**
- CI builds updated `net10` branch
- Deployment pipeline triggered (if configured)
- Release notes generated

---

### Collaboration Notes

**Single Developer Workflow:**
- Simple branch → upgrade → merge workflow
- Minimal coordination needed
- Fast iteration

**Team Workflow (If Applicable):**
- Communicate upgrade start/completion
- Coordinate to avoid conflicts
- Share findings during testing
- Collaborative PR review

**Communication Points:**
1. Before starting: "Beginning .NET 10 upgrade on upgrade-to-NET10 branch"
2. After successful build: "Build validated, proceeding to testing"
3. After testing: "Validation complete, ready for merge"
4. After merge: "Upgrade complete and merged to net10"

---

## Success Criteria

### Technical Success Criteria

The upgrade is considered technically successful when all of the following criteria are met:

#### 1. Framework Migration Complete

**Criterion:** RotoGLBridge.UI targets .NET 10.0-windows

**Validation:**
- [ ] `<TargetFramework>net10.0-windows</TargetFramework>` in `src\RotoGLBridge.UI\RotoGLBridge.UI.csproj`
- [ ] Project loads correctly in Visual Studio/Rider
- [ ] IDE recognizes project as .NET 10 target

**Evidence:** Project file inspection

---

#### 2. All Package Updates Applied

**Criterion:** All required package updates completed per plan

**Validation:**
- [ ] Microsoft.Extensions.DependencyInjection: 10.0.4
- [ ] Microsoft.Extensions.Logging: 10.0.4
- [ ] Microsoft.Extensions.DependencyInjection.Abstractions: 10.0.4
- [ ] Microsoft.Extensions.Logging.Abstractions: 10.0.4
- [ ] MaterialDesignThemes: 5.3.0 (validated as compatible)
- [ ] ScottPlot.WPF: 5.1.57 (validated as compatible)
- [ ] All other packages: Unchanged and compatible

**Evidence:** Project file inspection, NuGet package manager, `dotnet list package`

---

#### 3. Solution Builds Successfully

**Criterion:** Entire solution builds with 0 errors

**Validation:**
- [ ] `dotnet build RotoGLBridge.slnx --configuration Release` completes successfully
- [ ] All 12 projects compile without errors
- [ ] Exit code 0 from build command
- [ ] Build output shows "Build succeeded"

**Evidence:** Build log, command line output

---

#### 4. No New Compiler Warnings

**Criterion:** No new warnings introduced by upgrade

**Validation:**
- [ ] Warning count same or lower than .NET 8 baseline
- [ ] No framework-related warnings
- [ ] No package-related warnings
- [ ] No obsolete API warnings

**Evidence:** Build log comparison

---

#### 5. All Package Dependencies Resolved

**Criterion:** No package conflicts or missing dependencies

**Validation:**
- [ ] `dotnet restore` completes successfully
- [ ] No dependency conflict warnings
- [ ] No missing package errors
- [ ] Package graph resolves correctly

**Evidence:** Restore log

---

#### 6. All Automated Tests Pass

**Criterion:** 100% of xUnit tests in RotoGLBridge.Tests pass

**Validation:**
- [ ] `dotnet test src\RotoGLBridge.Tests\RotoGLBridge.Tests.csproj` passes
- [ ] All tests executed (no skipped tests unintentionally)
- [ ] No new test failures introduced
- [ ] Test pass rate same as .NET 8 baseline

**Evidence:** Test execution report

---

#### 7. Application Launches Successfully

**Criterion:** RotoGLBridge.UI application starts without exceptions

**Validation:**
- [ ] Application executable launches
- [ ] Main window displays
- [ ] No startup exceptions in logs
- [ ] Dependency injection container initializes
- [ ] Application reaches "ready" state

**Evidence:** Manual verification, log files

---

#### 8. UI Rendering Correct

**Criterion:** User interface displays correctly with no visual regressions

**Validation:**
- [ ] Main window layout correct
- [ ] MaterialDesignThemes styles applied
- [ ] All controls render properly (buttons, text boxes, etc.)
- [ ] Icons and images display
- [ ] Text readable and properly formatted
- [ ] No rendering artifacts

**Evidence:** Visual inspection, screenshots

---

#### 9. 3D Rendering Functional

**Criterion:** HelixToolkit 3D rendering works correctly

**Validation:**
- [ ] 3D viewport initializes
- [ ] .obj models load successfully:
  - [ ] assets\rotovr\base.obj
  - [ ] assets\rotovr\chair.obj
  - [ ] assets\roto2\base.obj
  - [ ] assets\roto2\chair.obj
- [ ] Materials (.mtl) apply correctly
- [ ] Camera navigation works (rotate, zoom, pan)
- [ ] No DirectX/SharpDX errors

**Evidence:** Manual testing, visual verification

---

#### 10. Charting Functional

**Criterion:** ScottPlot charting works correctly

**Validation:**
- [ ] Plot controls initialize in XAML
- [ ] Data displays correctly
- [ ] Interactive features work (zoom, pan)
- [ ] Plot styling correct
- [ ] No rendering errors

**Evidence:** Manual testing

---

#### 11. Plugin System Functional

**Criterion:** All Sharpie plugins load and function correctly

**Validation:**
- [ ] Sharpie.Plugins.SharpDX loads
- [ ] Sharpie.Plugins.Speech loads
- [ ] Sharpie.Plugins.UsbWatcher loads
- [ ] XInput controller functionality works (if available)
- [ ] Speech functionality works (if applicable)
- [ ] USB device detection works
- [ ] No plugin load errors in logs

**Evidence:** Manual testing, log files

---

#### 12. Logging Functional

**Criterion:** NLog logging works correctly

**Validation:**
- [ ] Log files created
- [ ] Log entries written during operation
- [ ] Log format correct
- [ ] Log levels respected (Debug, Info, Warn, Error)
- [ ] No logging errors

**Evidence:** Log file inspection

---

#### 13. Core Library Integration

**Criterion:** Integration with RotoGLBridge.csproj works correctly

**Validation:**
- [ ] Core library functionality accessible from UI
- [ ] Data flows correctly between UI and core
- [ ] No framework mismatch errors
- [ ] All interfaces work as expected

**Evidence:** Manual testing, no runtime errors

---

#### 14. USB Communication

**Criterion:** rotoUSB library integration works correctly

**Validation:**
- [ ] USB device detection functional
- [ ] USB communication protocols work
- [ ] Device enumeration successful
- [ ] No USB errors

**Evidence:** Manual testing (with USB device if available)

---

#### 15. No Runtime Errors

**Criterion:** Application runs without exceptions during normal operation

**Validation:**
- [ ] No unhandled exceptions
- [ ] No framework-related errors
- [ ] No package-related errors
- [ ] Application stable during typical usage

**Evidence:** Manual testing, log files, no crash reports

---

### Quality Criteria

#### Code Quality Maintained

**Validation:**
- [ ] No new code smells introduced
- [ ] Project file syntax clean and valid
- [ ] No temporary/debug code committed
- [ ] Consistent formatting maintained

---

#### Test Coverage Maintained

**Validation:**
- [ ] All existing tests still execute
- [ ] Test coverage percentage unchanged (or improved)
- [ ] No tests removed without justification

---

#### Documentation Updated

**Validation:**
- [ ] Commit message complete and accurate
- [ ] CHANGELOG updated (if applicable)
- [ ] README.md updated if framework version mentioned
- [ ] Build instructions updated if needed

---

### Process Criteria

#### All-At-Once Strategy Followed

**Validation:**
- [ ] Framework and package updates done atomically
- [ ] No intermediate multi-targeting (e.g., net8.0-windows;net10.0-windows)
- [ ] Single coordinated upgrade completed
- [ ] All changes in one commit (or minimal commits if issues required fixes)

---

#### All-At-Once Strategy Principles Applied

**Validation:**
- [ ] All project file updates applied simultaneously
- [ ] All package updates applied together
- [ ] Build performed after all updates complete
- [ ] Single unified testing phase
- [ ] Single commit approach maintained (or justified multi-commit)

---

#### Source Control Strategy Followed

**Validation:**
- [ ] Upgrade work performed on `upgrade-to-NET10` branch
- [ ] Changes committed with comprehensive commit message
- [ ] Branch merged back to `net10` after validation
- [ ] Clean Git history maintained

---

#### Testing Strategy Completed

**Validation:**
- [ ] Build validation performed
- [ ] Automated tests executed
- [ ] Manual functional testing completed
- [ ] Integration testing completed
- [ ] Performance validation completed
- [ ] Visual validation completed

---

#### Risk Management Applied

**Validation:**
- [ ] Identified risks monitored during execution
- [ ] Mitigation strategies applied as planned
- [ ] Contingency plans available (not necessarily used)
- [ ] Issues addressed appropriately

---

### Performance Criteria

#### Startup Time Acceptable

**Validation:**
- [ ] Application startup time within ±10% of .NET 8 baseline
- [ ] No significant delay introduced

**Measurement:** Time from launch to main window displayed

---

#### Memory Usage Acceptable

**Validation:**
- [ ] Memory consumption within ±10% of .NET 8 baseline
- [ ] No memory leaks detected during typical usage
- [ ] Memory profile similar to baseline

**Measurement:** Task Manager or profiler during 15-30 minute session

---

#### Rendering Performance Acceptable

**Validation:**
- [ ] 3D rendering frame rate within ±5% of .NET 8 baseline
- [ ] No stuttering or lag during navigation
- [ ] Smooth user interactions

**Measurement:** Frame rate during 3D model interaction

---

#### UI Responsiveness Maintained

**Validation:**
- [ ] Button clicks responsive
- [ ] Navigation smooth
- [ ] No noticeable delays in user interactions

**Measurement:** Subjective assessment during manual testing

---

### Security Criteria

#### No New Security Vulnerabilities

**Validation:**
- [ ] No packages with known CVEs
- [ ] Security scan passes (if applicable)
- [ ] No security-related warnings

**Evidence:** NuGet vulnerability check, security scan results

---

#### Security Best Practices Maintained

**Validation:**
- [ ] Sensitive data not logged
- [ ] Authentication/authorization unchanged (if applicable)
- [ ] Secure communication maintained

---

### Rollback Criteria (Definition of Failure)

The upgrade should be rolled back if any of the following occur:

#### Critical Failures

- ❌ **Application does not start** (unhandled exceptions at launch)
- ❌ **Core functionality broken** (plugin system fails, 3D rendering broken)
- ❌ **Data loss or corruption** (unlikely for this project, but critical if occurs)
- ❌ **Security vulnerability introduced**
- ❌ **Build failures unresolvable** within reasonable effort

#### Major Issues Requiring Rollback

- ⚠️ **>25% of tests fail** and root cause unclear
- ⚠️ **Significant performance regression** (>20% slower)
- ⚠️ **Critical dependency incompatibility** unresolvable
- ⚠️ **Stakeholder rejection** of upgrade outcome

#### Minor Issues NOT Requiring Rollback

- ✅ Individual test failures (investigate and fix)
- ✅ Minor visual differences (acceptable if within tolerance)
- ✅ Small performance variations (within ±10%)
- ✅ Solvable build warnings

---

### Acceptance Checklist

**Final sign-off requires all items checked:**

#### Pre-Commit Validation
- [ ] All technical success criteria met (items 1-15)
- [ ] All quality criteria met
- [ ] All process criteria followed
- [ ] All performance criteria acceptable
- [ ] No critical or major issues present

#### Commit Validation
- [ ] Commit message complete per template
- [ ] Only intended files committed (RotoGLBridge.UI.csproj)
- [ ] No debug or temporary code committed

#### Post-Commit Validation
- [ ] Branch merged to `net10` successfully
- [ ] Final build on `net10` branch successful
- [ ] Documentation updated
- [ ] Stakeholders notified (if applicable)

---

### Definition of Done

**The .NET 10 upgrade is DONE when:**

1. ✅ All technical success criteria met (15 items)
2. ✅ All quality criteria maintained
3. ✅ All process criteria followed
4. ✅ All performance criteria acceptable
5. ✅ All security criteria maintained
6. ✅ Changes committed with comprehensive message
7. ✅ Upgrade branch merged to `net10`
8. ✅ Final validation on `net10` branch successful
9. ✅ Documentation complete
10. ✅ No blocking issues remain

**Evidence Package:**
- ✅ Build log showing successful compilation
- ✅ Test execution report showing all tests pass
- ✅ Manual testing checklist completed
- ✅ Performance metrics documented
- ✅ Screenshots of validated UI
- ✅ Commit history clean and descriptive

---

### Sign-Off Template

```markdown
# .NET 10 Upgrade - Sign-Off

## Project
RotoGLBridge.UI upgrade from .NET 8 to .NET 10 LTS

## Date
[Date of completion]

## Validation Summary
- ✅ All 15 technical success criteria met
- ✅ All quality criteria maintained
- ✅ All-At-Once strategy principles applied
- ✅ Source control strategy followed
- ✅ Testing strategy completed
- ✅ Performance criteria acceptable
- ✅ No security issues identified

## Test Results
- Build: Success (0 errors, 0 new warnings)
- Automated Tests: [X/X] passed (100%)
- Manual Testing: All checklist items validated
- Performance: Within acceptable ranges

## Issues Found
[None / List any minor issues found and resolved]

## Recommendation
✅ **APPROVE** - Upgrade successful, ready for merge to net10

## Reviewer
[Name]

## Signature
[Digital signature or approval indicator]
```

---

### Continuous Improvement

After successful completion, consider documenting:

**Lessons Learned:**
- What went well?
- What could be improved?
- Any unexpected findings?

**Process Improvements:**
- Were success criteria sufficient?
- Was testing strategy appropriate?
- Any missing validation steps?

**Future Upgrades:**
- Template for next .NET upgrade
- Automation opportunities identified
- Risk factors to monitor

This feedback improves future upgrade processes.
