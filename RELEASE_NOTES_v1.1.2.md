# SimControlCentre v1.1.2 Release Notes

## ?? What's New

### Enhanced Update System
- **Automatic Update Checking**: App now checks for updates automatically on startup
- **Live Status Display**: About tab shows real-time update check status
  - "Checking for updates..." while checking
  - "You are running the latest version!" when up to date
  - "Update available: vX.X.X" when new version found
- **Smart Download Button**: Automatically detects your installation type and downloads the correct installer
  - Standalone users get the standalone installer
  - Regular users get the standard installer
  - Button appears automatically when update is available

### Update Check Improvements
- Non-blocking startup: App shows immediately while checking in background
- 15-second timeout prevents hanging on network issues
- Comprehensive diagnostic logging for troubleshooting
- Graceful error handling with user-friendly messages

### UI Cleanup
- Removed debug "Test GitHub API" button
- Cleaner About section with better status messaging

## ?? Technical Changes

### New Services
- `UpdateCheckService`: Manages update check state with event-driven status updates
- Subscribable status change events for real-time UI updates

### Enhanced Models
- `UpdateInfo` now includes release assets list
- `ReleaseAsset` model with download URLs and file names

### Smart Version Detection
- Automatic detection of standalone vs. framework-dependent installation
- Intelligent asset matching based on installation type
- Fallback to GitHub releases page if automatic detection fails

### Improved Error Handling
- Network timeout protection
- Retry logic with configurable timeouts
- Detailed logging to `%LocalAppData%\SimControlCentre\logs\update_checker_*.log`

## ?? Installation

### Two Versions Available:

**SimControlCentre-Setup-v1.1.2.exe** (2.4 MB)
- Requires .NET 8 Runtime to be installed
- Smaller download
- Updates with Windows Update

**SimControlCentre-Setup-Standalone-v1.1.2.exe** (64 MB)
- Includes .NET 8 Runtime
- No dependencies
- Works on any Windows 10/11 system

## ?? Upgrade Instructions

### From v1.1.1 or earlier:
1. Download the installer that matches your current installation:
   - If you installed the standalone version before, download standalone
   - If you installed the regular version, download regular
2. Run the installer
3. Choose "Repair" or "Reinstall" when prompted
4. Launch SimControlCentre

The app will detect your installation type automatically for future updates!

## ?? Bug Fixes
- Fixed app hanging on startup when update check failed
- Fixed misleading "Checking for updates..." message when no check was running
- Reduced HTTP timeouts to fail faster on network issues (5s from 10s)

## ?? Configuration

Update checking is enabled by default. To disable:
1. Open Settings ? General
2. Uncheck "Check for updates on startup"
3. You can still manually check via Settings ? About ? "Check for Updates"

## ?? Troubleshooting

If update checking isn't working:
1. Check logs in `%LocalAppData%\SimControlCentre\logs\update_checker_*.log`
2. Ensure your firewall allows HTTPS to github.com
3. Try manually checking via Settings ? About
4. Disable and re-enable update checking in settings

## ?? Technical Details

### System Requirements
- Windows 10/11 (64-bit)
- .NET 8 Runtime (if using regular installer)
- Internet connection (for GoXLR control and update checking)

### Update Check Behavior
- Checks on startup after 5-second delay
- 15-second hard timeout
- Uses GitHub Releases API
- Caches result until app restart
- Works independently of GoXLR connection

## ?? Acknowledgments

Thank you to all users who reported the update check hanging issue and provided feedback on the update system!

## ?? Support

- GitHub Issues: https://github.com/dcunliffe1980/SimControlCentre/issues
- Documentation: See README.md and other docs in repository

---

**Full Changelog**: https://github.com/dcunliffe1980/SimControlCentre/compare/v1.1.1...v1.1.2
