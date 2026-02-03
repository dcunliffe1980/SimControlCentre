# Release Checklist v1.3.0

## ? Pre-Release (Completed)

- [x] Version updated in `SimControlCentre.csproj` (1.3.0)
- [x] Version updated in `installer.iss` (1.3.0)
- [x] Version updated in `installer-standalone.iss` (1.3.0)
- [x] Version updated in `build-release.ps1` header (1.3.0)
- [x] All features tested and working
- [x] Documentation updated
  - [x] RELEASE_NOTES_v1.3.0.md
  - [x] GITHUB_RELEASE_v1.3.0.md
  - [x] docs/BUILD_AND_RELEASE.md
  - [x] docs/UI_DESIGN_GUIDELINES.md
  - [x] docs/SESSION_GOXLR_BUTTON_CAPTURE.md
- [x] Build script updated to include plugins
- [x] Installer scripts updated to include Plugins folder
- [x] All changes committed to master
- [x] Git tag created: `v1.3.0`
- [x] Tag pushed to GitHub
- [x] Release installers built successfully
  - [x] SimControlCentre-Setup-v1.3.0.exe (2.36 MB)
  - [x] SimControlCentre-Setup-Standalone-v1.3.0.exe (46.3 MB)
- [x] Plugins verified in installers

## ?? Release Steps (Do Now)

### 1. Verify Installers
- [ ] Check both installers exist in `Installers\` folder
- [ ] Verify file sizes are reasonable (2-3 MB and 45-50 MB)
- [ ] Optionally: Test on clean VM or machine

### 2. Create GitHub Release
1. [ ] Go to https://github.com/dcunliffe1980/SimControlCentre/releases
2. [ ] Click "Draft a new release"
3. [ ] Select tag: `v1.3.0`
4. [ ] Release title: `SimControlCentre v1.3.0`
5. [ ] Copy release notes from `GITHUB_RELEASE_v1.3.0.md`
6. [ ] Upload installers:
   - [ ] `Installers\SimControlCentre-Setup-v1.3.0.exe`
   - [ ] `Installers\SimControlCentre-Setup-Standalone-v1.3.0.exe`
7. [ ] Check "Set as the latest release"
8. [ ] Click "Publish release"

### 3. Post-Release
- [ ] Verify release appears on GitHub
- [ ] Test download links work
- [ ] Update main README.md with v1.3.0 badge/link (optional)
- [ ] Announce on social media/forums (optional)

## ?? What This Release Includes

### Major Features
? **Timeline Scrubber** - Jump to any point in telemetry recordings with visual flag markers
? **Flag Lighting Fixed** - Debouncing, profile restore, session end handling
? **GoXLR Plugin Complete** - Button capture, all channels, duplicate detection

### Bug Fixes
?? LEDs staying lit after flag clears
?? Clear Flag button not working  
?? Telemetry glitches causing flickering
?? Profile colors not restoring

### Technical
?? Plugins now included in installers
?? ILightingDevice interface enhanced
?? Profile reload API for GoXLR
?? Bidirectional type conversion

## ?? Testing Checklist (Optional)

If you want to test before release:

### Framework-Dependent Installer
- [ ] Install on machine with .NET 8 Runtime
- [ ] Verify app launches
- [ ] Check Plugins tab shows GoXLR plugin enabled
- [ ] Test flag lighting (Lighting tab ? Test Flag buttons)
- [ ] Test device control (Device Control tab ? add channel)
- [ ] Test telemetry timeline (Telemetry Debug ? select recording)

### Self-Contained Installer  
- [ ] Install on clean machine WITHOUT .NET 8
- [ ] Verify app launches (shouldn't need .NET install)
- [ ] Same tests as above

## ?? Release Metrics

**Version:** 1.3.0
**Date:** January 2025
**Tag:** v1.3.0
**Commits Since Last Release:** ~15
**Files Changed:** ~25
**Major Features:** 3
**Bug Fixes:** 4

## ?? Known Issues

None identified for this release.

## ?? If Rollback Needed

If critical bug found after release:

1. Unpublish GitHub release
2. Delete tag locally and remote
3. Fix issue
4. Create v1.3.1 hotfix
5. Document in release notes

## ? Release Complete

Once published:
- [ ] Mark this checklist as complete
- [ ] Archive release documentation
- [ ] Start planning v1.4.0 features
- [ ] Monitor GitHub issues for feedback

---

**Release Manager:** You
**Build Date:** $(Get-Date)
**Build Machine:** Local development
**Status:** Ready for publish ??
