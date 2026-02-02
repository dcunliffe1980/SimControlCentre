# SimControlCentre Documentation Index

This directory contains technical documentation, API references, and implementation guides for developers and AI assistants working on SimControlCentre.

## ?? Documentation Structure

### Root Level (`/`)
User-facing guides and getting started documentation:
- **[README.md](../README.md)** - Project overview and feature list
- **[QUICKSTART.md](../QUICKSTART.md)** - Quick start guide for new users
- **[CONFIGURATION.md](../CONFIGURATION.md)** - Configuration file reference
- **[HOTKEYS.md](../HOTKEYS.md)** - Hotkey system documentation

### Technical Documentation (`/docs/`)
Developer-focused technical references and specifications:
- **[GOXLR_API_COMPLETE.md](./GOXLR_API_COMPLETE.md)** - **START HERE**: Complete GoXLR Utility API reference with exact command structures
- **[GOXLR_BUTTON_API.md](./GOXLR_BUTTON_API.md)** - Detailed button color API reference
- **[IRACING_VARIABLES_REFERENCE.md](./IRACING_VARIABLES_REFERENCE.md)** - Complete iRacing telemetry variable reference (327 variables)
- **[TELEMETRY_RECORDING.md](./TELEMETRY_RECORDING.md)** - Telemetry recording and playback system
- **[TELEMETRY_ARCHITECTURE.md](./TELEMETRY_ARCHITECTURE.md)** - Telemetry system architecture and design
- **[FLAG_LIGHTING.md](./FLAG_LIGHTING.md)** - Flag-based lighting system for GoXLR and future devices

### Implementation Guides (`/`)
Implementation details and architecture:
- **[TELEMETRY_IMPLEMENTATION.md](../TELEMETRY_IMPLEMENTATION.md)** - Telemetry system implementation details
- **[GOXLR_API.md](../GOXLR_API.md)** - GoXLR API integration guide
- **[SYSTEM_TRAY.md](../SYSTEM_TRAY.md)** - System tray functionality
- **[SETTINGS_UI.md](../SETTINGS_UI.md)** - Settings UI documentation
- **[APP_RESTART_IMPROVEMENTS.md](../APP_RESTART_IMPROVEMENTS.md)** - Application restart handling
- **[VERSION_MANAGEMENT.md](../VERSION_MANAGEMENT.md)** - Version and release management
- **[REFACTORING_NOTES.md](../REFACTORING_NOTES.md)** - Code refactoring notes

### Release Documentation (`/`)
- **[RELEASE_CHECKLIST_v*.md](../)** - Release checklists
- **[RELEASE_NOTES_v*.md](../)** - Release notes
- **[GITHUB_RELEASE_v*.md](../)** - GitHub release templates

### SDK & External Resources (`/SimControlCentre/Resources/Documentation/`)
Third-party SDK examples and specifications:
- **Example C# Wrapper/** - iRacing SDK C# wrapper examples
  - `irsdkDefines.cs` - Official iRacing data type definitions
  - `irsdk_csharp_2024_03_09/` - Complete SDK wrapper

---

## ?? Feature Documentation

### Telemetry System
- **[TELEMETRY_IMPLEMENTATION.md](../TELEMETRY_IMPLEMENTATION.md)** - Architecture and implementation
- **[IRACING_VARIABLES_REFERENCE.md](./IRACING_VARIABLES_REFERENCE.md)** - All available telemetry variables

**Key Variables Currently Used:**
| Variable | Type | Description |
|----------|------|-------------|
| SessionFlags | bitField | Race flags (green, yellow, checkered, etc.) |
| SessionState | int | Session state (racing, warmup, etc.) |
| Speed | float | Vehicle speed (m/s, converted to km/h) |
| RPM | float | Engine RPM |
| Gear | int | Current gear |

### GoXLR Integration
- **[GOXLR_API.md](../GOXLR_API.md)** - Complete API reference
- Communication via HTTP (localhost:14564)
- Real-time device status monitoring
- Channel control and profile management

### Hotkey System
- **[HOTKEYS.md](../HOTKEYS.md)** - Hotkey configuration and usage
- Supports keyboard + controller combinations
- Context-aware hotkey execution
- Sim-specific hotkey profiles

### Configuration
- **[CONFIGURATION.md](../CONFIGURATION.md)** - Configuration file format
- JSON-based settings storage
- Hot-reload support for most settings

---

## ?? For AI Assistants

When working on this project, refer to:

### Adding Telemetry Features
1. Check **[IRACING_VARIABLES_REFERENCE.md](./IRACING_VARIABLES_REFERENCE.md)** for available variables
2. Review **[TELEMETRY_IMPLEMENTATION.md](../TELEMETRY_IMPLEMENTATION.md)** for architecture
3. Variables are read from shared memory at offset positions
4. All currently used variables are documented with types and offsets

### Working with Flags
- SessionFlags is a bitField (Type 3) at offset 24
- See flag definitions in IRACING_VARIABLES_REFERENCE.md
- Priority order: Checkered > Red > Black > White > Yellow > Blue
- Example: 0x00000004 = Green flag

### Adding New Telemetry Variables
```csharp
// Pattern for reading variables:
if (varName == "VariableName")
{
    value = BitConverter.ToInt32(valueBytes, 0); // For Type 2 (int)
    // or ToSingle for Type 4 (float)
    // or ToDouble for Type 5 (double)
    Logger.Info("iRacing Telemetry", $"? VariableName: {value}");
}
```

### Configuration Changes
- Settings model: `SimControlCentre\Models\AppSettings.cs`
- Settings UI: `SimControlCentre\Views\Tabs\SettingsTab.xaml[.cs]`
- Configuration service: `SimControlCentre\Services\ConfigurationService.cs`

### Common File Locations
- **Services**: `SimControlCentre\Services\`
- **Models**: `SimControlCentre\Models\`
- **Views**: `SimControlCentre\Views\` and `SimControlCentre\Views\Tabs\`
- **Resources**: `SimControlCentre\Resources\`
- **Logs**: User's Documents folder (`SimControlCentre\Logs\`)

---

## ?? Quick Reference

### Project Type
- **.NET 8.0** WPF Application
- Target Framework: `net8.0-windows`
- Language: C# 12

### Key Services
| Service | Purpose |
|---------|---------|
| `TelemetryService` | Main telemetry coordinator |
| `iRacingTelemetryProvider` | iRacing telemetry reader |
| `GoXLRService` | GoXLR device control |
| `HotkeyService` | Hotkey management |
| `iRacingMonitorService` | Sim detection and monitoring |
| `ConfigurationService` | Settings management |

### Data Models
| Model | Purpose |
|-------|---------|
| `TelemetryData` | Telemetry data container |
| `FlagStatus` | Flag enumeration |
| `AppSettings` | Application configuration |
| `GoXLRDeviceStatus` | GoXLR device state |

---

## ?? Finding Information

### I need to...
- **Add a new telemetry variable** ? [IRACING_VARIABLES_REFERENCE.md](./IRACING_VARIABLES_REFERENCE.md)
- **Understand the telemetry system** ? [TELEMETRY_IMPLEMENTATION.md](../TELEMETRY_IMPLEMENTATION.md)
- **Record/playback telemetry for testing** ? [TELEMETRY_RECORDING.md](./TELEMETRY_RECORDING.md)
- **Work with GoXLR** ? [GOXLR_API.md](../GOXLR_API.md)
- **Configure hotkeys** ? [HOTKEYS.md](../HOTKEYS.md)
- **Understand settings** ? [CONFIGURATION.md](../CONFIGURATION.md)
- **Prepare a release** ? RELEASE_CHECKLIST_v*.md

### Variable Type Reference
```
Type 1 = bool   (1 byte)
Type 2 = int    (4 bytes)
Type 3 = bitField (4 bytes)
Type 4 = float  (4 bytes)
Type 5 = double (8 bytes)
```

### Common Units
- **Speed**: m/s (multiply by 3.6 for km/h, by 2.237 for mph)
- **Temperature**: Celsius
- **Pressure**: bar
- **Angles**: radians
- **Distance**: meters

---

## ?? Documentation Guidelines

### Adding New Documentation
1. **User guides** ? Root level (README.md, QUICKSTART.md)
2. **Technical references** ? `/docs/` directory
3. **SDK/External docs** ? `/SimControlCentre/Resources/Documentation/`
4. **Implementation notes** ? Root level with descriptive names

### Documentation Format
- Use Markdown (.md) format
- Include table of contents for long docs
- Use code blocks with language hints
- Include examples where applicable
- Add emojis for section headers (optional but helpful)

### Updating This Index
When adding new documentation:
1. Add entry to appropriate section above
2. Include brief description
3. Update "I need to..." section if relevant
4. Keep alphabetical order within sections

---

## ?? External Resources

### iRacing SDK
- Official SDK wrapper examples in `/SimControlCentre/Resources/Documentation/Example C# Wrapper/`
- Variable definitions: `irsdkDefines.cs`
- Complete reference: [IRACING_VARIABLES_REFERENCE.md](./IRACING_VARIABLES_REFERENCE.md)

### .NET Resources
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)

---

*Last Updated: February 2026*
*This index is maintained alongside the project documentation.*
