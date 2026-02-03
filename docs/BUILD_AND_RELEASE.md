# Build and Release Guide for SimControlCentre v1.3.0

## Quick Start

```powershell
# Update version numbers first (see Version Management section)
.\build-release.ps1
# Output: Installers\SimControlCentre-Setup-v1.3.0.exe (2.36 MB)
#         Installers\SimControlCentre-Setup-Standalone-v1.3.0.exe (46.3 MB)
```

## Prerequisites

- **Visual Studio 2022** with .NET 8.0 SDK
- **Inno Setup 6** - https://jrsoftware.org/isdl.php
- **Git**

## Build Script Overview

The `build-release.ps1` script automates the entire build process:

1. ? Cleans previous builds
2. ? **Builds GoXLR plugin first** (NEW in v1.3.0)
3. ? Builds framework-dependent version
4. ? **Copies plugins to Plugins folder** (NEW in v1.3.0)
5. ? Builds self-contained version  
6. ? **Copies plugins to self-contained build** (NEW in v1.3.0)
7. ? Creates both installers with Inno Setup

## Version Management

**IMPORTANT:** Update version in 4 locations before building:

### 1. SimControlCentre/SimControlCentre.csproj
```xml
<AssemblyVersion>1.3.0</AssemblyVersion>
<FileVersion>1.3.0</FileVersion>
<Version>1.3.0</Version>
```

### 2. installer.iss
```
#define MyAppVersion "1.3.0"
```

### 3. installer-standalone.iss
```
#define MyAppVersion "1.3.0"
```

### 4. build-release.ps1 (header)
```powershell
# SimControlCentre v1.3.0 Release Build Script
```

## Installer Details

### Framework-Dependent (~2.36 MB)
- **File:** `SimControlCentre-Setup-v1.3.0.exe`
- **Requires:** .NET 8 Runtime on user's machine
- **Includes:** 
  - SimControlCentre.exe
  - Dependencies
  - **Plugins\SimControlCentre.Plugins.GoXLR.dll**
  - README, QUICKSTART, LICENSE

### Self-Contained (~46.3 MB)
- **File:** `SimControlCentre-Setup-Standalone-v1.3.0.exe`
- **Requires:** Nothing (includes .NET 8 Runtime)
- **Includes:**
  - SimControlCentre.exe
  - .NET 8 Runtime
  - All dependencies
  - **Plugins\SimControlCentre.Plugins.GoXLR.dll**
  - README, QUICKSTART, LICENSE

## Plugin Inclusion (NEW in v1.3.0)

**Critical:** Plugins MUST be included in installers for GoXLR features to work.

The build script now:
1. Builds `SimControlCentre.Plugins.GoXLR.dll`
2. Copies to `Plugins\` folder in both publish outputs
3. Inno Setup includes `Plugins\*.dll` in installers

**Verify plugins are included:**
```powershell
Get-ChildItem "SimControlCentre\bin\Release\Publish\Plugins"
Get-ChildItem "SimControlCentre\bin\Release\net8.0-windows\win-x64\publish\Plugins"
```

Both should show `SimControlCentre.Plugins.GoXLR.dll`.

## Release Process

### 1. Update Version Numbers
Update all 4 version locations (see above)

### 2. Build Installers
```powershell
.\build-release.ps1
```

### 3. Test Installers (Optional but Recommended)
- Test on clean VM or machine
- Verify plugins load (check Plugins tab)
- Test GoXLR features
- Test flag lighting

### 4. Create Git Tag
```powershell
git add -A
git commit -m "Release v1.3.0"
git tag -a v1.3.0 -m "Version 1.3.0 - Timeline scrubber and flag lighting improvements"
git push origin master
git push origin v1.3.0
```

### 5. Create GitHub Release
1. Go to: https://github.com/dcunliffe1980/SimControlCentre/releases
2. Click "Draft a new release"
3. Select tag: `v1.3.0`
4. Title: `SimControlCentre v1.3.0`
5. Copy content from `GITHUB_RELEASE_v1.3.0.md`
6. **Upload both installers:**
   - `Installers\SimControlCentre-Setup-v1.3.0.exe`
   - `Installers\SimControlCentre-Setup-Standalone-v1.3.0.exe`
7. Click "Publish release"

## File Locations After Build

```
SimControlCentre/
??? Installers/
?   ??? SimControlCentre-Setup-v1.3.0.exe              # 2.36 MB
?   ??? SimControlCentre-Setup-Standalone-v1.3.0.exe   # 46.3 MB
?
??? SimControlCentre/bin/Release/Publish/              # Framework-dependent
?   ??? SimControlCentre.exe
?   ??? *.dll
?   ??? Plugins/
?       ??? SimControlCentre.Plugins.GoXLR.dll
?
??? SimControlCentre/bin/Release/net8.0-windows/win-x64/publish/  # Self-contained
    ??? SimControlCentre.exe
    ??? *.dll (includes .NET Runtime)
    ??? Plugins/
        ??? SimControlCentre.Plugins.GoXLR.dll
```

## Troubleshooting

### "Plugins not found" after install
- **Cause:** Build script didn't copy plugins
- **Fix:** Verify `Plugins\` folder exists in publish folders
- **Check:** `build-release.ps1` contains plugin build and copy steps

### Installer creation fails
- **Cause:** Inno Setup not installed or wrong path
- **Fix:** Install from https://jrsoftware.org/isdl.php
- **Default path:** `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`

### Plugin doesn't load after install
- **Check logs:** `%LOCALAPPDATA%\SimControlCentre\logs\`
- **Verify:** `Plugins` folder exists in install directory
- **Verify:** `SimControlCentre.Plugins.GoXLR.dll` exists in `Plugins\` folder

## What's New in v1.3.0 Build Process

- ? **Plugin build step** added to build script
- ? **Plugin copy** to both publish folders
- ? **Installer scripts** updated to include `Plugins\*.dll`
- ? **Self-contained build** changed from single-file to multi-file (for plugin support)
- ? **Version updated** to 1.3.0 in all locations

## Next Steps After Release

1. Announce release on GitHub
2. Update project README.md with new version number
3. Close any related GitHub issues
4. Monitor for user feedback
5. Plan next version features

## Rollback Procedure

If critical bug found after release:

1. Pull previous tag: `git checkout v1.2.0`
2. Rebuild: `.\build-release.ps1`
3. Create hotfix release: `v1.2.1`
4. Document issue in release notes

## Support

For build issues:
- Check GitHub Issues
- Review logs in workspace
- Verify prerequisites installed
