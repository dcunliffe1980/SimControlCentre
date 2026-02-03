# Build and Release Instructions

## Building Installers

### Prerequisites
- **Inno Setup 6** - Download from https://jrsoftware.org/isdl.php
- **.NET 8 SDK** - For building the application
- **PowerShell** - For running build scripts

### Build Scripts

Located in root directory:
- `build-release.ps1` - Main build script
- `installer.iss` - Inno Setup script (framework-dependent installer)
- `installer-standalone.iss` - Inno Setup script (standalone installer)

### Building Process

#### 1. Update Version Number
Before building, update version in:
- `SimControlCentre/SimControlCentre.csproj`:
  ```xml
  <AssemblyVersion>1.2.0</AssemblyVersion>
  <FileVersion>1.2.0</FileVersion>
  <Version>1.2.0</Version>
  ```

#### 2. Run Build Script
```powershell
# Close the app first!
.\build-release.ps1
```

This creates two installers in `Installers/` folder:

#### 3. Output Files

**Standalone Installer** (~70MB):
- **File**: `SimControlCentre-Setup-Standalone-v1.2.0.exe`
- **Includes**: .NET 8 Runtime bundled
- **Users**: Anyone, no prerequisites
- **Build**: Uses `installer-standalone.iss`

**Framework-Dependent Installer** (~3MB):
- **File**: `SimControlCentre-Setup-v1.2.0.exe`
- **Requires**: .NET 8 Runtime already installed
- **Users**: Power users who already have .NET 8
- **Build**: Uses `installer.iss`

### Build Locations

```
C:\Users\david\source\repos\SimControlCentre\
??? bin\
?   ??? Release\
?       ??? net8.0-windows\          # Build output
?           ??? SimControlCentre.exe
??? Installers\                      # ? Final installers here!
?   ??? SimControlCentre-Setup-Standalone-v1.2.0.exe
?   ??? SimControlCentre-Setup-v1.2.0.exe
??? installer.iss                    # Framework-dependent config
??? installer-standalone.iss         # Standalone config
```

---

## Creating GitHub Release

### 1. Prepare Release

**Requirements**:
- ? Version updated in `.csproj`
- ? Release notes created
- ? All changes committed and pushed
- ? Installers built successfully
- ? Tested on clean machine

### 2. Create Git Tag

**Important**: The tag is `v1.2.0`, NOT `RELEASE_NOTES_v1.2.0.md`!

```bash
git tag v1.2.0
git push origin v1.2.0
```

### 3. Create Release on GitHub

1. Go to https://github.com/dcunliffe1980/SimControlCentre/releases/new
2. **Tag**: `v1.2.0` (select or create)
3. **Target**: `master` branch
4. **Title**: `v1.2.0 - iRacing Flag Lighting`
5. **Description**: Copy from `GITHUB_RELEASE_v1.2.0.md`

### 4. Upload Assets

Upload both installers from `Installers/` folder:
- ? `SimControlCentre-Setup-Standalone-v1.2.0.exe` (70MB)
- ? `SimControlCentre-Setup-v1.2.0.exe` (3MB)

### 5. Publish Release

Click "Publish release" - Done! ??

---

## Release Checklist

Use `RELEASE_CHECKLIST_v1.2.0.md` for step-by-step guidance.

### Pre-Release
- [ ] Version updated in `.csproj`
- [ ] Release notes created
- [ ] All features tested
- [ ] All commits pushed
- [ ] App closed (unlock exe)

### Build
- [ ] Run `build-release.ps1`
- [ ] Verify both installers created
- [ ] Test standalone installer on clean VM
- [ ] Test framework-dependent installer

### Release
- [ ] Create Git tag `v1.2.0`
- [ ] Push tag to GitHub
- [ ] Create GitHub release
- [ ] Upload both installers
- [ ] Verify downloads work

### Post-Release
- [ ] Update README.md if needed
- [ ] Announce on Discord/Forums
- [ ] Monitor for issues

---

## Installer Configuration

### Framework-Dependent (`installer.iss`)

```ini
#define MyAppVersion "1.2.0"
#define MyAppPublisher "Your Name"
#define MyAppName "SimControlCentre"

[Setup]
OutputBaseFilename=SimControlCentre-Setup-v{#MyAppVersion}
OutputDir=Installers
```

Key settings:
- Output: `Installers/SimControlCentre-Setup-v1.2.0.exe`
- Size: ~3MB
- Requires: .NET 8 Runtime

### Standalone (`installer-standalone.iss`)

```ini
#define MyAppVersion "1.2.0"
#define MyAppPublisher "Your Name"
#define MyAppName "SimControlCentre"

[Setup]
OutputBaseFilename=SimControlCentre-Setup-Standalone-v{#MyAppVersion}
OutputDir=Installers
```

Key settings:
- Output: `Installers/SimControlCentre-Setup-Standalone-v1.2.0.exe`
- Size: ~70MB
- Includes: .NET 8 Runtime bundled

---

## Troubleshooting

### "File is locked" Error

**Problem**: Build fails with "file is being used by another process"

**Solution**:
1. Close SimControlCentre.exe
2. Check Task Manager for running instances
3. End all SimControlCentre processes
4. Run build again

### Installers Not Created

**Problem**: Build succeeds but no installers in `Installers/`

**Solution**:
1. Install Inno Setup 6
2. Add Inno Setup to PATH
3. Check `installer.iss` and `installer-standalone.iss` exist
4. Run `build-release.ps1` with admin rights

### Wrong Version in Installer

**Problem**: Installer shows old version number

**Solution**:
1. Update version in `SimControlCentre.csproj`
2. Update `#define MyAppVersion` in `.iss` files
3. Clean build: `dotnet clean` then rebuild

---

## Version Naming Convention

**Semantic Versioning**: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (1.x.x ? 2.0.0)
- **MINOR**: New features (1.1.x ? 1.2.0)
- **PATCH**: Bug fixes (1.2.0 ? 1.2.1)

**Examples**:
- `v1.2.0` - iRacing lighting features added
- `v1.2.1` - Bug fixes for lighting
- `v2.0.0` - Major rewrite or breaking changes

**File Naming**:
- Git Tag: `v1.2.0`
- Standalone: `SimControlCentre-Setup-Standalone-v1.2.0.exe`
- Framework: `SimControlCentre-Setup-v1.2.0.exe`
- Release Notes: `RELEASE_NOTES_v1.2.0.md` (filename only)

---

## Testing Releases

### Before Publishing

1. **Test Standalone Installer**:
   - Install on clean Windows 10/11 VM
   - Verify app launches
   - Test all features

2. **Test Framework-Dependent Installer**:
   - Install on system with .NET 8
   - Verify smaller download
   - Test all features

3. **Upgrade Test**:
   - Install v1.1.2
   - Upgrade to v1.2.0
   - Verify settings preserved

### After Publishing

1. Download from GitHub releases
2. Verify both installers work
3. Check file sizes correct
4. Monitor GitHub Issues for problems

---

**Last Updated**: February 2026  
**Current Version**: 1.2.0
