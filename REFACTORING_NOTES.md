# REFACTORING NOTES - MainWindow Modularization
# ================================================

## What We're Doing:
Split the monolithic MainWindow.xaml/.cs (~1700 lines) into modular UserControls for each tab.

## Architecture:
```
SimControlCentre/Views/Tabs/
??? GeneralTab.xaml / .cs           ? COMPLETE
??? HotkeysTab.xaml / .cs           TODO
??? ChannelsProfilesTab.xaml / .cs  TODO
??? ControllersTab.xaml / .cs       TODO
??? AboutTab.xaml / .cs             TODO
```

## Status:
- ? GeneralTab created and functional
- ? Need to update MainWindow.xaml to use GeneralTab
- ? Need to update MainWindow.xaml.cs constructor
- ? Create remaining tabs

## Next Steps:

### 1. Update MainWindow.xaml
Replace the General TabItem content with:
```xaml
<TabItem Header="General">
    <tabs:GeneralTab x:Name="GeneralTabControl"/>
</TabItem>
```

### 2. Update MainWindow.xaml.cs Constructor
After InitializeComponent(), add:
```csharp
// Initialize GeneralTab
GeneralTabControl.DataContext = this;
// Pass services to GeneralTab (it's already created in constructor)
```

Actually, GeneralTab constructor needs services, so we need to create it in code-behind:
```csharp
// In MainWindow constructor, after InitializeComponent():
var generalTab = new Views.Tabs.GeneralTab(_goXLRService, _configService, _settings);
// Replace the XAML-created one or create in code

```

### 3. Cleaner Approach - Create Tabs in Code-Behind

Actually, the cleanest approach is to create tabs programmatically in MainWindow constructor:

```csharp
public MainWindow(ConfigurationService configService, AppSettings settings, GoXLRService goXLRService)
{
    InitializeComponent();
    
    _configService = configService;
    _settings = settings;
    _goXLRService = goXLRService;
    
    // Initialize tab controls
    var generalTab = new Views.Tabs.GeneralTab(_goXLRService, _configService, _settings);
    GeneralTabItem.Content = generalTab;
    
    // TODO: Initialize other tabs
    // HotkeysTabItem.Content = new Views.Tabs.HotkeysTab(...);
    // etc.
    
    RestoreWindowSettings();
    // ... rest of initialization
}
```

And in XAML, just use empty TabItems with x:Name:
```xaml
<TabItem Header="General" x:Name="GeneralTabItem"/>
<TabItem Header="Hotkeys" x:Name="HotkeysTabItem"/>
etc.
```

## Benefits of This Approach:
1. Clean separation of concerns
2. Each tab is self-contained
3. Easy to add/remove features
4. Testable in isolation
5. No god-object MainWindow

## Current Issue:
The full refactor is complex. Recommend:
1. Commit GeneralTab creation
2. Test that it builds
3. Gradually migrate other tabs
4. Or complete all tabs manually following the GeneralTab pattern

## To Complete Manually:
For each remaining tab:
1. Create TabName.xaml with UserControl root
2. Copy relevant XAML content from MainWindow
3. Create TabName.xaml.cs with constructor taking services
4. Copy relevant methods from MainWindow.xaml.cs
5. Update MainWindow to create and host the tab
6. Remove old code from MainWindow
7. Build and test

This is the safest approach for such a large refactor.
