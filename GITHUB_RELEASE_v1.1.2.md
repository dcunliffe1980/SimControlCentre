# v1.1.2 - Enhanced Auto-Update System ??

## ? Highlights

### Smart Auto-Updates
- ? **Automatic update checking** on startup (non-blocking)
- ? **Live status display** in About tab
- ? **Smart download button** - automatically detects your installation type
- ? Downloads the correct installer for you (standalone vs. regular)

### Better User Experience
- Fixed startup hanging when update check fails
- Faster timeout handling (15s max)
- Clear status messages throughout the update process
- Comprehensive diagnostic logging

## ?? Downloads

Choose the version that matches your installation:

### Regular Version (2.4 MB)
**SimControlCentre-Setup-v1.1.2.exe**
- Requires .NET 8 Runtime
- Smaller download
- Recommended if you already have .NET 8

### Standalone Version (64 MB)  
**SimControlCentre-Setup-Standalone-v1.1.2.exe**
- Includes .NET 8 Runtime
- No dependencies required
- Works on any Windows 10/11 system

## ?? Upgrading

Simply run the new installer - it will detect and update your existing installation automatically!

## ?? Full Details

See [RELEASE_NOTES_v1.1.2.md](RELEASE_NOTES_v1.1.2.md) for complete changelog and technical details.

## ?? Bug Fixes
- Fixed app startup hanging when GitHub API is unreachable
- Fixed misleading update check status messages
- Improved timeout handling

## ?? Technical Changes
- New `UpdateCheckService` with event-driven architecture
- Smart version detection (standalone vs. framework-dependent)
- Enhanced GitHub API integration with asset parsing
- Comprehensive diagnostic logging

---

**Important**: This is the first version with smart auto-updates. The next time an update is available, the app will automatically detect which version to download for you!
