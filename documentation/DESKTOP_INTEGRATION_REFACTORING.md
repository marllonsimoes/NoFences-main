# Desktop Integration Refactoring - WorkerW Method

This document describes the refactoring of NoFences to use the WorkerW desktop integration method, similar to how Lively and other modern wallpaper applications work.

## What Changed

### 1. New WorkerW Integration (`NoFences/Win32/WorkerWIntegration.cs`)

**Purpose:** Provides robust Windows desktop integration using the WorkerW window method.

**Key Features:**
- `ParentToBehindDesktopIcons()` - Positions windows behind desktop icons (wallpaper layer)
- `ParentToAboveDesktopIcons()` - Positions windows above desktop icons (traditional behavior)
- `GetWorkerW()` - Finds or creates the WorkerW window with caching
- `RefreshDesktopIntegration()` - Refreshes integration when display changes
- `GetIntegrationInfo()` - Debugging information about desktop state

**How It Works:**
1. Finds the "Progman" window (Program Manager - the desktop root)
2. Sends message `0x052C` to spawn a WorkerW window
3. Enumerates windows to find the WorkerW containing SHELLDLL_DefView
4. Parents the target window to this WorkerW

This places windows in the desktop layer, either behind or above icons.

### 2. Desktop Overlay Manager (`NoFences/View/DesktopOverlayManager.cs`)

**Purpose:** Manages transparent fullscreen overlays that can host WPF components.

**Key Features:**
- Creates one overlay window per screen
- Transparent, click-through capable
- Monitors display changes automatically
- Supports both "behind icons" and "above icons" modes

**Usage Example:**
```csharp
// Create overlay manager
var overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
overlayManager.Initialize();

// Get primary screen overlay
var primaryOverlay = overlayManager.GetPrimaryOverlay();

// Add WPF or WinForms controls to the overlay
var wpfHost = new ElementHost();
wpfHost.Child = new MyWpfControl();
primaryOverlay.Controls.Add(wpfHost);

// Change mode dynamically
overlayManager.SetDesktopMode(DesktopMode.BehindIcons);
```

### 3. Updated DesktopUtil (`NoFences/Win32/DesktopUtil.cs`)

**Old Implementation:**
```csharp
public static void GlueToDesktop(IntPtr handle)
{
    IntPtr progman = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
    SetWindowLongPtr(handle, GWL_HWNDPARENT, progman.ToInt32());
}
```

**New Implementation:**
```csharp
public static void GlueToDesktop(IntPtr handle, bool behindIcons = false)
{
    if (behindIcons)
    {
        WorkerWIntegration.ParentToBehindDesktopIcons(handle);
    }
    else
    {
        WorkerWIntegration.ParentToAboveDesktopIcons(handle);
    }
}
```

**Benefits:**
- More reliable on Windows 10/11
- Supports both positioning modes
- Properly handles display changes
- Better WorkerW window discovery

### 4. Enhanced FenceInfo Model (`NoFencesCore/Model/FenceInfo.cs`)

**New Property:**
```csharp
/// <summary>
/// Gets or sets whether this fence appears behind desktop icons (like a wallpaper)
/// or above desktop icons (traditional behavior). Default is false (above icons).
/// </summary>
public bool BehindDesktopIcons { get; set; } = false;
```

This property is serialized with fence data, allowing per-fence control of desktop positioning.

### 5. Updated FenceWindow (`NoFences/View/FenceWindow.cs`)

**Changes:**
1. Uses the new `WorkerWIntegration` for desktop parenting
2. Monitors display changes via `SystemEvents.DisplaySettingsChanged`
3. Automatically refreshes integration when displays change
4. New method `SetBehindDesktopIcons()` to change mode dynamically

**Example Usage:**
```csharp
// Change fence position relative to desktop icons
fenceWindow.SetBehindDesktopIcons(true);  // Move behind icons
fenceWindow.SetBehindDesktopIcons(false); // Move above icons
```

## Migration Guide

### For Existing Code

**Before:**
```csharp
DesktopUtil.GlueToDesktop(windowHandle);
```

**After (keeping same behavior):**
```csharp
// Default behavior - above icons (backward compatible)
DesktopUtil.GlueToDesktop(windowHandle);

// Or explicitly
DesktopUtil.GlueToDesktop(windowHandle, behindIcons: false);
```

**To position behind icons:**
```csharp
DesktopUtil.GlueToDesktop(windowHandle, behindIcons: true);
```

### For New Features

**Create a transparent desktop overlay:**
```csharp
// In your application startup (Program.cs)
var overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
overlayManager.Initialize();
overlayManager.ShowOverlays();

// Subscribe to display changes
overlayManager.DisplayChanged += (s, e) => {
    Console.WriteLine($"Display changed! Now have {e.Screens.Count} screens");
};

// Add to DI container if using
services.AddSingleton<DesktopOverlayManager>();
```

**Host WPF content in overlay:**
```csharp
var overlay = overlayManager.GetPrimaryOverlay();

// Create WPF content
var wpfWindow = new System.Windows.Window
{
    WindowStyle = WindowStyle.None,
    AllowsTransparency = true,
    Background = Brushes.Transparent,
    ShowInTaskbar = false
};

// Add your WPF controls
wpfWindow.Content = new MyCustomWpfControl();

// Host in overlay using ElementHost
var host = new System.Windows.Forms.Integration.ElementHost
{
    Child = wpfWindow.Content as UIElement,
    Dock = DockStyle.Fill
};

overlay.Controls.Add(host);
```

## Architecture Comparison

### Lively (Wallpaper Application)
```
[Desktop Icons]
      ↓
  [WorkerW] ← Parent animated wallpaper here
      ↓
  [Progman]
```

### NoFences - Traditional Mode (Above Icons)
```
[NoFences Windows] ← Interactive containers
      ↓
[Desktop Icons]
      ↓
  [WorkerW]
      ↓
  [Progman]
```

### NoFences - New Behind Icons Mode
```
[Desktop Icons]
      ↓
[NoFences Windows] ← Parented to WorkerW
      ↓
  [WorkerW]
      ↓
  [Progman]
```

## Technical Details

### The WorkerW Window

**What is WorkerW?**
- A hidden window created by Windows Explorer
- Part of the desktop composition system
- Sits between the wallpaper layer and desktop icons layer
- Created on-demand with the `0x052C` message

**Window Hierarchy:**
```
Desktop Root
├── Progman (Program Manager)
│   ├── SHELLDLL_DefView (Desktop Icons View)
│   └── SysListView32 (Icon List)
└── WorkerW (Multiple instances may exist)
    └── [Our parented windows go here]
```

**Discovery Process:**
1. Send `0x052C` message to Progman to spawn WorkerW
2. Enumerate all top-level windows
3. Find WorkerW that contains SHELLDLL_DefView
4. Get the NEXT WorkerW in z-order
5. This is the target for parenting

### Display Change Handling

**Events Monitored:**
- `SystemEvents.DisplaySettingsChanged` - Resolution changes, monitor add/remove
- Handled in both `FenceWindow` and `DesktopOverlayManager`

**Refresh Process:**
1. Detect display change event
2. Clear cached WorkerW handle
3. Re-discover WorkerW (may have been destroyed)
4. Re-parent all windows
5. Reposition for new screen bounds

### Multi-Monitor Support

**Overlay Manager Approach:**
```csharp
foreach (Screen screen in Screen.AllScreens)
{
    var overlay = new DesktopOverlayWindow(screen, mode);
    // Position overlay for specific screen
    overlay.Bounds = screen.Bounds;
    // Parent to WorkerW (WorkerW spans all monitors)
    WorkerWIntegration.ParentToBehindDesktopIcons(overlay.Handle);
}
```

**Note:** There's typically one WorkerW that spans all monitors. Each overlay window is positioned for its screen but parented to the same WorkerW.

## Usage Scenarios

### Scenario 1: Traditional NoFences (Above Icons)
```csharp
var fence = new FenceWindow(fenceInfo, handlerFactory);
// fenceInfo.BehindDesktopIcons = false (default)
// Fence appears above desktop icons, acts as container
```

### Scenario 2: Wallpaper-Style Fence (Behind Icons)
```csharp
fenceInfo.BehindDesktopIcons = true;
var fence = new FenceWindow(fenceInfo, handlerFactory);
// Fence appears behind desktop icons, like Lively wallpaper
```

### Scenario 3: Transparent Desktop Canvas
```csharp
var overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
overlayManager.Initialize();

// Create custom rendering on transparent overlay
var overlay = overlayManager.GetPrimaryOverlay();
overlay.Paint += (s, e) => {
    // Draw custom graphics on desktop
    e.Graphics.DrawString("Desktop Overlay", font, brush, point);
};
```

### Scenario 4: WPF Dialog on Desktop
```csharp
var overlay = overlayManager.GetPrimaryOverlay();

// Create WPF dialog
var wpfDialog = new MyWpfDialog();

// Host in ElementHost
var host = new ElementHost { Child = wpfDialog };

// Position on overlay
host.Location = new Point(100, 100);
host.Size = new Size(400, 300);

overlay.Controls.Add(host);
```

## Testing the Integration

### Test 1: Verify WorkerW Discovery
```csharp
var workerw = WorkerWIntegration.GetWorkerW();
Console.WriteLine($"WorkerW Handle: {workerw}");

var info = WorkerWIntegration.GetIntegrationInfo();
Console.WriteLine(info.ToString());
```

### Test 2: Test Behind Icons Mode
```csharp
// Create a test window
var testForm = new Form
{
    BackColor = Color.Red,
    Opacity = 0.5,
    FormBorderStyle = FormBorderStyle.None,
    Bounds = Screen.PrimaryScreen.Bounds
};

// Parent behind icons
WorkerWIntegration.ParentToBehindDesktopIcons(testForm.Handle);
testForm.Show();

// You should see a semi-transparent red layer BEHIND desktop icons
```

### Test 3: Test Above Icons Mode
```csharp
// Same as Test 2, but:
WorkerWIntegration.ParentToAboveDesktopIcons(testForm.Handle);

// You should see the red layer ABOVE desktop icons
```

### Test 4: Test Display Change
```csharp
SystemEvents.DisplaySettingsChanged += (s, e) => {
    Console.WriteLine("Display changed!");
    WorkerWIntegration.RefreshDesktopIntegration(testForm.Handle, true);
};

// Change display resolution or add/remove monitor
// Integration should automatically refresh
```

## Troubleshooting

### Issue: Window not appearing behind icons

**Possible causes:**
1. WorkerW not found - check with `GetIntegrationInfo()`
2. Windows security restrictions
3. Desktop composition disabled

**Solution:**
```csharp
var info = WorkerWIntegration.GetIntegrationInfo();
if (info.WorkerWHandle == IntPtr.Zero)
{
    // Force refresh
    var workerw = WorkerWIntegration.GetWorkerW(forceRefresh: true);
}
```

### Issue: Window disappears after display change

**Cause:** WorkerW was destroyed by Windows Explorer

**Solution:** Already handled automatically via display change events

### Issue: Performance problems with overlay

**Cause:** Continuous repainting of transparent window

**Solution:**
```csharp
// Reduce paint frequency
overlay.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

// Or make overlay non-visible when not needed
overlay.Visible = false;
```

## Next Steps

Now that the refactoring is complete, you can:

1. **Add UI controls** to switch between "Above Icons" and "Behind Icons" modes
2. **Create WPF-based fence types** that leverage the overlay manager
3. **Implement gestures** on the transparent overlay (e.g., double-click desktop to create fence)
4. **Add visual effects** like parallax or animations on the desktop layer
5. **Create a desktop widget system** using the overlay as a canvas

## Code Example: Complete Integration

```csharp
// In Program.cs
public static class Program
{
    private static DesktopOverlayManager _overlayManager;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Initialize DI
        DependencyInjectionSetup.InitializeIoCContainer();

        // Create overlay manager
        _overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
        _overlayManager.Initialize();
        _overlayManager.ShowOverlays();

        // Start application services
        var services = new List<IApplicationService>
        {
            new TrayIconManager(),
            new PipeService()
        };

        foreach (var service in services)
        {
            service.Start();
        }

        // Load fences
        var fenceManager = Ioc.Default.GetService<FenceManager>();
        fenceManager.LoadFences();

        // Run application
        Application.Run();

        // Cleanup
        _overlayManager?.Dispose();
    }
}
```

## Summary

The refactoring provides:
✅ Robust WorkerW-based desktop integration like Lively
✅ Support for both "above" and "behind" desktop icons modes
✅ Transparent fullscreen overlay system for WPF components
✅ Automatic display change handling
✅ Per-fence desktop positioning control
✅ Multi-monitor support
✅ Backward compatibility with existing code

The NoFences codebase is now ready for advanced desktop integration scenarios!
