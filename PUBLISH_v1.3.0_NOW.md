# ?? SimControlCentre v1.3.0 Release - Ready to Publish!

## ? Everything Complete!

All code, builds, and documentation are ready. The release just needs to be published on GitHub.

---

## ?? What You Have

### Installers (Ready in `Installers\` folder)
- ? `SimControlCentre-Setup-v1.3.0.exe` (2.36 MB) - Requires .NET 8 Runtime
- ? `SimControlCentre-Setup-Standalone-v1.3.0.exe` (46.3 MB) - Fully self-contained

**Both installers include the GoXLR plugin!**

### Git Repository
- ? All code committed to master
- ? Tag `v1.3.0` created and pushed
- ? All documentation updated

### Documentation
- ? `GITHUB_RELEASE_v1.3.0.md` - Copy this for GitHub release description
- ? `RELEASE_NOTES_v1.3.0.md` - Detailed release notes
- ? `RELEASE_CHECKLIST_v1.3.0.md` - Step-by-step checklist
- ? `docs/BUILD_AND_RELEASE.md` - Build documentation for future releases

---

## ?? Next Steps: Publish the Release

### Step 1: Go to GitHub Releases
Open: https://github.com/dcunliffe1980/SimControlCentre/releases

### Step 2: Draft New Release
1. Click **"Draft a new release"**
2. **Tag:** Select `v1.3.0` from dropdown
3. **Release title:** `SimControlCentre v1.3.0`
4. **Description:** Copy entire content from `GITHUB_RELEASE_v1.3.0.md`

### Step 3: Upload Installers
Drag and drop or click to upload:
- `Installers\SimControlCentre-Setup-v1.3.0.exe`
- `Installers\SimControlCentre-Setup-Standalone-v1.3.0.exe`

### Step 4: Publish
1. ? Check "Set as the latest release"
2. Click **"Publish release"**

**That's it!** ??

---

## ?? What's in This Release

### ?? Major Features

#### Timeline Scrubber for Telemetry Playback
- Visual timeline with scrubber slider
- Color-coded flag change markers
- Hover tooltips showing flag transitions
- Click to seek to any position
- Perfect for testing flag lighting

#### Flag Lighting Improvements
- Debounced None flags (500ms threshold) - no more flickering!
- Profile restore when flags clear
- Profile restore when telemetry disconnects
- Clear Flag button now works correctly

#### GoXLR Plugin Complete
- All GoXLR channels available (Mic, LineIn, Console, System, Game, Chat, Music, etc.)
- Button capture working (keyboard and controller)
- Inline duplicate detection (no popups!)
- Classic horizontal layout restored
- Clean UI - consistent spacing, no emojis

### ?? Bug Fixes
- Fixed LEDs staying lit after flag clears
- Fixed Clear Flag button not restoring profile
- Fixed telemetry glitches causing LED flickering
- Fixed button capture delegate issues
- Fixed plugin settings type conversion
- **Fixed plugins not included in installers** (critical!)

### ?? Technical Improvements
- ILightingDevice interface enhanced with SaveStateAsync/RestoreStateAsync
- Profile reload API for GoXLR
- Debouncing system with 500ms threshold
- Bidirectional type conversion for settings
- Timeline rendering with responsive canvas
- Seek functionality in TelemetryPlaybackProvider

---

## ?? Release Statistics

- **Version:** 1.3.0
- **Release Date:** January 2025
- **Commits:** ~15 since v1.2.0
- **Files Changed:** ~30
- **Major Features:** 3
- **Bug Fixes:** 6
- **Documentation Pages:** 8

---

## ?? Testing (Optional)

If you want to test before publishing:

### Quick Test on Your Machine
1. Run `SimControlCentre-Setup-v1.3.0.exe`
2. Launch app
3. Check **Plugins** tab - GoXLR plugin should show as "Enabled"
4. Test **Lighting** tab - flag buttons should work
5. Test **Device Control** tab - add channel, capture button
6. Test **Telemetry Debug** tab - timeline scrubber with recording

### Full Test on Clean VM (Recommended but optional)
- Install `SimControlCentre-Setup-Standalone-v1.3.0.exe` on machine without .NET 8
- Verify app launches and all features work
- Confirms no dependency issues

---

## ?? What Changed Since v1.2.0

### User-Facing
- ? **NEW:** Timeline scrubber in Telemetry Debug tab
- ? **NEW:** Flag markers showing all flag changes
- ? **IMPROVED:** Flag lighting now properly clears and restores
- ? **IMPROVED:** GoXLR plugin now included in installers
- ? **FIXED:** Clear Flag button works
- ? **FIXED:** No more LED flickering from brief None flags
- ? **FIXED:** Profile colors restore after session ends

### Developer-Facing
- ?? Build script now builds and includes plugins automatically
- ?? Installer scripts updated for plugin support
- ?? Self-contained build changed to support plugins
- ?? Documentation significantly expanded
- ?? UI design guidelines documented

---

## ?? After Publishing

Once the release is published on GitHub:

1. ? Verify download links work
2. ? Test installing from downloaded EXE
3. ? Close any related GitHub issues
4. ? Announce on social media (optional)
5. ? Update main README.md with latest version (optional)
6. ? Start planning v1.4.0 features

---

## ?? If You Need Help

### Build Issues
- Check `docs/BUILD_AND_RELEASE.md` for troubleshooting
- Verify Inno Setup is installed
- Check logs in workspace root

### Release Issues  
- Follow `RELEASE_CHECKLIST_v1.3.0.md` step-by-step
- Ensure both installers are uploaded
- Verify tag is selected correctly

### Plugin Issues
- Plugins must be in `Plugins\` subfolder
- Check installers include `Plugins\SimControlCentre.Plugins.GoXLR.dll`
- Verify with: `7z l Installers\SimControlCentre-Setup-v1.3.0.exe`

---

## ?? Congratulations!

You've completed a major release with:
- ? New timeline scrubber feature
- ? Fixed flag lighting system
- ? Complete GoXLR plugin integration
- ? Comprehensive documentation
- ? Proper plugin inclusion in installers

**Ready to publish? Go to GitHub and draft the release!** ??

---

**Quick Link:** https://github.com/dcunliffe1980/SimControlCentre/releases/new?tag=v1.3.0

Just:
1. Copy text from `GITHUB_RELEASE_v1.3.0.md`
2. Upload the 2 installers
3. Click "Publish release"

**Done!** ??
