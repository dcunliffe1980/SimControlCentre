# Phase 2.5 Part 2 - Remaining Work

## Current Status: 70% Complete

We have successfully:
- ? Created Contracts project with interfaces
- ? Created GoXLR plugin project structure
- ? Moved all GoXLR files to plugin project (8 files)
- ? Updated namespaces
- ? Added LightingColor enum to Contracts
- ? Created IPluginSettings interface for settings access
- ? Updated IPluginContext to provide proper abstractions

## Remaining Compilation Errors: ~95

### Root Causes:

1. **Logger References** (~40 errors)
   - Plugin files call `Logger.Info()`, `Logger.Error()`, etc. directly
   - **Fix**: Replace all with `_context.LogInfo()`, `_context.LogError()`, etc.

2. **AppSettings References** (~30 errors)
   - GoXLRService, GoXLRLightingPlugin, GoXLRDeviceControlPlugin need AppSettings
   - **Fix**: Use `IPluginContext.Settings.GetValue<T>()` instead

3. **Interface Mismatches** (~15 errors)
   - GoXLRLightingDevice doesn't fully implement ILightingDevice
   - Missing: DeviceId, IsConnected properties
   - Missing: InitializeAsync, DisconnectAsync, SetColorAsync(string) methods
   - **Fix**: Add missing members

4. **LightingColor not imported** (~10 errors)
   - Files use `LightingColor` but don't import `SimControlCentre.Contracts`
   - **Fix**: Add `using SimControlCentre.Contracts;`

---

## Systematic Fix Plan

### Step 1: Fix GoXLRApiClient
**File**: `SimControlCentre.Plugins.GoXLR\Services\GoXLRApiClient.cs`

Replace all:
- `Logger.Info()` ? `_context.LogInfo()`
- `Logger.Error()` ? `_context.LogError()`  
- `Logger.Warning()` ? `_context.LogWarning()`
- `Logger.Debug()` ? `_context.LogDebug()`
- `Console.WriteLine()` ? `_context.LogInfo()` (for consistency)

**Lines affected**: ~40 Logger references

---

### Step 2: Fix GoXLRService
**File**: `SimControlCentre.Plugins.GoXLR\Services\GoXLRService.cs`

Already started - needs:
- Replace remaining `_settings.General.X` with `_context.Settings.GetValue<T>("GoXLR.X")`
- Replace all Logger calls with _context methods
- Update auto-detect serial logic
- Update Reinitialize() method

**Lines affected**: ~30 references

---

### Step 3: Fix GoXLRDiagnostics
**File**: `SimControlCentre.Plugins.GoXLR\Services\GoXLRDiagnostics.cs`

This is a static utility class - needs refactoring:
- Make it instance-based instead of static
- Accept IPluginContext in constructor
- Use _context for logging and file paths

**Alternative**: Keep it static but make it write to a plugin-provided path

---

### Step 4: Fix GoXLRLightingDevice
**File**: `SimControlCentre.Plugins.GoXLR\Services\GoXLRLightingDevice.cs`

Changes needed:
1. Remove AppSettings dependency (already not used much)
2. Add missing interface members:
   ```csharp
   public string DeviceId => "goxlr";
   public bool IsConnected => _goXLRService.IsConnected;
   public Task<bool> InitializeAsync() => Task.FromResult(true);
   public Task DisconnectAsync() => Task.CompletedTask;
   public Task SetColorAsync(string hexColor) => SetColorAsync(ParseHex(hexColor));
   ```
3. Add ParseHex helper method
4. Replace all `Logger.X()` with `_context.LogX()`

**Lines affected**: ~35 Logger references

---

### Step 5: Fix GoXLRLightingPlugin
**File**: `SimControlCentre.Plugins.GoXLR\Services\GoXLRLightingPlugin.cs`

Changes needed:
1. Remove AppSettings dependency from constructor
2. Store IPluginContext from Initialize() method
3. Use `_context.Settings.GetValue<T>()` for button selection
4. Update CreateDevice() to pass IPluginContext:
   ```csharp
   return new GoXLRLightingDevice(_goXLRService, _context, _selectedButtons);
   ```
5. Replace Logger calls

---

### Step 6: Fix GoXLRDeviceControlPlugin
**File**: `SimControlCentre.Plugins.GoXLR\Services\GoXLRDeviceControlPlugin.cs`

Changes needed:
1. Remove AppSettings from constructor
2. Use IPluginContext.Settings for any settings access
3. Already has Initialize() method - ensure it stores context
4. No Logger calls in this file (good!)

---

## Time Estimate

- **Step 1** (GoXLRApiClient): 15 minutes (40 replacements)
- **Step 2** (GoXLRService): 15 minutes (30 replacements)
- **Step 3** (GoXLRDiagnostics): 5 minutes (simple refactor)
- **Step 4** (GoXLRLightingDevice): 20 minutes (35 replacements + add methods)
- **Step 5** (GoXLRLightingPlugin): 10 minutes
- **Step 6** (GoXLRDeviceControlPlugin): 5 minutes

**Total**: ~70 minutes of focused work

---

## After Fixing Plugin Project

Once plugin compiles, we still need to:

### Part 3: Create PluginLoader in Main App
- Scan Plugins folder
- Load DLLs dynamically
- Create PluginContext implementation
- Register plugins with services

### Part 4: Update Main App
- Remove old GoXLR files
- Update references to use loaded plugins
- Implement IPluginSettings wrapper around AppSettings
- Test everything

### Part 5: Build Configuration
- Post-build event to copy plugin DLL
- Test hot-loading

---

## Current Commits

- `c9a36f7` - Contracts project
- `6242ea1` - Plugin project structure  
- `033fe15` - Moved GoXLR code
- `1125546` - Docs update
- `ac33b94` - LightingColor + removed circular dependency
- `704eef6` - IPluginSettings + refactoring started (WIP)

---

## Decision Point

**Option A**: Continue systematic fixes (70 minutes)
**Option B**: Stop here, resume next session
**Option C**: Simplified approach - keep old architecture, add PluginLoader later

**Recommendation**: Option A if you have time, otherwise Option B
