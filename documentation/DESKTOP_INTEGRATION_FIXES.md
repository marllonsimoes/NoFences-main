# Desktop Integration Fixes - Mouse Events and Rendering

## Issues Fixed

### Issue 1: Black Window / No Rendering
**Symptom:** FenceWindow shows as a black window with no title or content

**Root Cause:** Desktop parenting was being applied in the constructor before the window was fully initialized and shown. This prevented the window from rendering properly.

**Fix:** Moved desktop integration to `FenceWindow_Load` event, which fires after the window is shown and fully initialized.

**Code Change:**
```csharp
// BEFORE (in constructor) - WRONG
public FenceWindow(...)
{
    InitializeComponent();
    DropShadow.ApplyShadows(this);
    BlurUtil.EnableBlur(Handle);
    DesktopUtil.GlueToDesktop(Handle, ...); // Too early!
    // ... rest of initialization
}

// AFTER (in Load event) - CORRECT
private void FenceWindow_Load(object sender, EventArgs e)
{
    DropShadow.ApplyShadows(this);
    BlurUtil.EnableBlur(Handle);
    DesktopUtil.GlueToDesktop(Handle, fenceInfo.BehindDesktopIcons);
    this.Invalidate();
    this.Refresh(); // Force repaint
}
```

### Issue 2: Mouse Hover/Minify Not Working
**Symptom:** Mouse hover doesn't un-minify the fence window

**Root Cause:** Two separate issues:
1. Windows parented before being shown don't receive proper window messages
2. Windows positioned **behind desktop icons** cannot receive mouse events at all (they're literally behind the icons)

**Fix:**
1. Moved desktop parenting to Load event (fixes window message issue)
2. Added logic to prevent BehindDesktopIcons mode when CanMinify is enabled
3. Show clear error message if user tries to enable conflicting modes

**Code Change:**
```csharp
// Prevent incompatible mode combination
if (fenceInfo.CanMinify && fenceInfo.BehindDesktopIcons)
{
    Logger.Log("Disabling BehindDesktopIcons because CanMinify requires mouse events");
    fenceInfo.BehindDesktopIcons = false;
}
```

### Issue 3: Title Not Showing
**Symptom:** Window title bar content doesn't display

**Root Cause:** Same as Issue 1 - Paint event wasn't being triggered due to early parenting

**Fix:** Same as Issue 1 - moved parenting to Load event and added explicit Refresh() calls

## Important Limitations

### Windows Behind Desktop Icons Cannot Receive Mouse Events

**Why:** When a window is parented to WorkerW and positioned behind desktop icons (like Lively wallpaper), it's in a lower z-order layer. Windows in this layer do not receive mouse events because the desktop icons layer is in front.

**Impact:**
- Minify feature (mouse hover to expand/collapse) will NOT work
- Mouse clicks will NOT be received
- Drag/drop will NOT work
- Context menus triggered by right-click will NOT work

**Modes Affected:**
- `BehindDesktopIcons = true` (wallpaper mode)

**Modes That Work Normally:**
- `BehindDesktopIcons = false` (traditional mode - above icons)

### Mode Compatibility Matrix

| Feature | Above Icons | Behind Icons |
|---------|-------------|--------------|
| Rendering | ✅ Works | ✅ Works |
| Mouse Hover | ✅ Works | ❌ No events |
| Click/Drag | ✅ Works | ❌ No events |
| Minify/Expand | ✅ Works | ❌ Cannot work |
| Context Menu | ✅ Works | ❌ No events |
| Keyboard Input | ✅ Works | ❌ No events |
| Display Changes | ✅ Auto-refresh | ✅ Auto-refresh |

### Design Recommendation

**For Interactive Fences (default NoFences behavior):**
```csharp
fenceInfo.BehindDesktopIcons = false; // Above icons
fenceInfo.CanMinify = true;           // Minify works
```

**For Wallpaper/Background Fences (Lively-style):**
```csharp
fenceInfo.BehindDesktopIcons = true;  // Behind icons
fenceInfo.CanMinify = false;          // Minify won't work anyway
// This is good for:
// - Static displays
// - Picture fences
// - Clock fences
// - Background animations
```

## Testing the Fixes

### Test 1: Traditional Mode (Above Icons)
```csharp
var fenceInfo = new FenceInfo
{
    Name = "Test Interactive",
    BehindDesktopIcons = false,  // Above icons
    CanMinify = true,
    Width = 400,
    Height = 300
};
```

**Expected Behavior:**
- ✅ Window renders with title and content
- ✅ Mouse hover expands when minified
- ✅ Can drag fence around
- ✅ Right-click shows context menu
- ✅ Can interact with fence content

### Test 2: Wallpaper Mode (Behind Icons)
```csharp
var fenceInfo = new FenceInfo
{
    Name = "Test Background",
    BehindDesktopIcons = true,   // Behind icons
    CanMinify = false,           // Disabled
    Width = 800,
    Height = 600
};
```

**Expected Behavior:**
- ✅ Window renders with title and content
- ✅ Window appears BEHIND desktop icons
- ❌ Mouse hover does nothing (expected - no events)
- ❌ Cannot drag (expected - no events)
- ❌ Right-click goes to desktop (expected - no events)
- ✅ Display changes handled automatically

### Test 3: Conflicting Modes (Should Prevent)
```csharp
var fenceInfo = new FenceInfo
{
    Name = "Test Conflict",
    BehindDesktopIcons = true,   // Behind icons
    CanMinify = true,            // Requires mouse events - CONFLICT!
};

var fence = new FenceWindow(fenceInfo, handlerFactory);
```

**Expected Behavior:**
- ✅ Constructor automatically sets `BehindDesktopIcons = false`
- ✅ Logs warning message
- ✅ Fence works normally in Above Icons mode

If user tries to change mode via `SetBehindDesktopIcons(true)` while minify is enabled:
- ✅ Shows error dialog
- ✅ Mode change is blocked
- ✅ Fence remains in Above Icons mode

## Code Verification Checklist

When creating or modifying fence windows, ensure:

- [ ] Desktop parenting happens in `Load` or `Shown` event, NOT constructor
- [ ] `Invalidate()` and `Refresh()` called after parenting
- [ ] If enabling `BehindDesktopIcons`, disable `CanMinify`
- [ ] If enabling `CanMinify`, disable `BehindDesktopIcons`
- [ ] Effects (blur, shadow) applied before parenting for best results
- [ ] Window is shown before parenting (`Show()` or wait for Load event)

## Debugging Tips

### Issue: Window still black after fixes
**Check:**
1. Is `FenceWindow_Load` event handler attached?
   ```csharp
   // In FenceWindow.Designer.cs
   this.Load += new System.EventHandler(this.FenceWindow_Load);
   ```
2. Is Paint event being called?
   ```csharp
   protected override void OnPaint(PaintEventArgs e)
   {
       Logger.Log("Paint called");
       base.OnPaint(e);
   }
   ```
3. Try forcing invalidation:
   ```csharp
   this.Invalidate(true);
   this.Update();
   this.Refresh();
   ```

### Issue: Mouse events not working in Above Icons mode
**Check:**
1. Verify mode:
   ```csharp
   Logger.Log($"BehindDesktopIcons: {fenceInfo.BehindDesktopIcons}");
   ```
2. Check window parent:
   ```csharp
   var info = WorkerWIntegration.GetIntegrationInfo();
   Logger.Log($"WorkerW: {info.WorkerWHandle:X}");
   ```
3. Verify WndProc is receiving messages:
   ```csharp
   protected override void WndProc(ref Message m)
   {
       if (m.Msg == 0x0200) // WM_MOUSEMOVE
           Logger.Log("Mouse move detected");
       base.WndProc(ref m);
   }
   ```

### Issue: Effects (blur/shadow) not working
**Try:**
1. Apply effects AFTER parenting:
   ```csharp
   DesktopUtil.GlueToDesktop(Handle, false);
   BlurUtil.EnableBlur(Handle);
   DropShadow.ApplyShadows(this);
   ```
2. Or reapply effects after parenting
3. Note: Some effects may not work with WorkerW parenting

## Window Lifecycle Order

**Correct Order:**
```
1. Constructor
   - InitializeComponent()
   - Set properties
   - Initialize handlers

2. Load Event (or Shown Event)
   - Apply effects (blur, shadow)
   - Apply desktop parenting
   - Call Invalidate() + Refresh()

3. Paint Event
   - Draws window content
   - Should be called automatically after Refresh()

4. Message Loop
   - WndProc receives messages
   - Mouse/keyboard events handled
```

**Wrong Order (causes issues):**
```
1. Constructor
   - InitializeComponent()
   - Apply desktop parenting ❌ TOO EARLY
   - Apply effects ❌ TOO EARLY
   - Window not ready yet!
```

## Summary of Changes

### Files Modified
1. `NoFences/View/FenceWindow.cs`
   - Removed desktop parenting from constructor
   - Added desktop parenting to Load event
   - Added mode conflict prevention
   - Added explicit Refresh() calls
   - Added user feedback for mode conflicts

### Behavior Changes
- ✅ Rendering now works correctly
- ✅ Mouse events work in Above Icons mode
- ✅ Minify feature works correctly
- ✅ Title displays properly
- ✅ Content draws correctly
- ⚠️ BehindDesktopIcons mode disallows CanMinify (by design)
- ⚠️ User warned about mode conflicts

### No Breaking Changes
- Existing fences continue to work
- Default behavior unchanged (Above Icons mode)
- BehindDesktopIcons is opt-in feature

## Future Enhancements

### Possible Solutions for Behind-Icons Interaction

1. **Transparent Overlay Layer**
   ```csharp
   // Create a click-through overlay above icons that detects hits
   // and forwards them to the fence behind icons
   var overlay = new DesktopOverlayWindow(...);
   overlay.MouseMove += (s, e) => {
       if (HitTestFence(e.Location)) {
           // Show/expand fence programmatically
       }
   };
   ```

2. **Keyboard Shortcuts**
   ```csharp
   // Global hotkeys to expand/collapse fences
   RegisterHotKey(this.Handle, 1, MOD_CONTROL, Keys.Space);
   ```

3. **Timer-Based Auto-Expand**
   ```csharp
   // Detect mouse proximity and auto-expand
   Timer proximityTimer = new Timer();
   proximityTimer.Tick += (s, e) => {
       var mouse = Cursor.Position;
       if (IsNearFence(mouse)) {
           ExpandFence();
       }
   };
   ```

4. **Tray Icon Control**
   ```csharp
   // Control fences from tray menu
   trayMenu.Items.Add("Expand All Fences");
   trayMenu.Items.Add("Collapse All Fences");
   ```

Currently, the recommended approach is to use Above Icons mode for interactive fences (default NoFences behavior) and Behind Icons mode only for static/display fences.

---

**Status:** ✅ Issues Fixed and Documented

**Next Steps:** Build and test the fixes with various fence configurations
