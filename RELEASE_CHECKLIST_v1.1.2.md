# SimControlCentre v1.1.2 Release Checklist

## ? Pre-Release Tasks

### Version Updates
? **Read VERSION_MANAGEMENT.md for detailed version update process**

- [x] Updated version in `SimControlCentre.csproj` to 1.1.2
- [x] Updated version in `installer.iss` to 1.1.2
- [x] Updated version in `installer-standalone.iss` to 1.1.2
- [x] Updated version in `build-release.ps1` header to 1.1.2
- [x] ~~Updated version in `AboutTab.xaml`~~ - Now reads from assembly automatically
- [x] ~~Updated fallback version in `UpdateService.cs`~~ - Now reads from assembly automatically

### Build Verification
- [x] Project builds successfully
- [x] No compilation errors
- [x] All tests pass (manual testing required)

### Installers Created
- [x] `SimControlCentre-Setup-v1.1.2.exe` (2.4 MB) - Regular version
- [x] `SimControlCentre-Setup-Standalone-v1.1.2.exe` (64 MB) - Standalone version

### Documentation
- [x] Created `RELEASE_NOTES_v1.1.2.md` with full details
- [x] Created `GITHUB_RELEASE_v1.1.2.md` for GitHub release description

## ?? Release Steps

### 1. Test Installers (Recommended)
- [ ] Test regular installer on clean machine with .NET 8
- [ ] Test standalone installer on clean machine without .NET 8
- [ ] Verify update detection works
- [ ] Verify download button appears and downloads correct version
- [ ] Test upgrade from v1.1.1

### 2. Create GitHub Release
- [ ] Go to https://github.com/dcunliffe1980/SimControlCentre/releases/new
- [ ] Tag: `v1.1.2`
- [ ] Release title: `v1.1.2 - Enhanced Auto-Update System`
- [ ] Copy content from `GITHUB_RELEASE_v1.1.2.md` as description
- [ ] Upload `SimControlCentre-Setup-v1.1.2.exe`
- [ ] Upload `SimControlCentre-Setup-Standalone-v1.1.2.exe`
- [ ] Mark as latest release
- [ ] Publish release

### 3. Post-Release Verification
- [ ] Verify release appears at https://github.com/dcunliffe1980/SimControlCentre/releases/latest
- [ ] Test that v1.1.1 detects the new update
- [ ] Verify download button downloads correct installer
- [ ] Check that update check doesn't hang on startup

### 4. Announce (Optional)
- [ ] Update README.md badges if needed
- [ ] Post to any relevant forums/communities
- [ ] Tweet/social media announcement

## ?? Release Files

### Location
```
C:\Users\david\source\repos\SimControlCentre\Installers\
```

### Files
```
SimControlCentre-Setup-v1.1.2.exe                (2,426,687 bytes / 2.31 MB)
SimControlCentre-Setup-Standalone-v1.1.2.exe    (67,042,009 bytes / 63.94 MB)
```

## ?? Key Features to Highlight

1. **Automatic Update Checking**: No more manual checks required
2. **Smart Download**: Automatically selects correct installer type
3. **Live Status**: Real-time update check feedback
4. **Non-Blocking**: App starts immediately while checking in background
5. **Robust Error Handling**: Won't hang on network issues

## ?? Key Fixes to Mention

1. Fixed startup hanging when update check fails
2. Faster timeout handling (15s max)
3. Removed debug UI elements
4. Better error messages

## ?? Important Notes

- This is the first version with the smart download system
- Users on v1.1.1 will get update notification via the old popup system
- Future updates will use the new About tab download button
- Both installer types must be uploaded to GitHub for smart download to work

## ?? Git Commands

After testing and before creating GitHub release:

```bash
# Commit version updates
git add .
git commit -m "Release v1.1.2 - Enhanced auto-update system"
git tag -a v1.1.2 -m "Version 1.1.2"
git push origin master
git push origin v1.1.2
```

Then create the GitHub release with the uploaded installers.

---

**Last Updated**: 2025-01-02
**Status**: Ready for release
