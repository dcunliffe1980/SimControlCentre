# ?? Hotfix v1.3.1 - Update Checker Issue Fixed

## What Happened

You discovered a critical bug: **The update checker was downloading the standalone version for everyone**, regardless of which installer type they used.

## Root Cause

When we changed the build process to support plugins in v1.3.0:
- We changed standalone build from `PublishSingleFile=true` to `PublishSingleFile=false`
- This meant **BOTH** installer types now have `SimControlCentre.dll` present
- The old detection logic checked for this DLL to determine installer type
- **Result:** Always detected as standalone = always downloaded the 46 MB installer

## The Fix

**New detection method** checks for .NET Runtime DLLs instead:

### Framework-Dependent (2.36 MB)
- ? Does NOT include: `System.Private.CoreLib.dll`, `coreclr.dll`, `clrjit.dll`
- ? Uses system-installed .NET 8 Runtime

### Standalone (46.3 MB)
- ? INCLUDES: `System.Private.CoreLib.dll`, `coreclr.dll`, `clrjit.dll`, etc.
- ? Has its own bundled .NET 8 Runtime

**Detection logic:** If 2+ runtime DLLs are found ? Standalone

## Version History

- **v1.3.0** - Released with plugin support, introduced update checker bug
- **v1.3.1** - Hotfix for update checker detection

## What You Need to Do

### Option 1: Release v1.3.0 AS-IS (Recommended)

**Why:** The bug only affects future updates FROM v1.3.0, not the initial install.

**Action:**
1. Publish v1.3.0 release on GitHub (as planned)
2. Mention in release notes: "v1.3.1 hotfix coming soon"
3. Then release v1.3.1 shortly after

**Release Notes Addition for v1.3.0:**
```
**Known Issue:** Update checker may download wrong installer type. 
This will be fixed in v1.3.1 (available within 24 hours).
```

### Option 2: Skip v1.3.0, Go Straight to v1.3.1

**Why:** Ensures no one ever has the bug

**Action:**
1. Update all installers to v1.3.1 (re-run build script)
2. Update release notes to v1.3.1
3. Publish v1.3.1 as the main release

**Pros:** Clean, no known bugs
**Cons:** More work, delays release

## Recommendation: Option 1

**Reasoning:**
- v1.3.0 is fully functional for NEW installs
- The bug only affects FUTURE updates
- Users can manually download the correct installer anyway
- v1.3.1 can be released as a quick follow-up hotfix

## Testing the Fix

To verify the fix works:

1. **Framework-Dependent Install:**
   - Check update ? Should say "2.36 MB" installer
   - Logs should show: `[IsStandalone] Result: FRAMEWORK-DEPENDENT`

2. **Standalone Install:**
   - Check update ? Should say "46.3 MB" installer  
   - Logs should show: `[IsStandalone] Result: STANDALONE`

Logs are in: `%LOCALAPPDATA%\SimControlCentre\logs\`

## Files Changed

### v1.3.1 Changes:
- `SimControlCentre/SimControlCentre.csproj` - Version 1.3.1
- `SimControlCentre\Views\Tabs\SettingsTab.xaml.cs` - New `IsStandaloneVersion()` logic

### Git Status:
- ? Committed to master
- ? Tagged as v1.3.1
- ? Pushed to GitHub

## Next Steps

### If Going with Option 1 (Recommended):

1. **NOW:** Publish v1.3.0 release on GitHub
   - Use existing installers
   - Add known issue note to release notes
   - Complete as planned

2. **TOMORROW:** Build and publish v1.3.1 hotfix
   - Run `.\build-release.ps1` 
   - Update installer versions to 1.3.1
   - Create quick hotfix release

### If Going with Option 2:

1. **Update build script header** to v1.3.1
2. **Update installer scripts** to v1.3.1
3. **Rebuild installers:** `.\build-release.ps1`
4. **Update release notes** files to v1.3.1
5. **Publish v1.3.1** on GitHub

## Summary

? **Bug Fixed:** Update checker now correctly detects installer type
? **Code Committed:** Version 1.3.1 ready
? **Git Tagged:** v1.3.1 tag created and pushed

**Your call:** Release v1.3.0 now + v1.3.1 hotfix soon, OR skip to v1.3.1 directly.

**My recommendation:** Go with Option 1 - release v1.3.0 today with a note about the hotfix coming.
