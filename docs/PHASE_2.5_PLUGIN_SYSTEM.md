# Phase 2.5: True Plugin System Implementation

## Session Overview
**Date**: Current Session  
**Goal**: Implement a proper plugin architecture where plugins are drop-in DLLs

---

## Completed: Part 1 - Foundation

### Created SimControlCentre.Contracts Project

A shared class library that defines the plugin interfaces. Both the main app and plugins reference this.

**Files Created:**
- `IPlugin.cs` - Base interface for all plugins
- `IPluginContext.cs` - Provides plugins access to app services (settings, logging, etc.)
- `ILightingPlugin.cs` - Interface for lighting/RGB control plugins
- `IDeviceControlPlugin.cs` - Interface for device control plugins

**Key Decisions:**
1. **Single Settings File**: All plugins share `AppSettings` via `IPluginContext`
2. **Keep in Main Repo**: GoXLR plugin stays in the main repository as base package
3. **Shared Contracts**: Interfaces in separate project for clean dependency management

---

## Next Steps: Part 2-4

### Part 2: Create GoXLR Plugin Project
1. Create `SimControlCentre.Plugins.GoXLR` class library
2. Reference `SimControlCentre.Contracts`
3. Move existing GoXLR code:
   - `GoXLRLightingPlugin` ? Plugin project
   - `GoXLRDeviceControlPlugin` ? Plugin project
   - `GoXLRService`, `GoXLRApiClient`, etc. ? Plugin project
4. Create GoXLR-specific UserControls for configuration UI

### Part 3: Implement Plugin Loader
1. Create `PluginLoader` service in main app
2. Scan `%LocalAppData%\SimControlCentre\Plugins` folder
3. Dynamically load plugin DLLs using reflection
4. Register plugins with appropriate services

### Part 4: Update Main App
1. Reference `SimControlCentre.Contracts`
2. Remove hardcoded GoXLR references
3. Update `LightingTab` to embed plugin configuration controls
4. Update `Device Control Tab` to embed plugin configuration controls
5. Remove GoXLR-specific code from main app

### Part 5: Build Configuration
1. Add post-build event to copy GoXLR plugin DLL to Plugins folder
2. Update release build to include Plugins folder
3. Test hot-loading (add/remove plugin DLLs)

---

## Project Structure (Target)

```
SimControlCentre.sln
??? SimControlCentre (Main WPF App)
?   ??? No GoXLR-specific code
?   ??? Dynamic plugin loading
?
??? SimControlCentre.Contracts (Interfaces)
?   ??? IPlugin, IPluginContext
?   ??? ILightingPlugin, IDeviceControlPlugin
?   ??? Shared by app and all plugins
?
??? SimControlCentre.Plugins.GoXLR (GoXLR Plugin)
    ??? GoXLRLightingPlugin
    ??? GoXLRDeviceControlPlugin
    ??? GoXLR services and models
    ??? GoXLR-specific UI controls
```

**Output:**
```
bin\Debug\net8.0-windows\
??? SimControlCentre.exe
??? SimControlCentre.Contracts.dll
??? Plugins\
    ??? SimControlCentre.Plugins.GoXLR.dll
```

---

## Benefits Achieved

1. **True Plugin Architecture**: Plugins are separate DLLs, not compiled into main app
2. **3rd Party Support**: Anyone can create a plugin DLL following the contracts
3. **Clean Separation**: Main app has zero GoXLR-specific code
4. **Hot-Loadable**: Add/remove plugins without recompiling app
5. **Smaller Core**: Main app is leaner, plugins add functionality
6. **Maintainable**: Plugin code isolated in its own project

---

## Status

**Phase 2.5 Progress**: 20% Complete (Part 1/5)

**Commit**: `c9a36f7` - Phase 2.5: Plugin System Foundation - Part 1

**Next Session**: Continue with Part 2 (Create GoXLR Plugin Project)
