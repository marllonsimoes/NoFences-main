# New Architecture Guide - DesktopCanvas + FenceContainer

## Overview

This document explains the new architecture where all fences are hosted in a single window (DesktopCanvas) rather than each fence being a separate Form.

## Architecture Comparison

### OLD Architecture (FenceWindow - Still Works!)
```
Program.cs
  └── FenceManager (old)
        ├── FenceWindow #1 (Form) ← Separate window
        ├── FenceWindow #2 (Form) ← Separate window
        └── FenceWindow #3 (Form) ← Separate window

Each fence is its own Form window.
Each window tries to parent itself to desktop.
```

### NEW Architecture (DesktopCanvas)
```
Program.cs
  └── FenceManagerNew
        └── DesktopCanvas (Single Form)
              ├── FenceContainer #1 (UserControl)
              ├── FenceContainer #2 (UserControl)
              └── FenceContainer #3 (UserControl)

All fences are UserControls hosted in one canvas window.
Only the canvas needs to be parented to desktop.
```

## Key Components

### 1. `FenceContainer` (UserControl)
**File:** `NoFences/View/FenceContainer.cs`

Replaces `FenceWindow` Form with a UserControl.

**Features:**
- All fence logic (rendering, events, minify, drag)
- Context menu
- Handler-based rendering (same as FenceWindow)
- Events: `FenceChanged`, `FenceDeleted`, `FenceEdited`

**Usage:**
```csharp
var container = new FenceContainer(fenceInfo, handlerFactory);
// Add to parent
parentControl.Controls.Add(container);
```

### 2. `DesktopCanvas` (Form)
**File:** `NoFences/View/DesktopCanvas.cs`

Single transparent fullscreen window that hosts all fences.

**Features:**
- Transparent, borderless, fullscreen
- Parented to desktop
- Manages all FenceContainers
- Click-through where there are no fences
- Toggle between WorkerW and legacy integration

**Usage:**
```csharp
var canvas = new DesktopCanvas(handlerFactory, useWorkerW: false);
canvas.Show();

// Add fences
canvas.AddFence(fenceInfo1);
canvas.AddFence(fenceInfo2);
```

### 3. `FenceManagerNew`
**File:** `NoFences/Model/FenceManagerNew.cs`

New fence manager that works with DesktopCanvas.

**Features:**
- Loads fences from disk
- Creates fences as UserControls
- Auto-saves on changes
- Manages canvas lifecycle

**Usage:**
```csharp
var manager = new FenceManagerNew(handlerFactory, useWorkerW: false);
manager.LoadFences();
manager.ShowCanvas();
```

## How to Use

### Option 1: Use OLD Architecture (Still Works)

In `Program.cs`:
```csharp
// This is the ORIGINAL code - unchanged
DependencyInjectionSetup.InitializeIoCContainer();

var fenceManager = Ioc.Default.GetService<FenceManager>();
fenceManager.LoadFences();

if (Application.OpenForms.Count == 0)
{
    fenceManager.CreateFence("First fence");
}

Application.Run();
```

### Option 2: Use NEW Architecture

In `Program.cs`:
```csharp
DependencyInjectionSetup.InitializeIoCContainer();

var handlerFactory = Ioc.Default.GetService<FenceHandlerFactory>();

// Create new manager with legacy integration (no WorkerW)
var fenceManager = new FenceManagerNew(handlerFactory, useWorkerW: false);

// Load fences
fenceManager.LoadFences();

// Show canvas
fenceManager.ShowCanvas();

// Create default fence if none exist
if (fenceManager.FenceCount == 0)
{
    fenceManager.CreateFence("First fence");
}

// Run with canvas as main form
Application.Run(fenceManager.Canvas);
```

### Option 3: Toggle Between Both

```csharp
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    DependencyInjectionSetup.InitializeIoCContainer();

    // Choose architecture
    bool useNewArchitecture = true; // ← Change this to switch

    if (useNewArchitecture)
    {
        RunNewArchitecture();
    }
    else
    {
        RunOldArchitecture();
    }
}

static void RunOldArchitecture()
{
    var fenceManager = Ioc.Default.GetService<FenceManager>();
    fenceManager.LoadFences();

    if (Application.OpenForms.Count == 0)
    {
        fenceManager.CreateFence("First fence");
    }

    Application.Run();
}

static void RunNewArchitecture()
{
    var handlerFactory = Ioc.Default.GetService<FenceHandlerFactory>();
    var fenceManager = new FenceManagerNew(handlerFactory, useWorkerW: false);

    fenceManager.LoadFences();
    fenceManager.ShowCanvas();

    if (fenceManager.FenceCount == 0)
    {
        fenceManager.CreateFence("First fence");
    }

    Application.Run(fenceManager.Canvas);
}
```

## Benefits of New Architecture

### ✅ Advantages
1. **Simpler Desktop Integration** - Only one window to parent
2. **Better Performance** - One window instead of many
3. **Easier to Manage** - All fences in one place
4. **WPF Ready** - Easy to add WPF content via ElementHost
5. **Future-Proof** - Foundation for advanced features

### ⚠️ Important Notes
1. **Both architectures work** - Old code is unchanged
2. **Separate codebases** - No mixing of old/new
3. **Same data format** - Both use same XML fence storage
4. **Can toggle** - Switch between old/new easily

## Visual Comparison

### OLD (FenceWindow):
```
Desktop
├── FenceWindow #1 (z-order 10)
├── FenceWindow #2 (z-order 11)
└── FenceWindow #3 (z-order 12)

Each manages its own desktop parenting
Each has own window handle
Multiple WndProc message loops
```

### NEW (DesktopCanvas):
```
Desktop
└── DesktopCanvas (z-order 10)
      ├── FenceContainer #1
      ├── FenceContainer #2
      └── FenceContainer #3

Single desktop parenting
Single window handle
One WndProc message loop
Child controls inherit parent state
```

## Testing the New Architecture

### Test 1: Build
```bash
msbuild NoFences.sln /p:Configuration=Debug
```
Should build without errors.

### Test 2: Run with Old Architecture
In `Program.cs`, use old code (original).
```bash
./NoFences/bin/Debug/NoFences.exe
```
Should work exactly as before.

### Test 3: Run with New Architecture
In `Program.cs`, use new code (see Option 2 above).
```bash
./NoFences/bin/Debug/NoFences.exe
```
Should create a transparent canvas with fences as controls.

### Test 4: Visual Comparison
Run both side-by-side:
1. Build the solution
2. Make two copies of NoFences.exe:
   - `NoFences-Old.exe` (with old code)
   - `NoFences-New.exe` (with new code)
3. Run both simultaneously
4. Compare behavior

## Files Modified

### New Files Created
- `NoFences/View/FenceContainer.cs` - UserControl version of fence
- `NoFences/View/DesktopCanvas.cs` - Main canvas window
- `NoFences/Model/FenceManagerNew.cs` - New manager for canvas architecture

### Old Files Preserved (Unchanged)
- `NoFences/View/FenceWindow.cs` - Original Form-based fence (WORKING)
- `NoFences/Model/FenceManager.cs` - Original manager (WORKING)

### Files Updated (Both versions supported)
- `NoFences/Win32/DesktopUtil.cs` - Added `GlueToDesktopNew()` method
  - `GlueToDesktop()` - Original method (unchanged)
  - `GlueToDesktopNew()` - New method with WorkerW

## Migration Path

If you want to migrate from old to new:

1. **Keep old code working** ✅ Done - FenceWindow still works
2. **Test new architecture** ← You are here
3. **Compare both visually** ← Next step
4. **Choose which to use** ← After testing
5. **Optionally deprecate old** ← Future

You can also keep both and let users choose!

## Troubleshooting

### Issue: Old architecture stopped working
**Cause:** None - old code is unchanged
**Check:** Make sure you're using `FenceManager` (old) not `FenceManagerNew`

### Issue: New architecture shows black canvas
**Cause:** Canvas might be covering entire screen
**Check:**
```csharp
// In DesktopCanvas constructor
this.BackColor = Color.Red; // Temporarily make it visible
// this.TransparencyKey = Color.Magenta; // Comment out
```

### Issue: Fences not appearing on canvas
**Check:**
```csharp
Logger.Log($"Canvas has {canvas.FenceCount} fences");
Logger.Log($"Canvas controls: {canvas.Controls.Count}");
```

### Issue: Can't interact with fences
**Check WndProc in DesktopCanvas:**
- Click-through only works outside fence bounds
- Inside fence bounds should allow interaction

## Next Steps

1. ✅ Build and run with old architecture
2. ✅ Build and run with new architecture
3. Compare both visually
4. Choose which architecture to use going forward
5. Add features to chosen architecture

## Code Injection Points

### Add to Tray Menu
```csharp
// In TrayIconManager.cs
var switchArchitectureItem = new ToolStripMenuItem("Switch Architecture");
switchArchitectureItem.Click += (s, e) =>
{
    // Restart with different architecture
    Application.Restart();
};
trayMenu.Items.Add(switchArchitectureItem);
```

### Save Architecture Preference
```csharp
// In app settings
Properties.Settings.Default.UseNewArchitecture = true;
Properties.Settings.Default.Save();
```

## Summary

- **Old Architecture**: Individual Form windows (FenceWindow) - WORKING
- **New Architecture**: Single canvas with UserControls (DesktopCanvas + FenceContainer) - NEW
- **Both Coexist**: No conflicts, can toggle easily
- **Same Data**: Both read/write same XML files
- **Your Choice**: Use whichever works best for your needs

The new architecture provides a better foundation for advanced features, but the old architecture is proven and stable. Choose based on your needs!

---

**Status:** ✅ Both architectures ready for testing
**Recommendation:** Test new architecture, compare with old, then decide
