# Version Management Guide

## Overview
This document explains how version numbers are managed in SimControlCentre to ensure consistency across all components.

## Single Source of Truth: .csproj File

The **only place** you need to update the version number is in `SimControlCentre.csproj`:

```xml
<PropertyGroup>
  <AssemblyVersion>1.1.2</AssemblyVersion>
  <FileVersion>1.1.2</FileVersion>
  <Version>1.1.2</Version>
</PropertyGroup>
```

All three properties should have the same version number.

## How Version Propagates

### Automatic (via Assembly metadata)
These components automatically read from the assembly version at runtime:

1. **AboutTab** - Displays `Version X.X.X` in the UI
   - Location: `SimControlCentre/Views/Tabs/AboutTab.xaml.cs`
   - Method: `GetVersion()` reads `Assembly.GetExecutingAssembly().GetName().Version`

2. **UpdateService** - Reports current version to GitHub API
   - Location: `SimControlCentre/Services/UpdateService.cs`
   - Method: `GetCurrentVersion()` reads assembly version
   - Fallback: Returns hardcoded "1.1.2" if assembly read fails

3. **SettingsTab** - Displays version in About section
   - Uses `UpdateService.GetCurrentVersion()`

### Manual (must be updated manually)
These files need manual updates when releasing:

1. **installer.iss** - Inno Setup script for regular installer
   ```
   #define MyAppVersion "1.1.2"
   ```

2. **installer-standalone.iss** - Inno Setup script for standalone installer
   ```
   #define MyAppVersion "1.1.2"
   ```

3. **build-release.ps1** - Build script header (cosmetic only)
   ```powershell
   # SimControlCentre v1.1.2 Release Build Script
   ```

## Release Checklist for Version Updates

When releasing a new version (e.g., 1.1.3):

### Step 1: Update .csproj (PRIMARY)
```xml
<AssemblyVersion>1.1.3</AssemblyVersion>
<FileVersion>1.1.3</FileVersion>
<Version>1.1.3</Version>
```

### Step 2: Update Installer Scripts
In `installer.iss`:
```
#define MyAppVersion "1.1.3"
```

In `installer-standalone.iss`:
```
#define MyAppVersion "1.1.3"
```

### Step 3: Update Build Script (Optional)
In `build-release.ps1`:
```powershell
# SimControlCentre v1.1.3 Release Build Script
Write-Host "SimControlCentre v1.1.3 Release Build"
```

### Step 4: Build
```powershell
dotnet build
powershell -ExecutionPolicy Bypass -File build-release.ps1
```

### Step 5: Verify
- Run the app
- Go to Settings ? About
- Verify it shows "Version 1.1.3"
- Check installer filenames are correct

## Why This Approach?

### Benefits
- ? **Single source of truth** for code (.csproj)
- ? **No hardcoded versions** in C# or XAML code
- ? **Automatic UI updates** when assembly version changes
- ? **Consistent version reporting** across all components
- ? **Less error-prone** - fewer places to update

### Limitations
- ?? Installer scripts must be updated manually
  - This is by design - installer version must match assembly version
  - Build script validates this during release

## Version Format

We use **semantic versioning**: `Major.Minor.Patch`

- **Major**: Breaking changes (1.x.x ? 2.0.0)
- **Minor**: New features, backward compatible (1.1.x ? 1.2.0)
- **Patch**: Bug fixes, backward compatible (1.1.2 ? 1.1.3)

## Troubleshooting

### "About screen shows wrong version"
- Check that `.csproj` version was updated
- Rebuild the solution (not just F5)
- Clear `bin/` and `obj/` folders if needed

### "Installer has wrong version in filename"
- Check `installer.iss` and `installer-standalone.iss`
- Version must match `.csproj`

### "UpdateService reports wrong version"
- Check fallback version in `UpdateService.cs`
- Should match `.csproj` version

### "Assembly version not reading correctly"
- This is very rare
- Check build output for warnings
- Ensure `.csproj` has all three version properties

## Future Improvements

### Possible Enhancements
1. **PowerShell script to sync versions**
   - Read version from .csproj
   - Update installer scripts automatically
   - Validate all versions match

2. **Pre-build task**
   - MSBuild task to validate versions
   - Fail build if versions mismatch

3. **CI/CD integration**
   - GitHub Actions to enforce version consistency
   - Automated release creation

## Files Reference

### Version is read from:
- `SimControlCentre/SimControlCentre.csproj` (primary source)

### Version is used by:
- `SimControlCentre/Views/Tabs/AboutTab.xaml.cs` (dynamic)
- `SimControlCentre/Services/UpdateService.cs` (dynamic)
- `SimControlCentre/Views/Tabs/SettingsTab.xaml.cs` (via UpdateService)

### Version must be updated manually:
- `installer.iss`
- `installer-standalone.iss`
- `build-release.ps1` (cosmetic)

---

**Last Updated**: 2025-01-02  
**Current Version**: 1.1.2
