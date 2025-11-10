using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media;

namespace NoFences.Util
{
    /// <summary>
    /// Manages theme colors for WPF windows based on Windows system theme.
    /// Provides automatic dark/light mode detection and color constants.
    /// </summary>
    public static class ThemeManager
    {
        #region Dark Theme Colors
        public static readonly Color DarkBackground = Color.FromRgb(30, 30, 30);           // #1E1E1E
        public static readonly Color DarkForeground = Colors.White;
        public static readonly Color DarkAccent = Color.FromRgb(0, 122, 204);              // #007ACC
        public static readonly Color DarkBorderColor = Color.FromRgb(60, 60, 60);          // #3C3C3C
        public static readonly Color DarkCardBackground = Color.FromRgb(45, 45, 48);       // #2D2D30
        #endregion

        #region Light Theme Colors
        public static readonly Color LightBackground = Colors.White;                        // #FFFFFF
        public static readonly Color LightForeground = Colors.Black;
        public static readonly Color LightAccent = Color.FromRgb(0, 120, 212);             // #0078D4
        public static readonly Color LightBorderColor = Color.FromRgb(200, 200, 200);      // #C8C8C8
        public static readonly Color LightCardBackground = Color.FromRgb(245, 245, 245);   // #F5F5F5
        #endregion

        /// <summary>
        /// Detects if Windows is in dark mode by checking registry.
        /// </summary>
        /// <returns>True if dark mode is enabled, false for light mode.</returns>
        public static bool IsWindowsDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key?.GetValue("AppsUseLightTheme");
                    return value is int i && i == 0; // 0 = dark mode, 1 = light mode
                }
            }
            catch (Exception)
            {
                return false; // Default to light mode on error
            }
        }

        /// <summary>
        /// Gets the appropriate background color based on Windows theme.
        /// </summary>
        public static Color GetBackgroundColor()
        {
            return IsWindowsDarkMode() ? DarkBackground : LightBackground;
        }

        /// <summary>
        /// Gets the appropriate foreground color based on Windows theme.
        /// </summary>
        public static Color GetForegroundColor()
        {
            return IsWindowsDarkMode() ? DarkForeground : LightForeground;
        }

        /// <summary>
        /// Gets the appropriate accent color based on Windows theme.
        /// </summary>
        public static Color GetAccentColor()
        {
            return IsWindowsDarkMode() ? DarkAccent : LightAccent;
        }

        /// <summary>
        /// Gets the appropriate border color based on Windows theme.
        /// </summary>
        public static Color GetBorderColor()
        {
            return IsWindowsDarkMode() ? DarkBorderColor : LightBorderColor;
        }

        /// <summary>
        /// Gets the appropriate card/panel background color based on Windows theme.
        /// </summary>
        public static Color GetCardBackgroundColor()
        {
            return IsWindowsDarkMode() ? DarkCardBackground : LightCardBackground;
        }

        /// <summary>
        /// Gets the appropriate background brush based on Windows theme.
        /// </summary>
        public static SolidColorBrush GetBackgroundBrush()
        {
            return new SolidColorBrush(GetBackgroundColor());
        }

        /// <summary>
        /// Gets the appropriate foreground brush based on Windows theme.
        /// </summary>
        public static SolidColorBrush GetForegroundBrush()
        {
            return new SolidColorBrush(GetForegroundColor());
        }

        /// <summary>
        /// Gets the appropriate accent brush based on Windows theme.
        /// </summary>
        public static SolidColorBrush GetAccentBrush()
        {
            return new SolidColorBrush(GetAccentColor());
        }

        /// <summary>
        /// Gets the appropriate border brush based on Windows theme.
        /// </summary>
        public static SolidColorBrush GetBorderBrush()
        {
            return new SolidColorBrush(GetBorderColor());
        }

        /// <summary>
        /// Gets the appropriate card/panel background brush based on Windows theme.
        /// </summary>
        public static SolidColorBrush GetCardBackgroundBrush()
        {
            return new SolidColorBrush(GetCardBackgroundColor());
        }

        /// <summary>
        /// Applies the current system theme to a WPF Window.
        /// Sets Background, Foreground, and BorderBrush properties.
        /// </summary>
        /// <param name="window">The window to apply theme to.</param>
        public static void ApplyTheme(Window window)
        {
            if (window == null) return;

            window.Background = GetBackgroundBrush();
            window.Foreground = GetForegroundBrush();
            window.BorderBrush = GetBorderBrush();
        }

        /// <summary>
        /// Applies the current system theme to a FrameworkElement.
        /// Sets Background and Foreground properties if they exist.
        /// </summary>
        /// <param name="element">The element to apply theme to.</param>
        public static void ApplyTheme(FrameworkElement element)
        {
            if (element == null) return;

            // Use reflection to set Background if property exists
            var backgroundProperty = element.GetType().GetProperty("Background");
            if (backgroundProperty != null && backgroundProperty.CanWrite)
            {
                backgroundProperty.SetValue(element, GetBackgroundBrush());
            }

            // Use reflection to set Foreground if property exists
            var foregroundProperty = element.GetType().GetProperty("Foreground");
            if (foregroundProperty != null && foregroundProperty.CanWrite)
            {
                foregroundProperty.SetValue(element, GetForegroundBrush());
            }
        }
    }
}
