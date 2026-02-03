# Release Process Guide

**For AI Assistants**: This document explains the complete release process that the user expects. Follow these steps exactly.

---

## User Preferences

### ?? CRITICAL: Git Tag Format
- **Tag**: v1.2.0 (just the version, NO link formatting)
- **NOT**: `v1.2.0` (no markdown formatting)
- **NOT**: [v1.2.0] (no brackets)
- **NOT**: RELEASE_NOTES_v1.2.0.md (that's a filename)

### Communication Style
When telling the user about releases:
- Be clear and concise
- Don't add extra formatting to tag names
- State the tag plainly: "Tag: v1.2.0"

---

## Complete Release Process

### Step 1: Update Version Numbers

**Files to update**:
1. `SimControlCentre/SimControlCentre.csproj`
   ```xml
   <AssemblyVersion>1.2.0</AssemblyVersion>
   <FileVersion>1.2.0</FileVersion>
   <Version>1.2.0</Version>
   ```

2. `build-release.ps1`
   ```powershell
   # SimControlCentre v1.2.0 Release Build Script
   Write-Host "SimControlCentre v1.2.0 Release Build"
   ```

3. `installer.iss`
   ```ini
   #define MyAppVersion "1.2.0"
   ```

4. `installer-standalone.iss`
   ```ini
   #define MyAppVersion "1.2.0"
   ```

**Commit**:
```bash
git add -A
git commit -m "Bump version to v1.2.0"
git push origin master
```

---

### Step 2: Create Release Documentation

**Files to create**:

1. **`RELEASE_NOTES_v1.2.0.md`** - Comprehensive release notes
   - What's new
   - Bug fixes
   - Installation instructions
   - Known issues

2. **`GITHUB_RELEASE_v1.2.0.md`** - GitHub release description
   - Shorter, focused version
   - Copy this into GitHub release

**Commit**:
```bash
git add -A
git commit -m "Add release documentation for v1.2.0"
git push origin master
```

---

### Step 3: Create Git Tag

**AI Assistant: Create the tag for the user!**

```bash
git tag v1.2.0
git push origin v1.2.0
```

**Show confirmation**:
```
? Git tag v1.2.0 created and pushed
```

---

### Step 4: Build Installers

**Command**:
```powershell
.\build-release.ps1
```

**Output location**: `C:\Users\david\source\repos\SimControlCentre\Installers\`

**Files created**:
- `SimControlCentre-Setup-v1.2.0.exe` (~2-3MB) - Framework-dependent
- `SimControlCentre-Setup-Standalone-v1.2.0.exe` (~60-70MB) - Includes .NET 8

**Verify**:
```powershell
Get-ChildItem Installers -Filter "*v1.2.0.exe"
```

**Show user** the files created with sizes.

---

### Step 5: Display Release Information

**AI Assistant: Show the release notes in the conversation!**

Read `GITHUB_RELEASE_v1.2.0.md` and display it formatted in the conversation so the user can review it.

**Example**:
```markdown
# ?? SimControlCentre v1.2.0 - iRacing Flag Lighting

[Full content of GITHUB_RELEASE_v1.2.0.md here]
```

---

### Step 6: Create GitHub Release

**AI Assistant: Provide clear, simple instructions:**

```markdown
## Create GitHub release:
- Tag: v1.2.0
- Target: master
- Title: v1.2.0 - iRacing Flag Lighting
- Description: Copy from GITHUB_RELEASE_v1.2.0.md (displayed above)
- Upload: Both installers from Installers/ folder
```

**Link**: https://github.com/dcunliffe1980/SimControlCentre/releases/new

**Important**: 
- Don't add markdown formatting to the tag name
- Just state it plainly: "Tag: v1.2.0"

---

## Release Checklist Template

Use this checklist for every release:

### Pre-Release
- [ ] Version updated in 4 files (csproj, build script, 2 installer scripts)
- [ ] Release notes created (RELEASE_NOTES_vX.X.X.md)
- [ ] GitHub release template created (GITHUB_RELEASE_vX.X.X.md)
- [ ] All features tested
- [ ] Documentation updated
- [ ] All commits pushed

### Git Tag
- [ ] AI creates Git tag: `git tag vX.X.X`
- [ ] AI pushes tag: `git push origin vX.X.X`
- [ ] Confirm tag created

### Build
- [ ] App closed (unlock exe)
- [ ] Run `.\build-release.ps1`
- [ ] Verify both installers created in `Installers/` folder
- [ ] Check file sizes reasonable

### Release Notes Display
- [ ] AI displays formatted content of `GITHUB_RELEASE_vX.X.X.md` in conversation
- [ ] User can review before creating GitHub release

### GitHub Release
- [ ] AI provides simple instructions (Tag: vX.X.X - no formatting)
- [ ] User creates release on GitHub
- [ ] User uploads both installers
- [ ] User publishes release

### Post-Release
- [ ] Verify GitHub release is live
- [ ] Test download links work
- [ ] Update README.md if needed
- [ ] Announce to users

---

## Example Release Session (What AI Should Do)

### User says: "Let's release v1.2.0"

**AI Response**:

1. **Update versions** (show what's being updated)
2. **Create release docs** (create files)
3. **Create Git tag** (do it automatically):
   ```bash
   git tag v1.2.0
   git push origin v1.2.0
   ```
   ? Git tag v1.2.0 created and pushed

4. **Build installers** (run script, show output)
5. **Display release notes** (show formatted GITHUB_RELEASE_vX.X.X.md)
6. **Provide GitHub instructions**:
   ```markdown
   ## Create GitHub release:
   - Tag: v1.2.0
   - Title: v1.2.0 - iRacing Flag Lighting
   - Upload: Both installers from Installers/ folder
   ```

---

## File Naming Conventions

### Git Tag
- Format: `v1.2.0`
- NOT: `RELEASE_NOTES_v1.2.0.md`

### Installers
- Framework: `SimControlCentre-Setup-v1.2.0.exe`
- Standalone: `SimControlCentre-Setup-Standalone-v1.2.0.exe`

### Documentation
- Release notes: `RELEASE_NOTES_v1.2.0.md`
- GitHub template: `GITHUB_RELEASE_v1.2.0.md`
- Checklist: `RELEASE_CHECKLIST_v1.2.0.md` (optional)

---

## Troubleshooting

### "File is locked" Error
**Problem**: Build fails - SimControlCentre.exe is running

**Solution**:
1. Close the app
2. Check Task Manager
3. End all SimControlCentre processes
4. Run build again

### Git Tag Already Exists
**Problem**: Tag vX.X.X already exists

**Solution**:
```bash
# Delete local tag
git tag -d v1.2.0

# Delete remote tag
git push origin :refs/tags/v1.2.0

# Create new tag
git tag v1.2.0
git push origin v1.2.0
```

### Installers Have Wrong Version
**Problem**: Installer filename shows old version

**Solution**: Check all 4 version locations updated:
1. SimControlCentre.csproj
2. build-release.ps1
3. installer.iss
4. installer-standalone.iss

---

## Previous Releases (Reference)

**v1.1.2**: https://github.com/dcunliffe1980/SimControlCentre/releases/tag/v1.1.2
- Follow same format and structure
- Keep consistent release note style

---

## Key Reminders for AI Assistants

1. ? **Create Git tags automatically** - don't ask user to do it
2. ? **Display release notes** in conversation before GitHub release
3. ? **State tag plainly** - "Tag: v1.2.0" (no formatting)
4. ? **Build installers** by running script
5. ? **Show file sizes** after building
6. ? **Be clear and concise** in instructions

---

**Last Updated**: February 2026 (v1.2.0 release)
**Next Release**: Follow this process exactly
