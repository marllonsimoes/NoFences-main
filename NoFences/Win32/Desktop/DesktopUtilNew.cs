using System;
using System.Runtime.InteropServices;

namespace NoFences.Win32.Desktop
{
    /// <summary>
    /// Desktop integration utilities for the canvas-based architecture.
    /// Provides methods to manage window styles and prevent minimize/maximize behavior.
    /// </summary>
    public static class DesktopUtilNew
    {
        #region P/Invoke Declarations

        private const Int32 GWL_STYLE = -16;
        private const Int32 GWL_HWNDPARENT = -8;
        private const Int32 WS_MAXIMIZEBOX = 0x00010000;
        private const Int32 WS_MINIMIZEBOX = 0x00020000;

        [DllImport("User32.dll", EntryPoint = "GetWindowLong")]
        private extern static Int32 GetWindowLongPtr(IntPtr hWnd, Int32 nIndex);

        [DllImport("User32.dll", EntryPoint = "SetWindowLong")]
        private extern static Int32 SetWindowLongPtr(IntPtr hWnd, Int32 nIndex, Int32 dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        #endregion

        #region Public Methods

        /// <summary>
        /// Prevents minimize/maximize buttons on a window.
        /// </summary>
        public static void PreventMinimize(IntPtr handle)
        {
            Int32 windowStyle = GetWindowLongPtr(handle, GWL_STYLE);
            SetWindowLongPtr(handle, GWL_STYLE, windowStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX);
        }

        /// <summary>
        /// Glues a window to the desktop using the NEW WorkerW-based approach.
        /// This supports both above and behind desktop icons modes.
        /// </summary>
        /// <param name="handle">Window handle to glue to desktop</param>
        /// <param name="behindIcons">If true, positions behind desktop icons (like wallpaper). If false, positions above icons.</param>
        public static void GlueToDesktop(IntPtr handle, bool behindIcons = false)
        {
            if (behindIcons)
            {
                // Use the WorkerW method to position behind desktop icons
                WorkerWIntegration.ParentToBehindDesktopIcons(handle);
            }
            else
            {
                // Use the WorkerW method to position above desktop icons
                WorkerWIntegration.ParentToAboveDesktopIcons(handle);
            }
        }

        /// <summary>
        /// Legacy Progman-based method for compatibility.
        /// This is the simple approach that doesn't use WorkerW.
        /// </summary>
        public static void GlueToDesktopLegacy(IntPtr handle)
        {
            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
            {
                SetParent(handle, progman);
            }
        }

        #endregion
    }
}
