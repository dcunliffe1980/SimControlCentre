# UI Design Guidelines

## Core Principles

### 1. Prefer Inline Feedback Over Popups

**? DO:**
- Show validation errors inline in the UI (e.g., red text in textbox)
- Use tooltips for helpful hints
- Display status messages in the UI itself
- Provide immediate visual feedback where the action occurred

**? AVOID:**
- Modal dialogs for validation errors
- Popup windows for routine feedback
- MessageBox for duplicate detection or validation failures

**Exceptions (Popups ARE appropriate for):**
- Critical errors that prevent operation
- Destructive actions requiring confirmation (e.g., "Delete all profiles?")
- Temporary debugging/diagnostic information
- Situations where logs should be checked first

**Example:**
```csharp
// GOOD: Inline error
textBox.Text = "Already assigned to: Chat Volume Down";
textBox.Foreground = System.Windows.Media.Brushes.Red;

// BAD: Popup error
MessageBox.Show("This button is already assigned!", "Error", ...);
```

### 2. Use Plain Text, Avoid Emojis

**? DO:**
- Use descriptive text: "Remove", "Delete", "Add", "Refresh"
- Use colors or icons for status indication if needed
- Keep button text simple and clear

**? AVOID:**
- Emoji in button text (?, ?, ??, etc.)
- Emoji in status messages
- Unicode symbols that may render inconsistently

**Example:**
```csharp
// GOOD
Content = "Remove"
Content = "Clear"
Content = "Refresh"

// BAD
Content = "? Remove"
Content = "?? Refresh"
```

### 3. Consistent Spacing and Layout

**Standard button spacing:**
- 5px margin between related buttons
- Use `Margin = new Thickness(5, 0, 5, 0)` for buttons in a row
- First button: `Margin(0, 0, 5, 0)` (5px right)
- Middle buttons: `Margin(0, 0, 5, 0)` (5px right)
- Last button: `Margin(5, 0, 0, 0)` (5px left)

**Button padding:**
- Standard: `Padding = new Thickness(8, 5, 8, 5)`
- Larger buttons: `Padding = new Thickness(10, 5, 10, 5)`

### 4. Error Handling

**Display Order:**
1. **Inline validation** - Show immediately in the UI
2. **Log file** - Always log errors with context
3. **User notification** - Only if user action required
4. **Popup** - Last resort, for critical issues only

**Error Message Format:**
- Be specific: "Already assigned to: Chat Volume Down"
- Not vague: "Invalid input"
- Include context: What failed and why
- Suggest solution if possible

### 5. Temporary UI States

**Capture/Loading states:**
- Show state in the control itself (textbox, not button)
- Use gray text for prompts: "Press key or button..."
- Restore normal color after capture
- Don't disable unrelated controls

**Example:**
```csharp
// GOOD: Show prompt in textbox
textBox.Text = "Press key or button...";
textBox.Foreground = System.Windows.Media.Brushes.Gray;

// BAD: Change button text
button.Content = "Press key or button...";
button.IsEnabled = false;
```

## Plugin UI Guidelines

When creating plugin UIs:

1. **Complete functionality** - Plugin provides ALL UI and logic for its features
2. **Consistent styling** - Match main app's visual design
3. **Self-contained** - No assumptions about main app internals
4. **Use IPluginContext** - For all cross-plugin communication
5. **Follow these guidelines** - Inline feedback, plain text, consistent spacing

## Testing Checklist

Before committing UI changes:

- [ ] No emojis in user-facing text
- [ ] No unnecessary popups (validation, duplicates, etc.)
- [ ] Errors shown inline with red text
- [ ] Consistent button spacing (5px)
- [ ] Temporary states use textbox, not button
- [ ] Logged errors for debugging
- [ ] Tested with actual user workflow

## References

- See `SimControlCentre.Plugins.GoXLR\Views\GoXLRDeviceControlPanel.xaml.cs` for examples
- Main app provides framework via `IPluginContext`
- Plugins implement complete UI following these guidelines
