# WPF Integration Guide - Complete Isolation

## Overview

The NEW canvas-based architecture now features **complete isolation** with WPF-based content rendering, separate from the OLD WinForms painting approach.

## Architecture Comparison

### OLD Architecture (WinForms Painting)
- **View**: `FenceWindow` (Form per fence)
- **Rendering**: GDI+ painting via `Paint` events
- **Handlers**: `IFenceHandler` interface
  - `FilesFenceHandler` - Paints file icons using Graphics
  - `PictureFenceHandler` - Paints images using Graphics
- **Factory**: `FenceHandlerFactory`
- **DI Setup**: `DependencyInjectionSetup`

### NEW Architecture (WPF Integration)
- **View**: `DesktopCanvasWpf` (Single Form) → `FenceContainerWpf` (UserControls)
- **Rendering**: WPF UIElements hosted via `ElementHost`
- **Handlers**: `IFenceHandlerWpf` interface
  - `FilesFenceHandlerWpf` - Returns WPF ItemsControl with file items
  - `PictureFenceHandlerWpf` - Returns WPF Image control with slideshow
- **Factory**: `FenceHandlerFactoryWpf`
- **DI Setup**: `DependencyInjectionSetupNew`

## Complete File Structure

```
NoFences/
├── ApplicationLogic/
│   ├── DependencyInjectionSetup.cs          ← OLD (unchanged)
│   └── CanvasBased/
│       └── DependencyInjectionSetupNew.cs   ← NEW (WPF handlers)
│
├── Model/
│   ├── FenceManager.cs                      ← OLD (unchanged)
│   └── CanvasBased/
│       └── FenceManagerNew.cs               ← NEW (WPF canvas)
│
├── View/
│   ├── FenceWindow.cs                       ← OLD (unchanged)
│   ├── Fences/Handlers/                     ← OLD (WinForms handlers)
│   │   ├── IFenceHandler.cs
│   │   ├── FilesFenceHandler.cs
│   │   ├── PictureFenceHandler.cs
│   │   └── FenceHandlerFactory.cs
│   └── CanvasBased/                         ← NEW (WPF architecture)
│       ├── DesktopCanvas.cs                 ← OLD approach (WinForms painting)
│       ├── FenceContainer.cs                ← OLD approach (WinForms painting)
│       ├── DesktopCanvasWpf.cs              ← NEW (WPF integration)
│       ├── FenceContainerWpf.cs             ← NEW (WPF integration)
│       └── Handlers/                        ← NEW (WPF handlers)
│           ├── IFenceHandlerWpf.cs
│           ├── FilesFenceHandlerWpf.cs
│           ├── PictureFenceHandlerWpf.cs
│           └── FenceHandlerFactoryWpf.cs
│
└── Win32/
    ├── DesktopUtil.cs                       ← OLD (unchanged)
    ├── WorkerWIntegration.cs                ← Shared
    └── CanvasBased/
        └── DesktopUtilNew.cs                ← NEW
```

## Key WPF Components

### 1. IFenceHandlerWpf Interface

```csharp
public interface IFenceHandlerWpf
{
    void Initialize(FenceInfo fenceInfo);
    UIElement CreateContentElement(int titleHeight);
    void Cleanup();
    void Refresh();
}
```

**Key difference from IFenceHandler**: Instead of implementing `Paint(PaintEventArgs e)`, it returns a `UIElement` that gets hosted via ElementHost.

### 2. FilesFenceHandlerWpf

**Features**:
- Returns WPF `ItemsControl` with `WrapPanel`
- Uses `ObservableCollection<FileItemViewModel>` for data binding
- Each file item displays icon + text using WPF data templates
- Hover effects via WPF styling
- Double-click to open files

**WPF Controls Used**:
- `ItemsControl` - Main container
- `WrapPanel` - Grid layout
- `ScrollViewer` - Scrolling support
- `Image` - File icons (converted from System.Drawing.Icon)
- `TextBlock` - File names with drop shadow

### 3. PictureFenceHandlerWpf

**Features**:
- Returns WPF `Image` control
- Uses `BitmapImage` for picture loading
- `DispatcherTimer` for slideshow
- Automatic EXIF rotation handling
- Stretch.Uniform for proper aspect ratio

**WPF Controls Used**:
- `Image` - Picture display
- `DispatcherTimer` - Slideshow timing

### 4. FenceContainerWpf

**Architecture**:
```
FenceContainerWpf (UserControl)
├── titlePanel (Panel) - WinForms panel for title bar
└── elementHost (ElementHost) - Hosts WPF content
    └── WPF UIElement from handler
```

**Key Features**:
- Hybrid WinForms/WPF approach
- Title bar remains WinForms (for dragging)
- Content area uses ElementHost to host WPF UIElements
- All fence logic (minify, drag, context menu) preserved

### 5. DesktopCanvasWpf

**Same as DesktopCanvas but hosts `FenceContainerWpf` instead of `FenceContainer`**

## Usage Examples

### Example 1: Using NEW Architecture with WPF Handlers

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

        // Use NEW DI setup with WPF handlers
        var serviceProvider = DependencyInjectionSetupNew.InitializeIoCContainer(useWorkerW: false);

        // Get NEW FenceManager (uses WPF handlers)
        var fenceManager = serviceProvider.GetRequiredService<FenceManagerNew>();

        fenceManager.LoadFences();
        fenceManager.ShowCanvas();

        if (fenceManager.FenceCount == 0)
        {
            // Create a file fence
            var fileFence = fenceManager.CreateFence("My Files");
            fileFence.Type = "Files";
            fileFence.Path = @"C:\Users\YourName\Desktop";
            fenceManager.UpdateFence(fileFence);

            // Create a picture fence
            var pictureFence = fenceManager.CreateFence("Slideshow");
            pictureFence.Type = "Pictures";
            pictureFence.Items = new List<string>
            {
                @"C:\Pictures\photo1.jpg",
                @"C:\Pictures\photo2.jpg"
            };
            pictureFence.Interval = 5000; // 5 seconds
            fenceManager.UpdateFence(pictureFence);
        }

        Application.Run(fenceManager.Canvas);
    }
}
```

### Example 2: Toggle Between Architectures

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
        bool useNewArchitecture = true;   // ← Toggle here
        bool useWpfHandlers = true;       // ← Toggle WPF vs WinForms handlers (NEW only)
        bool useWorkerW = false;          // ← Toggle WorkerW integration

        if (useNewArchitecture)
        {
            if (useWpfHandlers)
            {
                RunNewArchitectureWithWpf(useWorkerW);
            }
            else
            {
                RunNewArchitectureWithWinForms(useWorkerW);
            }
        }
        else
        {
            RunOldArchitecture();
        }
    }

    static void RunOldArchitecture()
    {
        // Original DI with WinForms handlers
        DependencyInjectionSetup.InitializeIoCContainer();

        // Original manager with Form-per-fence
        var fenceManager = Ioc.Default.GetService<FenceManager>();
        fenceManager.LoadFences();

        if (Application.OpenForms.Count == 0)
        {
            fenceManager.CreateFence("First fence");
        }

        Application.Run();
    }

    static void RunNewArchitectureWithWpf(bool useWorkerW)
    {
        // NEW DI with WPF handlers
        var serviceProvider = DependencyInjectionSetupNew.InitializeIoCContainer(useWorkerW);

        // NEW manager with WPF canvas
        var fenceManager = serviceProvider.GetRequiredService<FenceManagerNew>();
        fenceManager.LoadFences();
        fenceManager.ShowCanvas();

        if (fenceManager.FenceCount == 0)
        {
            fenceManager.CreateFence("First fence (WPF)");
        }

        Application.Run(fenceManager.Canvas);
    }

    static void RunNewArchitectureWithWinForms(bool useWorkerW)
    {
        // Use DesktopCanvas with FenceContainer (WinForms painting)
        // NOTE: This would require keeping old DI setup for old handlers
        // Not recommended - prefer either OLD or NEW with WPF
    }
}
```

## Benefits of WPF Integration

### 1. **Complete Isolation**
- OLD architecture: 100% WinForms with GDI+ painting
- NEW architecture: WPF content rendering via ElementHost
- Zero code mixing between architectures

### 2. **Modern UI Capabilities**
- Data binding with ObservableCollection
- XAML-style layouts (WrapPanel, StackPanel, Grid)
- Built-in animation support
- Better text rendering with ClearType
- Hardware acceleration

### 3. **Easier Development**
- No manual painting code
- WPF controls handle layout automatically
- Simpler event handling
- Data templates for consistent styling

### 4. **Future Extensibility**
- Easy to add new WPF-based fence types
- Can use third-party WPF controls
- Can integrate with XAML designers
- Supports custom WPF user controls

## Creating Custom WPF Handlers

### Step 1: Implement IFenceHandlerWpf

```csharp
public class CustomFenceHandlerWpf : IFenceHandlerWpf
{
    private FenceInfo fenceInfo;

    public void Initialize(FenceInfo fenceInfo)
    {
        this.fenceInfo = fenceInfo;
    }

    public UIElement CreateContentElement(int titleHeight)
    {
        // Create your WPF UI here
        var textBlock = new TextBlock
        {
            Text = $"Custom Fence: {fenceInfo.Name}",
            Foreground = Brushes.White,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return textBlock;
    }

    public void Refresh()
    {
        // Refresh content if needed
    }

    public void Cleanup()
    {
        // Cleanup resources
    }
}
```

### Step 2: Register in DI

```csharp
// In DependencyInjectionSetupNew.cs
services.AddTransient<IFenceHandlerWpf, CustomFenceHandlerWpf>();

// In handler factory registration
handlers["Custom"] = typeof(CustomFenceHandlerWpf);
```

### Step 3: Use in FenceInfo

```csharp
var customFence = new FenceInfo(Guid.NewGuid())
{
    Name = "My Custom Fence",
    Type = "Custom",  // Matches registration key
    PosX = 100,
    PosY = 100,
    Width = 400,
    Height = 300
};

fenceManager.CreateFence(customFence);
```

## Technical Notes

### ElementHost Integration

`ElementHost` is a WinForms control that hosts WPF content. It bridges the two UI frameworks:

- **WinForms side**: FenceContainerWpf is a UserControl
- **WPF side**: Handler returns UIElement
- **Bridge**: ElementHost.Child = wpfUIElement

### Thread Safety

WPF requires STA threading. Ensure your application uses `[STAThread]`:

```csharp
[STAThread]
static void Main()
{
    // Your code
}
```

### Performance Considerations

- WPF uses hardware acceleration (GPU rendering)
- ElementHost has slight overhead for WinForms/WPF interop
- For best performance, minimize WinForms/WPF crossing
- Each FenceContainerWpf has one ElementHost (minimal overhead)

## Migration Path

### From OLD to NEW

1. Keep using OLD architecture as-is (fully working)
2. Test NEW architecture with WPF handlers side-by-side
3. Once satisfied, switch main application to NEW
4. OLD code remains available for reference/fallback

### From NEW WinForms to NEW WPF

If you were using DesktopCanvas with FenceContainer (WinForms painting):

1. Switch DI to use `DependencyInjectionSetupNew` (updated version)
2. Change code to use `DesktopCanvasWpf` instead of `DesktopCanvas`
3. Handlers automatically switch to WPF versions via DI

## Troubleshooting

### Issue: WPF content not visible

**Solution**: Check that ElementHost is properly added and brought to front:
```csharp
this.Controls.Add(elementHost);
elementHost.BringToFront();
titlePanel.BringToFront(); // Keep title on top
```

### Issue: Mouse events not working in WPF content

**Solution**: ElementHost automatically handles mouse events. Ensure WPF controls have appropriate event handlers.

### Issue: Icons not displaying

**Solution**: Ensure icon conversion from System.Drawing.Icon to BitmapSource:
```csharp
var icon = entry.ExtractIcon(thumbnailProvider);
var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
    icon.Handle,
    Int32Rect.Empty,
    BitmapSizeOptions.FromEmptyOptions());
```

## Summary

✅ **Complete isolation achieved**:
- OLD: WinForms painting with IFenceHandler
- NEW: WPF rendering with IFenceHandlerWpf

✅ **Modern UI capabilities**:
- Data binding
- WPF layouts
- Better rendering

✅ **Easy extensibility**:
- Create custom WPF handlers
- Use third-party WPF controls
- XAML integration possible

✅ **Backward compatibility**:
- OLD architecture unchanged
- Can run both side-by-side
- Easy toggle between approaches

**Status**: ✅ Full WPF integration complete with complete isolation!
