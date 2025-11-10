using log4net;
using NoFences.Util;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NoFences.Win32.Desktop
{
    /// <summary>
    /// Provides Windows desktop integration using the WorkerW method.
    /// This technique allows windows to be positioned in the desktop layer,
    /// similar to how Lively and other wallpaper applications work.
    /// </summary>
    public static class WorkerWIntegration
    {
        #region P/Invoke Declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(
            IntPtr parentHandle,
            IntPtr hWndChildAfter,
            string className,
            string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            SendMessageTimeoutFlags fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        #region Constants and Enums

        private const uint WM_SPAWN_WORKER = 0x052C;

        [Flags]
        private enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }

        [Flags]
        private enum SetWindowPosFlags : uint
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOACTIVATE = 0x0010,
            SWP_SHOWWINDOW = 0x0040,
            SWP_FRAMECHANGED = 0x0020,
        }

        #endregion

        #region Private Fields

        private static readonly ILog log = LogManager.GetLogger(typeof(WorkerWIntegration));

        private static IntPtr? _cachedWorkerW = null;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheTimeout = TimeSpan.FromSeconds(5);

        #endregion

        #region Public Methods

        /// <summary>
        /// Positions a window behind desktop icons using the WorkerW method.
        /// </summary>
        /// <param name="windowHandle">Handle of the window to position</param>
        public static void ParentToBehindDesktopIcons(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(windowHandle));

            IntPtr workerw = GetWorkerW();

            if (workerw == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not find or create WorkerW window");
            }

            // Parent the window to WorkerW
            SetParent(windowHandle, workerw);

            log.Debug($"Window {windowHandle} parented to WorkerW {workerw} (behind desktop icons)");
        }

        /// <summary>
        /// Positions a window above desktop icons but below normal windows.
        /// This is the traditional NoFences behavior.
        /// Instead of parenting, we use window styles to keep it at desktop level.
        /// </summary>
        /// <param name="windowHandle">Handle of the window to position</param>
        public static void ParentToAboveDesktopIcons(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(windowHandle));

            // For "above icons" mode, we need to ensure the window stays BELOW normal app windows
            // Strategy:
            // 1. Add WS_EX_TOOLWINDOW style to prevent taskbar appearance and lower Z-order priority
            // 2. Use HWND_BOTTOM to position at bottom of Z-order (above desktop, below apps)
            // 3. Call SetWindowPos multiple times to force Windows to accept it

            const int GWL_EXSTYLE = -20;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;

            // Get current extended styles
            int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);

            // Add TOOLWINDOW and NOACTIVATE styles
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLong(windowHandle, GWL_EXSTYLE, exStyle);

            log.Debug($"Window {windowHandle} extended styles updated: WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE");

            // Position at bottom of Z-order (above desktop, below all normal windows)
            IntPtr HWND_BOTTOM = new IntPtr(1);

            // Call SetWindowPos multiple times to really force it
            // Some applications fight for z-order, so we need to be aggressive
            for (int i = 0; i < 5; i++)
            {
                SetWindowPos(
                    windowHandle,
                    HWND_BOTTOM,  // Bottom of Z-order
                    0, 0, 0, 0,
                    SetWindowPosFlags.SWP_NOSIZE |
                    SetWindowPosFlags.SWP_NOMOVE |
                    SetWindowPosFlags.SWP_NOACTIVATE |
                    SetWindowPosFlags.SWP_FRAMECHANGED);

                // Small delay between calls
                if (i < 4)
                    System.Threading.Thread.Sleep(10);
            }

            log.Debug($"Window {windowHandle} positioned at HWND_BOTTOM (above desktop icons, below normal windows) - called 5 times");
        }

        /// <summary>
        /// Gets the WorkerW window handle, creating it if necessary.
        /// Uses caching to avoid repeated window enumeration.
        /// </summary>
        public static IntPtr GetWorkerW(bool forceRefresh = false)
        {
            // Check cache
            if (!forceRefresh &&
                _cachedWorkerW.HasValue &&
                _cachedWorkerW.Value != IntPtr.Zero &&
                DateTime.Now - _cacheTime < CacheTimeout)
            {
                return _cachedWorkerW.Value;
            }

            // Find the Progman window (Program Manager)
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                log.Error("Error: Could not find Progman window");
                return IntPtr.Zero;
            }

            // Send the magic message to spawn WorkerW
            // This is an undocumented message that causes Windows Explorer
            // to create a WorkerW window between the wallpaper and desktop icons
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(
                progman,
                WM_SPAWN_WORKER,
                IntPtr.Zero,
                IntPtr.Zero,
                SendMessageTimeoutFlags.SMTO_NORMAL,
                1000,
                out result);

            // Now find the WorkerW window that was created
            IntPtr workerw = FindWorkerW();

            // Cache the result
            _cachedWorkerW = workerw;
            _cacheTime = DateTime.Now;

            log.Debug($"WorkerW found/created: {workerw}");

            return workerw;
        }

        /// <summary>
        /// Refreshes the desktop integration for a window.
        /// Call this when display settings change or desktop is refreshed.
        /// </summary>
        /// <param name="windowHandle">Handle of the window to refresh</param>
        /// <param name="behindIcons">True to position behind icons, false for above</param>
        public static void RefreshDesktopIntegration(IntPtr windowHandle, bool behindIcons)
        {
            if (windowHandle == IntPtr.Zero)
                return;

            // Clear cache to force re-discovery of WorkerW
            _cachedWorkerW = null;

            if (behindIcons)
            {
                ParentToBehindDesktopIcons(windowHandle);
            }
            else
            {
                ParentToAboveDesktopIcons(windowHandle);
            }

            log.Debug($"Desktop integration refreshed for window {windowHandle}");
        }

        /// <summary>
        /// Removes desktop parenting, returning window to normal z-order.
        /// </summary>
        /// <param name="windowHandle">Handle of the window to unparent</param>
        public static void UnparentFromDesktop(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return;

            // Unparent by setting parent to null
            SetParent(windowHandle, IntPtr.Zero);

            log.Debug($"Window {windowHandle} unparented from desktop");
        }

        /// <summary>
        /// Positions and sizes a window for a specific screen.
        /// </summary>
        public static void PositionWindowForScreen(IntPtr windowHandle, Screen screen)
        {
            if (windowHandle == IntPtr.Zero || screen == null)
                return;

            SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                screen.Bounds.X,
                screen.Bounds.Y,
                screen.Bounds.Width,
                screen.Bounds.Height,
                SetWindowPosFlags.SWP_NOZORDER |
                SetWindowPosFlags.SWP_NOACTIVATE |
                SetWindowPosFlags.SWP_SHOWWINDOW);

            log.Debug($"Window {windowHandle} positioned for screen at {screen.Bounds}");
        }

        /// <summary>
        /// Gets information about the current desktop integration status.
        /// </summary>
        public static DesktopIntegrationInfo GetIntegrationInfo()
        {
            var progman = FindWindow("Progman", null);
            var workerw = _cachedWorkerW ?? IntPtr.Zero;

            return new DesktopIntegrationInfo
            {
                ProgmanHandle = progman,
                WorkerWHandle = workerw,
                WorkerWCached = _cachedWorkerW.HasValue,
                CacheAge = _cachedWorkerW.HasValue ? DateTime.Now - _cacheTime : TimeSpan.Zero
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Enumerates windows to find the WorkerW that sits behind desktop icons.
        /// This follows the same approach as Lively wallpaper.
        /// </summary>
        private static IntPtr FindWorkerW()
        {
            IntPtr workerw = IntPtr.Zero;

            // Enumerate all top-level windows
            EnumWindows((tophandle, topparamhandle) =>
            {
                // Find the WorkerW window that contains SHELLDLL_DefView
                IntPtr shelldll = FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", null);

                if (shelldll != IntPtr.Zero)
                {
                    // This is the WorkerW that contains SHELLDLL_DefView (desktop icons)
                    // This is the correct window to parent to for positioning BEHIND icons
                    // (between wallpaper and desktop icons layer)
                    workerw = tophandle;

                    log.Debug($"Found WorkerW with SHELLDLL_DefView: {workerw:X} (shelldll: {shelldll:X})");

                    // Stop enumeration, we found what we need
                    return false;
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            if (workerw == IntPtr.Zero)
            {
                log.Warn("Could not find WorkerW window with SHELLDLL_DefView");
            }

            return workerw;
        }

        #endregion

        #region Supporting Classes

        /// <summary>
        /// Information about the desktop integration status.
        /// </summary>
        public class DesktopIntegrationInfo
        {
            public IntPtr ProgmanHandle { get; set; }
            public IntPtr WorkerWHandle { get; set; }
            public bool WorkerWCached { get; set; }
            public TimeSpan CacheAge { get; set; }

            public override string ToString()
            {
                return $"Progman: {ProgmanHandle}, WorkerW: {WorkerWHandle}, " +
                       $"Cached: {WorkerWCached}, Age: {CacheAge.TotalSeconds:F1}s";
            }
        }

        #endregion
    }
}
