using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Util;
using NoFences.View.Canvas.Handlers;
using NoFences.Win32.Desktop;
using NoFences.Win32.Window;
using NoFences.Win32.Shell;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NoFences.View.Canvas
{
    /// <summary>
    /// The main canvas window that hosts all fences with WPF content.
    /// This is a single transparent fullscreen window parented to the desktop.
    /// All fences are UserControls hosted within this window, using WPF for content rendering.
    ///
    /// This is part of the NEW canvas-based architecture with WPF integration.
    /// For the original Form-per-fence approach, see FenceWindow.cs
    /// </summary>
    public class DesktopCanvas : Form
    {
        #region Private Fields

        private static readonly ILog log = LogManager.GetLogger(typeof(DesktopCanvas));

        private readonly Dictionary<Guid, FenceContainer> fenceContainers;
        private readonly FenceHandlerFactoryWpf handlerFactory;
        private bool useWorkerWIntegration = false;
        private System.Windows.Forms.Timer zOrderEnforcementTimer;

        #endregion

        #region Win32 APIs

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        #endregion

        #region Events

        public event EventHandler<FenceInfo> FenceChanged;
        public event EventHandler<FenceInfo> FenceDeleted;

        #endregion

        #region Constructor

        public DesktopCanvas(FenceHandlerFactoryWpf handlerFactory, bool useWorkerW = false)
        {
            this.handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            this.useWorkerWIntegration = useWorkerW;
            this.fenceContainers = new Dictionary<Guid, FenceContainer>();

            InitializeWindow();

            log.Debug($"DesktopCanvas created (WPF, WorkerW: {useWorkerW})");
        }

        #endregion

        #region Initialization

        private void InitializeWindow()
        {
            // Window style - transparent, borderless, fullscreen
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false; // Hidden from taskbar for production use
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = false; // Don't stay on top - let desktop integration handle z-order

            // Use pure black as TransparencyKey to avoid purple/magenta tint during fades
            // Fence theme backgrounds use slightly off-black colors (RGB(30,30,33) etc.) so they won't become transparent
            // Images with pure black pixels may have transparency issues, but this is better than purple tint
            // The minimum alpha enforcement in FadeWpfElementBackgrounds ensures backgrounds never reach pure black
            this.BackColor = Color.FromArgb(255, 0, 0, 0);  // Pure black
            this.TransparencyKey = Color.FromArgb(255, 0, 0, 0);  // Pure black becomes transparent
            this.Opacity = 1.0;

            // Cover primary screen (for now - can be extended to multi-monitor)
            this.Bounds = Screen.PrimaryScreen.Bounds;

            // Enable double buffering
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);

            this.DoubleBuffered = true;

            // Event handlers
            this.Load += DesktopCanvas_Load;
            this.FormClosing += DesktopCanvas_FormClosing;

            // Monitor display changes
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            // Add a visible test label to verify canvas is rendering
#if DEBUG
            AddTestVisibilityIndicator();
#endif
        }

#if DEBUG
        private void AddTestVisibilityIndicator()
        {
            // Add a bright test label to verify the canvas is visible
            var testLabel = new Label
            {
                Text = "CANVAS VISIBLE - Desktop Integration Working",
                AutoSize = false,
                Size = new Size(400, 80),
                Location = new Point(100, 100),
                BackColor = Color.FromArgb(255, 0, 128, 255), // Bright blue (NOT Magenta)
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true // CRITICAL: Explicitly set visible
            };

            this.Controls.Add(testLabel);
            testLabel.BringToFront();

            log.Debug($"Test visibility indicator added - Visible={testLabel.Visible}");
        }
#endif

        #endregion

        #region Event Handlers

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Apply desktop integration IMMEDIATELY when handle is created
            // This ensures correct Z-order from the very first frame
            try
            {
                // Temporarily disable desktop integration to test visibility
#if DEBUG
                bool skipDesktopIntegration = false; // Set to true to test without desktop integration

                if (skipDesktopIntegration)
                {
                    log.Debug("Skipping desktop integration for testing");
                    log.Debug($"Canvas Handle: 0x{this.Handle:X}");
                    log.Debug($"Canvas Bounds: {this.Bounds}");
                    log.Debug($"Canvas TopMost: {this.TopMost}");
                    return;
                }
#endif

                log.Debug($"OnHandleCreated: Applying desktop integration (WorkerW: {useWorkerWIntegration})...");
                log.Debug($"Canvas state BEFORE integration: Handle=0x{this.Handle:X}, Bounds={this.Bounds}");

                // NOTE: Blur disabled - it blurs the canvas's Magenta background, creating purple tint
                // To properly blur the desktop behind fences, each fence would need to be a top-level window
                // Instead, we use semi-transparent backgrounds for a modern look without performance cost
                // See: https://github.com/issues/blur-architecture-discussion

                if (useWorkerWIntegration)
                {
                    // New WorkerW approach
                    log.Info("Using WorkerW desktop integration...");

                    // Hide from Alt+Tab
                    WindowUtil.HideFromAltTab(this.Handle);
                    log.Debug("Hidden from Alt+Tab");

                    // Parent to desktop (above icons for now, to keep mouse events working)
                    DesktopUtilNew.GlueToDesktop(this.Handle, behindIcons: false);
                    log.Debug("Parented to desktop using WorkerW (above icons)");
                }
                else
                {
                    // Legacy approach - simpler and reliable
                    log.Info("Using legacy desktop integration...");

                    WindowUtil.HideFromAltTab(this.Handle);
                    DesktopUtilNew.PreventMinimize(this.Handle);

                    // Use the legacy method from DesktopUtilNew
                    DesktopUtilNew.GlueToDesktopLegacy(this.Handle);

                    log.Debug("Legacy desktop integration applied (Progman parenting)");
                }

                log.Debug($"Canvas state AFTER integration: Controls={this.Controls.Count}");
                log.Debug("Desktop integration completed successfully in OnHandleCreated");

                // Start z-order enforcement timer
                // This periodically re-enforces HWND_BOTTOM in case other apps try to change our z-order
                StartZOrderEnforcementTimer();
            }
            catch (Exception ex)
            {
                log.Error($"ERROR in OnHandleCreated: {ex.Message}", ex);
            }
        }

        private void StartZOrderEnforcementTimer()
        {
            zOrderEnforcementTimer = new System.Windows.Forms.Timer();
            zOrderEnforcementTimer.Interval = 2000; // Check every 2 seconds
            zOrderEnforcementTimer.Tick += ZOrderEnforcementTimer_Tick;
            zOrderEnforcementTimer.Start();
            log.Debug("Z-order enforcement timer started (2 second interval)");
        }

        private void ZOrderEnforcementTimer_Tick(object sender, EventArgs e)
        {
            // Periodically re-enforce HWND_BOTTOM to keep window below all apps
            try
            {
                IntPtr HWND_BOTTOM = new IntPtr(1);
                SetWindowPos(
                    this.Handle,
                    HWND_BOTTOM,
                    0, 0, 0, 0,
                    SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            }
            catch (Exception ex)
            {
                log.Error($"Error enforcing z-order: {ex.Message}", ex);
            }
        }

        private void DesktopCanvas_Load(object sender, EventArgs e)
        {
            // Desktop integration now happens in OnHandleCreated (before window is shown)
            // This event can be used for other initialization if needed
            log.Debug($"DesktopCanvas_Load: Window is now visible with {this.Controls.Count} controls");

            // Force refresh
            this.Invalidate();
            this.Refresh();
        }

        private void DesktopCanvas_FormClosing(object sender, FormClosingEventArgs e)
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            // Stop and dispose z-order enforcement timer
            if (zOrderEnforcementTimer != null)
            {
                zOrderEnforcementTimer.Stop();
                zOrderEnforcementTimer.Dispose();
                zOrderEnforcementTimer = null;
            }

            // Cleanup all fences
            //foreach (var container in fenceContainers.Values.ToList())
            //{
            //    RemoveFence(container.FenceInfo.Id);
            //}
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            try
            {
                // Resize canvas to new screen size
                this.Bounds = Screen.PrimaryScreen.Bounds;

                // Refresh desktop integration
                if (useWorkerWIntegration)
                {
                    WorkerWIntegration.RefreshDesktopIntegration(this.Handle, behindIcons: false);
                }

                this.Invalidate();
                this.Refresh();

                log.Debug("DesktopCanvas refreshed after display change");
            }
            catch (Exception ex)
            {
                log.Error($"Error refreshing canvas after display change: {ex.Message}", ex);
            }
        }

        #endregion

        #region Public Methods - Fence Management

        public void AddFence(FenceInfo fenceInfo)
        {
            if (fenceInfo == null)
                throw new ArgumentNullException(nameof(fenceInfo));

            if (fenceContainers.ContainsKey(fenceInfo.Id))
            {
                log.Debug($"Fence {fenceInfo.Id} already exists, skipping");
                return;
            }

            try
            {
                // Validate and correct fence position to ensure it's within screen bounds
                EnsureFenceWithinBounds(fenceInfo);

                var container = new FenceContainer(fenceInfo, handlerFactory);

                // Wire up events
                container.FenceChanged += (s, info) => FenceChanged?.Invoke(this, info);
                container.FenceDeleted += (s, info) => RemoveFence(info.Id);
                container.FenceEdited += Container_FenceEdited;

                // CRITICAL: Explicitly set visibility BEFORE adding to canvas
                // If we add to invisible parent, control defaults to invisible and stays that way
                container.Visible = true;

                // Add to canvas
                this.Controls.Add(container);
                container.BringToFront();

                fenceContainers[fenceInfo.Id] = container;

                log.Debug($"Fence added to canvas (WPF): {fenceInfo.Name}");
                log.Debug($"  - Canvas controls count: {this.Controls.Count}");
                log.Debug($"  - Container visible (should be TRUE): {container.Visible}");
                log.Debug($"  - Container bounds: {container.Bounds}");
                log.Debug($"  - Canvas bounds: {this.Bounds}");
                log.Debug($"  - Canvas visible: {this.Visible}");
            }
            catch (Exception ex)
            {
                log.Error($"Error adding fence to canvas: {ex.Message}", ex);
            }
        }

        public void RemoveFence(Guid fenceId)
        {
            if (fenceContainers.TryGetValue(fenceId, out var container))
            {
                this.Controls.Remove(container);
                container.Dispose();
                fenceContainers.Remove(fenceId);

                FenceDeleted?.Invoke(this, container.FenceInfo);

                log.Debug($"Fence removed from canvas: {container.FenceInfo.Name}");
            }
        }

        public FenceContainer GetFence(Guid fenceId)
        {
            fenceContainers.TryGetValue(fenceId, out var container);
            return container;
        }

        public IEnumerable<FenceContainer> GetAllFences()
        {
            return fenceContainers.Values;
        }

        public int FenceCount => fenceContainers.Count;

        #endregion

        #region Private Methods

        private void Container_FenceEdited(object sender, FenceInfo fenceInfo)
        {
            // FenceContainer already handled the edit dialog internally (FenceEditWindow)
            // This event is just a notification that editing was completed
            // Just pass it through to any listeners
            FenceChanged?.Invoke(this, fenceInfo);
        }

        #endregion

        #region WndProc Override

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_NOACTIVATE - Prevents window from being activated when clicked
                // This keeps the window from stealing focus from other applications
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Intercept WM_WINDOWPOSCHANGING to prevent window from being moved above other windows
            const int WM_WINDOWPOSCHANGING = 0x0046;

            if (m.Msg == WM_WINDOWPOSCHANGING)
            {
                // Prevent any attempt to move this window above other windows
                // This enforces that we stay at HWND_BOTTOM
                try
                {
                    // WINDOWPOS structure
                    var windowPos = (WINDOWPOS)Marshal.PtrToStructure(m.LParam, typeof(WINDOWPOS));

                    const uint SWP_NOZORDER = 0x0004;

                    // If someone is trying to change our z-order, force HWND_BOTTOM
                    if ((windowPos.flags & SWP_NOZORDER) == 0)
                    {
                        windowPos.hwndInsertAfter = new IntPtr(1); // HWND_BOTTOM
                        Marshal.StructureToPtr(windowPos, m.LParam, true);
                    }
                }
                catch
                {
                    // If marshaling fails, just continue
                }
            }

            // Handle right-click FIRST before HTTRANSPARENT check
            // This allows right-click to work on empty canvas areas
            const int WM_RBUTTONUP = 0x0205;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_CONTEXTMENU = 0x007B;

            if (m.Msg == WM_RBUTTONDOWN || m.Msg == WM_RBUTTONUP || m.Msg == WM_CONTEXTMENU)
            {
                // Check if we're over a fence
                var pos = this.PointToClient(Cursor.Position);
                bool overFence = false;

                foreach (var fence in fenceContainers.Values)
                {
                    if (fence.Bounds.Contains(pos) && fence.Visible)
                    {
                        overFence = true;
                        break;
                    }
                }

                if (!overFence)
                {
                    // Not over a fence - show desktop context menu
                    log.Debug($"DesktopCanvas: Right-click on empty area at {Cursor.Position}, showing desktop menu");
                    ShowDesktopContextMenu();
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            // Allow click-through for the canvas itself
            // Fences (child controls) will still receive events
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;
            const int HTCLIENT = 1;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);

                if ((int)m.Result == HTCLIENT)
                {
                    // Check if we're over a fence
                    var pos = this.PointToClient(Cursor.Position);
                    bool overFence = false;

                    foreach (var fence in fenceContainers.Values)
                    {
                        if (fence.Bounds.Contains(pos) && fence.Visible)
                        {
                            // Over a fence, allow normal hit testing
#if DEBUG
                            log.Debug($"WndProc: Over fence '{fence.FenceInfo.Name}' at {pos}, allowing hit test");
#endif
                            overFence = true;
                            break;
                        }
                    }

                    if (overFence)
                    {
                        return; // Allow hit testing for the fence
                    }

                    // Not over a fence, make click-through
                    m.Result = (IntPtr)HTTRANSPARENT;
                }
                return;
            }

            // Always prevent activation - WS_EX_NOACTIVATE handles this but reinforce it here
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 3;
            const int MA_NOACTIVATEANDEAT = 4;

            if (m.Msg == WM_MOUSEACTIVATE)
            {
                // Never activate the canvas window, even when clicking fences
                // This prevents the canvas from stealing focus from other apps
                // Child controls (fences) will still receive mouse events
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        /// <summary>
        /// Ensures a fence is positioned within screen bounds with at least 50 pixels visible.
        /// Corrects position if the fence would be hidden or mostly off-screen.
        /// </summary>
        private void EnsureFenceWithinBounds(FenceInfo fenceInfo)
        {
            const int MinVisible = 50;
            Rectangle screenBounds = this.ClientRectangle;

            bool positionCorrected = false;
            int originalX = fenceInfo.PosX;
            int originalY = fenceInfo.PosY;

            // Ensure X position keeps fence visible
            if (fenceInfo.PosX < -fenceInfo.Width + MinVisible)
            {
                fenceInfo.PosX = -fenceInfo.Width + MinVisible;
                positionCorrected = true;
            }
            if (fenceInfo.PosX > screenBounds.Width - MinVisible)
            {
                fenceInfo.PosX = screenBounds.Width - MinVisible;
                positionCorrected = true;
            }

            // Ensure Y position keeps fence visible
            if (fenceInfo.PosY < 0)
            {
                fenceInfo.PosY = 0;
                positionCorrected = true;
            }
            if (fenceInfo.PosY > screenBounds.Height - MinVisible)
            {
                fenceInfo.PosY = screenBounds.Height - MinVisible;
                positionCorrected = true;
            }

            if (positionCorrected)
            {
                log.Info($"Fence '{fenceInfo.Name}' position corrected: ({originalX},{originalY}) → ({fenceInfo.PosX},{fenceInfo.PosY})");
            }
        }

        /// <summary>
        /// Shows the desktop context menu at the current cursor position.
        /// Forwards the right-click to the desktop shell so it displays its native context menu.
        /// </summary>
        private void ShowDesktopContextMenu()
        {
            try
            {
                // Get the desktop ListView window (desktop icons)
                // Desktop hierarchy: Progman -> SHELLDLL_DefView -> SysListView32
                IntPtr progman = FindWindow("Progman", null);
                if (progman == IntPtr.Zero)
                {
                    log.Warn(" Could not find Progman window");
                    return;
                }

                IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView == IntPtr.Zero)
                {
                    log.Warn("Could not find SHELLDLL_DefView");
                    return;
                }

                IntPtr sysListView = FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
                if (sysListView == IntPtr.Zero)
                {
                    log.Warn("Could not find SysListView32");
                    return;
                }

                // Post the context menu message (WM_CONTEXTMENU) to the desktop icons window
                // Format: lParam = MAKELONG(x, y) in screen coordinates
                int x = Cursor.Position.X;
                int y = Cursor.Position.Y;
                IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));

                PostMessage(sysListView, 0x007B, IntPtr.Zero, lParam);
                log.Debug($"Posted context menu message to desktop at ({x}, {y})");
            }
            catch (Exception ex)
            {
                log.Error($" Error - {ex.Message}", ex);
            }
        }

        #endregion
    }
}
