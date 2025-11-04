# Complete Architecture Separation Guide

## Summary

Both architectures are now **100% separate** with zero mixing of code. Each has its own utilities, DI setup, and components.

## Complete File Organization

```
NoFences/
├── ApplicationLogic/
│   ├── DependencyInjectionSetup.cs          ← OLD (unchanged)
│   └── CanvasBased/
│       └── DependencyInjectionSetupNew.cs   ← NEW
│
├── Model/
│   ├── FenceManager.cs                      ← OLD (unchanged)
│   ├── FenceInfo.cs                         ← Shared
│   ├── EntryType.cs                         ← Shared
│   └── CanvasBased/
│       └── FenceManagerNew.cs               ← NEW
│
├── View/
│   ├── FenceWindow.cs                       ← OLD (unchanged)
│   ├── EditDialog.cs                        ← Shared
│   ├── Fences/Handlers/                     ← Shared
│   │   ├── IFenceHandler.cs
│   │   ├── FilesFenceHandler.cs
│   │   └── ...
│   └── CanvasBased/
│       ├── DesktopCanvas.cs                 ← NEW
│       └── FenceContainer.cs                ← NEW
│
└── Win32/
    ├── DesktopUtil.cs                       ← OLD (unchanged)
    ├── WindowUtil.cs                        ← Shared
    ├── BlurUtil.cs                          ← Shared
    ├── WorkerWIntegration.cs                ← NEW (shared with new arch)
    └── CanvasBased/
        └── DesktopUtilNew.cs                ← NEW
```

## Usage Examples

### OLD Architecture (Original - Works!)

```csharp
using NoFences.ApplicationLogic;
using NoFences.Model;
using CommunityToolkit.Mvvm.DependencyInjection;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Use ORIGINAL DI setup
        DependencyInjectionSetup.InitializeIoCContainer();

        // Use ORIGINAL FenceManager
        var fenceManager = Ioc.Default.GetService<FenceManager>();
        fenceManager.LoadFences();

        if (Application.OpenForms.Count == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run();
    }
}
```

### NEW Architecture (Canvas-Based)

#### Option 1: Without DI (Manual instantiation)
```csharp
using NoFences.Model.CanvasBased;
using NoFences.View.CanvasBased;
using NoFences.View.Fences.Handlers;
using CommunityToolkit.Mvvm.DependencyInjection;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Use ORIGINAL DI for handlers only
        NoFences.ApplicationLogic.DependencyInjectionSetup.InitializeIoCContainer();

        // Get handler factory
        var handlerFactory = Ioc.Default.GetService<FenceHandlerFactory>();

        // Manually create NEW FenceManager (not in DI)
        var fenceManager = new FenceManagerNew(handlerFactory, useWorkerW: false);

        fenceManager.LoadFences();
        fenceManager.ShowCanvas();

        if (fenceManager.FenceCount == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run(fenceManager.Canvas);
    }
}
```

#### Option 2: With NEW DI (Fully managed)
```csharp
using NoFences.ApplicationLogic.CanvasBased;
using NoFences.Model.CanvasBased;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Use NEW DI setup
        var serviceProvider = DependencyInjectionSetupNew.InitializeIoCContainer(useWorkerW: false);

        // Get NEW FenceManager from DI
        var fenceManager = serviceProvider.GetRequiredService<FenceManagerNew>();

        fenceManager.LoadFences();
        fenceManager.ShowCanvas();

        if (fenceManager.FenceCount == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run(fenceManager.Canvas);
    }
}
```

#### Option 3: With NEW DI + CommunityToolkit (Compatibility mode)
```csharp
using NoFences.ApplicationLogic.CanvasBased;
using NoFences.Model.CanvasBased;
using CommunityToolkit.Mvvm.DependencyInjection;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Use NEW DI with CommunityToolkit
        DependencyInjectionSetupNew.InitializeIoCContainerWithToolkit(useWorkerW: false);

        // Get from Ioc.Default (like old code)
        var fenceManager = Ioc.Default.GetService<FenceManagerNew>();

        fenceManager.LoadFences();
        fenceManager.ShowCanvas();

        if (fenceManager.FenceCount == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run(fenceManager.Canvas);
    }
}
```

## Toggle Between Architectures

```csharp
using NoFences.ApplicationLogic;
using NoFences.ApplicationLogic.CanvasBased;
using NoFences.Model;
using NoFences.Model.CanvasBased;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Configuration
        bool useNewArchitecture = false;  // ← Toggle here
        bool useWorkerW = false;          // ← For new architecture

        if (useNewArchitecture)
        {
            RunNewArchitecture(useWorkerW);
        }
        else
        {
            RunOldArchitecture();
        }
    }

    static void RunOldArchitecture()
    {
        // Original DI
        DependencyInjectionSetup.InitializeIoCContainer();

        // Original manager
        var fenceManager = Ioc.Default.GetService<FenceManager>();
        fenceManager.LoadFences();

        if (Application.OpenForms.Count == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run();
    }

    static void RunNewArchitecture(bool useWorkerW)
    {
        // New DI
        var serviceProvider = DependencyInjectionSetupNew.InitializeIoCContainer(useWorkerW);

        // New manager
        var fenceManager = serviceProvider.GetRequiredService<FenceManagerNew>();
        fenceManager.LoadFences();
        fenceManager.ShowCanvas();

        if (fenceManager.FenceCount == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run(fenceManager.Canvas);
    }
}
```

## What's Different Between Architectures?

### OLD Architecture Uses:
- `NoFences.ApplicationLogic.DependencyInjectionSetup`
- `NoFences.Model.FenceManager`
- `NoFences.View.FenceWindow`
- `NoFences.Win32.DesktopUtil` (original methods)

### NEW Architecture Uses:
- `NoFences.ApplicationLogic.CanvasBased.DependencyInjectionSetupNew`
- `NoFences.Model.CanvasBased.FenceManagerNew`
- `NoFences.View.CanvasBased.DesktopCanvas` + `FenceContainer`
- `NoFences.Win32.CanvasBased.DesktopUtilNew`
- `NoFences.Win32.WorkerWIntegration` (optional)

### Both Share:
- `NoFences.Model.FenceInfo`
- `NoFences.Model.EntryType`
- `NoFences.View.Fences.Handlers.*`
- `NoFences.Win32.WindowUtil`
- `NoFences.Win32.BlurUtil`
- `NoFences.Util.*`

## DI Container Options

### For OLD Architecture:
```csharp
// Only one option - use original
DependencyInjectionSetup.InitializeIoCContainer();

// Get services via CommunityToolkit
var manager = Ioc.Default.GetService<FenceManager>();
var factory = Ioc.Default.GetService<FenceHandlerFactory>();
```

### For NEW Architecture:

#### Option A: Use NEW DI directly
```csharp
var provider = DependencyInjectionSetupNew.InitializeIoCContainer(useWorkerW: false);

// Get services via Microsoft.Extensions.DependencyInjection
var manager = provider.GetRequiredService<FenceManagerNew>();
var factory = provider.GetRequiredService<FenceHandlerFactory>();
```

#### Option B: Use NEW DI with CommunityToolkit
```csharp
DependencyInjectionSetupNew.InitializeIoCContainerWithToolkit(useWorkerW: false);

// Get services via CommunityToolkit (same as old)
var manager = Ioc.Default.GetService<FenceManagerNew>();
var factory = Ioc.Default.GetService<FenceHandlerFactory>();
```

#### Option C: Manual instantiation (no DI for manager)
```csharp
// Use old DI for handlers
DependencyInjectionSetup.InitializeIoCContainer();

// Get factory
var factory = Ioc.Default.GetService<FenceHandlerFactory>();

// Manually create manager (not in DI)
var manager = new FenceManagerNew(factory, useWorkerW: false);
```

**Recommendation:** Option B or C for simplicity. Option A if you want full control.

## Build and Test

### Build
```bash
msbuild NoFences.sln /p:Configuration=Debug
```

### Test OLD Architecture
Set `useNewArchitecture = false` in Program.cs, then:
```bash
./NoFences/bin/Debug/NoFences.exe
```

### Test NEW Architecture
Set `useNewArchitecture = true` in Program.cs, then:
```bash
./NoFences/bin/Debug/NoFences.exe
```

### Test Both Side-by-Side
1. Build once
2. Copy exe twice with different names
3. Edit Program.cs between builds
4. Run both simultaneously

## Summary of Separation

| Component | OLD Location | NEW Location |
|-----------|--------------|--------------|
| DI Setup | `ApplicationLogic/` | `ApplicationLogic/CanvasBased/` |
| Manager | `Model/` | `Model/CanvasBased/` |
| View | `View/` | `View/CanvasBased/` |
| Utilities | `Win32/` | `Win32/CanvasBased/` |

✅ **Zero mixing** - Each architecture is completely independent
✅ **Clear separation** - Different namespaces and folders
✅ **Shared components** - Only common code is shared
✅ **Easy toggle** - One boolean flag to switch
✅ **DI flexibility** - NEW architecture supports multiple DI options

**Status:** ✅ Complete separation achieved!
