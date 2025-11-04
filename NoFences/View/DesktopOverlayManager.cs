using log4net;
using NoFences.Win32.Desktop;
using NoFences.Win32.Window;
using NoFences.Win32.Shell;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using NoFences.Util;

namespace NoFences.View
{
    /// <summary>
    /// Manages a transparent fullscreen overlay on the desktop that can host WPF components
    /// and fence windows. This provides a unified canvas for desktop organization.
    /// </summary>
    public class DesktopOverlayManager : IDisposable
    {
        #region Private Fields

        private static readonly ILog log = LogManager.GetLogger(typeof(DesktopOverlayManager));

        private readonly Dictionary<Screen, DesktopOverlayWindow> _overlayWindows;
        private bool _disposed = false;
        private DesktopMode _currentMode = DesktopMode.AboveIcons;

        #endregion

        #region Events

        /// <summary>
        /// Raised when display settings change (resolution, monitors added/removed)
        /// </summary>
        public event EventHandler<DisplayChangedEventArgs> DisplayChanged;

        #endregion

        #region Constructor

        public DesktopOverlayManager(DesktopMode mode = DesktopMode.AboveIcons)
        {
            _overlayWindows = new Dictionary<Screen, DesktopOverlayWindow>();
            _currentMode = mode;

            // Monitor display changes
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            log.Info($"DesktopOverlayManager initialized in {mode} mode");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes overlay windows for all screens.
        /// </summary>
        public void Initialize()
        {
            CreateOverlayWindows();
        }

        /// <summary>
        /// Gets the overlay window for a specific screen.
        /// </summary>
        public DesktopOverlayWindow GetOverlayForScreen(Screen screen)
        {
            if (_overlayWindows.TryGetValue(screen, out var overlay))
            {
                return overlay;
            }
            return null;
        }

        /// <summary>
        /// Gets the overlay window for the primary screen.
        /// </summary>
        public DesktopOverlayWindow GetPrimaryOverlay()
        {
            return GetOverlayForScreen(Screen.PrimaryScreen);
        }

        /// <summary>
        /// Gets all overlay windows.
        /// </summary>
        public IEnumerable<DesktopOverlayWindow> GetAllOverlays()
        {
            return _overlayWindows.Values;
        }

        /// <summary>
        /// Changes the desktop integration mode.
        /// </summary>
        public void SetDesktopMode(DesktopMode mode)
        {
            if (_currentMode == mode)
                return;

            _currentMode = mode;

            foreach (var overlay in _overlayWindows.Values)
            {
                overlay.SetDesktopMode(mode);
            }

            log.Info($"Desktop mode changed to: {mode}");
        }

        /// <summary>
        /// Shows all overlay windows.
        /// </summary>
        public void ShowOverlays()
        {
            foreach (var overlay in _overlayWindows.Values)
            {
                overlay.Show();
            }
        }

        /// <summary>
        /// Hides all overlay windows.
        /// </summary>
        public void HideOverlays()
        {
            foreach (var overlay in _overlayWindows.Values)
            {
                overlay.Hide();
            }
        }

        /// <summary>
        /// Refreshes all overlay windows (call when desktop changes).
        /// </summary>
        public void RefreshOverlays()
        {
            foreach (var overlay in _overlayWindows.Values)
            {
                overlay.RefreshDesktopIntegration();
            }
        }

        #endregion

        #region Private Methods

        private void CreateOverlayWindows()
        {
            // Clear existing overlays
            foreach (var overlay in _overlayWindows.Values)
            {
                overlay.Close();
                overlay.Dispose();
            }
            _overlayWindows.Clear();

            // Create overlay for each screen
            foreach (Screen screen in Screen.AllScreens)
            {
                var overlay = new DesktopOverlayWindow(screen, _currentMode);
                overlay.Show();
                _overlayWindows[screen] = overlay;

                log.Info($"Created overlay for screen: {screen.DeviceName} at {screen.Bounds}");
            }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            log.Info("Display settings changed, recreating overlays...");

            // Recreate overlays for new display configuration
            CreateOverlayWindows();

            // Raise event for other components
            DisplayChanged?.Invoke(this, new DisplayChangedEventArgs
            {
                Screens = Screen.AllScreens.ToList()
            });
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

                foreach (var overlay in _overlayWindows.Values)
                {
                    overlay.Close();
                    overlay.Dispose();
                }
                _overlayWindows.Clear();
            }

            _disposed = true;
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Represents a transparent fullscreen window for a single screen.
    /// This can host WPF components and other controls.
    /// </summary>
    public class DesktopOverlayWindow : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DesktopOverlayWindow));

        private readonly Screen _screen;
        private DesktopMode _mode;

        public DesktopOverlayWindow(Screen screen, DesktopMode mode)
        {
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _mode = mode;

            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Window style setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = false;

            // Make window transparent
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.Opacity = 1.0;

            // Position and size for screen
            this.Bounds = _screen.Bounds;

            // Enable double buffering for smooth rendering
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);

            // Allow controls to be added
            this.AllowDrop = true;

            log.Info($"DesktopOverlayWindow initialized for screen {_screen.DeviceName}");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Apply desktop integration after window is shown
            ApplyDesktopIntegration();

            // Hide from Alt+Tab
            WindowUtil.HideFromAltTab(this.Handle);
        }

        /// <summary>
        /// Sets the desktop integration mode for this overlay.
        /// </summary>
        public void SetDesktopMode(DesktopMode mode)
        {
            if (_mode == mode)
                return;

            _mode = mode;
            ApplyDesktopIntegration();
        }

        /// <summary>
        /// Refreshes the desktop integration (call when desktop changes).
        /// </summary>
        public void RefreshDesktopIntegration()
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                WorkerWIntegration.RefreshDesktopIntegration(
                    this.Handle,
                    _mode == DesktopMode.BehindIcons);

                // Ensure window is still properly sized
                this.Bounds = _screen.Bounds;
            }
        }

        private void ApplyDesktopIntegration()
        {
            if (!this.IsHandleCreated || this.IsDisposed)
                return;

            try
            {
                if (_mode == DesktopMode.BehindIcons)
                {
                    WorkerWIntegration.ParentToBehindDesktopIcons(this.Handle);
                }
                else
                {
                    WorkerWIntegration.ParentToAboveDesktopIcons(this.Handle);
                }

                // Ensure window covers entire screen
                WorkerWIntegration.PositionWindowForScreen(this.Handle, _screen);

                log.Info($"Desktop integration applied: {_mode} mode");
            }
            catch (Exception ex)
            {
                log.Error($"Error applying desktop integration: {ex.Message}", ex);
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Make window non-interactive (click-through)
            // Remove this if you want the overlay to capture mouse events
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;

            if (m.Msg == WM_NCHITTEST)
            {
                // Make window transparent to mouse clicks
                // This allows clicks to pass through to windows below
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Gets the screen associated with this overlay.
        /// </summary>
        public Screen Screen => _screen;

        /// <summary>
        /// Gets the current desktop mode.
        /// </summary>
        public DesktopMode Mode => _mode;
    }

    /// <summary>
    /// Defines how windows are positioned relative to desktop icons.
    /// </summary>
    public enum DesktopMode
    {
        /// <summary>
        /// Windows appear above desktop icons (traditional NoFences behavior).
        /// </summary>
        AboveIcons,

        /// <summary>
        /// Windows appear behind desktop icons (wallpaper layer, like Lively).
        /// </summary>
        BehindIcons
    }

    /// <summary>
    /// Event args for display change events.
    /// </summary>
    public class DisplayChangedEventArgs : EventArgs
    {
        public List<Screen> Screens { get; set; }
    }

    #endregion
}
