# Quick Start Guide - New Desktop Integration

This guide will help you quickly test and use the new WorkerW desktop integration features.

## Step 1: Build the Project

```bash
cd /mnt/z/stuff/dev/living-project/NoFences-main

# Restore and build
msbuild NoFences.sln -t:Restore
msbuild NoFences.sln /p:Configuration=Debug /p:Platform="Any CPU"
```

Expected output: `Build succeeded. 0 Warning(s)`

## Step 2: Add Test Menu to Tray Icon

Open `NoFences/ApplicationLogic/TrayIconManager.cs` and add a test menu item:

```csharp
// In the CreateContextMenu() or similar method:
var testIntegrationItem = new ToolStripMenuItem("🔧 Test Desktop Integration");
testIntegrationItem.Click += (s, e) => {
    NoFences.View.DesktopIntegrationTest.ShowTestDialog();
};
trayMenu.Items.Add(testIntegrationItem);
```

## Step 3: Run and Test

1. **Start the application:**
   ```bash
   ./NoFences/bin/Debug/NoFences.exe
   ```

2. **Right-click the system tray icon**

3. **Click "🔧 Test Desktop Integration"**

4. **Test Behind Icons Mode:**
   - Click "Test: Behind Desktop Icons"
   - A semi-transparent **blue** window should appear **BEHIND** your desktop icons
   - You should see desktop icons floating OVER the blue window
   - Click "Close This Test Window" to close it

5. **Test Above Icons Mode:**
   - Click "Test: Above Desktop Icons"
   - A semi-transparent **green** window should appear **ABOVE** your desktop icons
   - Desktop icons should be UNDER the green window
   - Click "Close This Test Window" to close it

6. **View Integration Info:**
   - Click "Show Integration Info"
   - Check that WorkerW Handle is not zero (e.g., `0x12345678`)
   - Check that Progman Handle is not zero

## Step 4: Create a Fence Behind Desktop Icons

### Option A: Via EditDialog

1. Create a new fence or edit existing one
2. Add a checkbox to `EditDialog.cs`:
   ```csharp
   var chkBehindIcons = new CheckBox
   {
       Text = "Position behind desktop icons (wallpaper mode)",
       Checked = fenceInfo.BehindDesktopIcons,
       Location = new Point(20, 200)
   };
   this.Controls.Add(chkBehindIcons);

   // In save/OK handler:
   fenceInfo.BehindDesktopIcons = chkBehindIcons.Checked;
   ```

### Option B: Programmatically

Add to `Program.cs` for testing:
```csharp
// After fenceManager.LoadFences()
var testFence = new FenceInfo
{
    Id = Guid.NewGuid(),
    Name = "Test Behind Icons",
    BehindDesktopIcons = true,  // <-- Key line!
    Width = 400,
    Height = 300,
    PosX = 100,
    PosY = 100,
    Type = EntryType.Files.ToString()
};

var fenceWindow = new FenceWindow(testFence,
    CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<FenceHandlerFactory>());
fenceWindow.Show();
```

Run the app and you should see the fence **behind desktop icons**!

## Step 5: Use Transparent Desktop Overlay

Add to `Program.cs`:

```csharp
public static class Program
{
    private static DesktopOverlayManager _overlayManager;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        DependencyInjectionSetup.InitializeIoCContainer();

        // Create overlay manager
        _overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
        _overlayManager.Initialize();

        // Add some content to primary overlay
        var overlay = _overlayManager.GetPrimaryOverlay();
        var label = new Label
        {
            Text = "Desktop Overlay Active!",
            Font = new Font("Arial", 18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(100, Color.Blue),
            AutoSize = true,
            Location = new Point(50, 50)
        };
        overlay.Controls.Add(label);

        _overlayManager.ShowOverlays();

        // ... rest of startup code ...

        Application.Run();

        // Cleanup
        _overlayManager?.Dispose();
    }
}
```

Run the app and you'll see "Desktop Overlay Active!" text on your desktop!

## Step 6: Test Display Changes

1. Run the application with some fences
2. Open Display Settings (Windows + P, then Display Settings)
3. Change the resolution
4. Watch the fences automatically reposition and re-parent
5. Check the log for "refreshed after display change" messages

## Common Issues and Solutions

### Issue: "WorkerW Handle is 0"

**Cause:** WorkerW couldn't be found or created

**Solution:**
1. Click "Refresh Integration" in test dialog
2. Check if Desktop Window Manager is running:
   ```bash
   tasklist | findstr dwm.exe
   ```
3. Restart Windows Explorer:
   - Ctrl + Shift + Esc (Task Manager)
   - Find "Windows Explorer"
   - Right-click → Restart

### Issue: Windows appear but not in correct layer

**Cause:** Parenting happened before window was shown

**Solution:** Always call `Show()` before parenting:
```csharp
window.Show();
WorkerWIntegration.ParentToBehindDesktopIcons(window.Handle);
```

### Issue: Overlay blocks mouse clicks

**Cause:** Overlay `WndProc` not handling `WM_NCHITTEST` correctly

**Solution:** Already implemented in `DesktopOverlayWindow`, but if customizing:
```csharp
protected override void WndProc(ref Message m)
{
    const int WM_NCHITTEST = 0x0084;
    const int HTTRANSPARENT = -1;

    if (m.Msg == WM_NCHITTEST)
    {
        m.Result = (IntPtr)HTTRANSPARENT;  // Click-through
        return;
    }
    base.WndProc(ref m);
}
```

### Issue: Fences disappear after display change

**Cause:** This should not happen - auto-refresh is implemented

**Check:**
1. Look for exceptions in logs
2. Verify `SystemEvents.DisplaySettingsChanged` is subscribed
3. Try manual refresh: `WorkerWIntegration.RefreshDesktopIntegration(handle, true)`

## Quick Reference

### Position Fence Behind Icons
```csharp
fenceInfo.BehindDesktopIcons = true;
```

### Position Fence Above Icons (Default)
```csharp
fenceInfo.BehindDesktopIcons = false;
```

### Change Mode Dynamically
```csharp
fenceWindow.SetBehindDesktopIcons(true);  // Move behind
fenceWindow.SetBehindDesktopIcons(false); // Move above
```

### Create Overlay
```csharp
var overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
overlayManager.Initialize();
```

### Get Overlay for Screen
```csharp
var overlay = overlayManager.GetPrimaryOverlay();
// or
var overlay = overlayManager.GetOverlayForScreen(Screen.PrimaryScreen);
```

### Add WPF Content to Overlay
```csharp
var wpfControl = new MyWpfControl();
var host = new ElementHost
{
    Child = wpfControl,
    Dock = DockStyle.Fill
};
overlay.Controls.Add(host);
```

### Get Integration Info (Debug)
```csharp
var info = WorkerWIntegration.GetIntegrationInfo();
Console.WriteLine($"WorkerW: {info.WorkerWHandle:X}");
Console.WriteLine($"Progman: {info.ProgmanHandle:X}");
```

## Example: Complete Desktop Canvas Application

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using NoFences.View;
using NoFences.Win32;

class DesktopCanvasExample
{
    private DesktopOverlayManager overlayManager;

    public void Initialize()
    {
        // Create overlay system
        overlayManager = new DesktopOverlayManager(DesktopMode.AboveIcons);
        overlayManager.Initialize();

        // Get primary overlay
        var overlay = overlayManager.GetPrimaryOverlay();

        // Add custom drawing
        overlay.Paint += OnOverlayPaint;

        // Add click handler (if overlay is not click-through)
        overlay.MouseClick += OnOverlayClick;

        // Show overlays
        overlayManager.ShowOverlays();
    }

    private void OnOverlayPaint(object sender, PaintEventArgs e)
    {
        // Draw something on desktop
        using (var font = new Font("Arial", 24))
        using (var brush = new SolidBrush(Color.White))
        {
            e.Graphics.DrawString(
                "Custom Desktop Canvas",
                font,
                brush,
                new PointF(100, 100)
            );
        }
    }

    private void OnOverlayClick(object sender, MouseEventArgs e)
    {
        // Handle click at e.Location
        MessageBox.Show($"Clicked at {e.Location}");
    }

    public void Cleanup()
    {
        overlayManager?.Dispose();
    }
}
```

## What's Next?

Now that you have the desktop integration working:

1. **Add UI Controls:** Create checkboxes/menus to toggle `BehindDesktopIcons` mode
2. **Explore WPF:** Use `DesktopOverlayManager` to host rich WPF controls
3. **Create Widgets:** Build desktop widgets using the overlay system
4. **Implement Gestures:** Add double-click or drag gestures on overlays
5. **Visual Effects:** Add animations, parallax, or other effects

## Need Help?

- Read: `DESKTOP_INTEGRATION_REFACTORING.md` for technical details
- Read: `REFACTORING_SUMMARY.md` for overview
- Run: `DesktopIntegrationTest.ShowTestDialog()` for debugging
- Check: Logs in `Logger.Log()` output

## Success Checklist

- [ ] Project builds without errors
- [ ] Test dialog opens and shows WorkerW info
- [ ] Blue test window appears **behind** desktop icons
- [ ] Green test window appears **above** desktop icons
- [ ] Display changes are handled automatically
- [ ] Overlay manager creates transparent windows
- [ ] Can add controls to overlays
- [ ] Fences can be positioned behind or above icons

If all checked: ✅ **You're ready to build advanced desktop features!**

---

Happy coding! 🚀
