using System.Drawing;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Defines visual themes for fence appearance.
    /// Each fence can have its own theme for customization.
    /// </summary>
    public enum FenceTheme
    {
        Dark,
        Light,
        DarkBlue,
        DarkGreen
    }

    /// <summary>
    /// Provides color schemes for each fence theme.
    /// </summary>
    public static class FenceThemeColors
    {
        public static FenceThemeDefinition GetTheme(FenceTheme theme)
        {
            switch (theme)
            {
                case FenceTheme.Light:
                    return new FenceThemeDefinition
                    {
                        BackgroundColor = Color.FromArgb(255, 240, 240, 242),           // Light gray
                        BorderColor = Color.FromArgb(150, 100, 100, 110),               // Subtle border
                        TitleBackgroundColor = Color.FromArgb(220, 220, 220, 222),      // Light title bg
                        TitleTextColor = Color.FromArgb(255, 30, 30, 30),               // Dark text
                        ContentBackgroundColor = Color.FromArgb(255, 250, 250, 252),    // Very light content
                        ContentTextColor = Color.FromArgb(255, 20, 20, 20),             // Dark text
                        ContentSecondaryTextColor = Color.FromArgb(255, 100, 100, 100), // Gray secondary
                        ScrollbarColor = Color.FromArgb(255, 180, 180, 182),            // Light scrollbar
                        ScrollbarHoverColor = Color.FromArgb(255, 150, 150, 152)        // Darker on hover
                    };

                case FenceTheme.DarkBlue:
                    return new FenceThemeDefinition
                    {
                        BackgroundColor = Color.FromArgb(255, 20, 25, 35),              // Dark blue tint
                        BorderColor = Color.FromArgb(150, 60, 100, 180),                // Blue border
                        TitleBackgroundColor = Color.FromArgb(200, 25, 35, 50),         // Dark blue title
                        TitleTextColor = Color.FromArgb(255, 220, 230, 255),            // Light blue text
                        ContentBackgroundColor = Color.FromArgb(255, 25, 30, 40),       // Slightly lighter blue
                        ContentTextColor = Color.FromArgb(255, 200, 210, 240),          // Light blue text
                        ContentSecondaryTextColor = Color.FromArgb(255, 140, 150, 180), // Muted blue
                        ScrollbarColor = Color.FromArgb(255, 60, 80, 120),              // Blue scrollbar
                        ScrollbarHoverColor = Color.FromArgb(255, 80, 100, 150)         // Lighter on hover
                    };

                case FenceTheme.DarkGreen:
                    return new FenceThemeDefinition
                    {
                        BackgroundColor = Color.FromArgb(255, 20, 28, 22),              // Dark green tint
                        BorderColor = Color.FromArgb(150, 60, 150, 80),                 // Green border
                        TitleBackgroundColor = Color.FromArgb(200, 22, 32, 25),         // Dark green title
                        TitleTextColor = Color.FromArgb(255, 200, 255, 210),            // Light green text
                        ContentBackgroundColor = Color.FromArgb(255, 25, 33, 27),       // Slightly lighter green
                        ContentTextColor = Color.FromArgb(255, 180, 240, 190),          // Light green text
                        ContentSecondaryTextColor = Color.FromArgb(255, 120, 160, 130), // Muted green
                        ScrollbarColor = Color.FromArgb(255, 50, 100, 60),              // Green scrollbar
                        ScrollbarHoverColor = Color.FromArgb(255, 70, 130, 80)          // Lighter on hover
                    };

                case FenceTheme.Dark:
                default:
                    return new FenceThemeDefinition
                    {
                        BackgroundColor = Color.FromArgb(255, 25, 25, 28),              // Dark gray
                        BorderColor = Color.FromArgb(150, 100, 150, 200),               // Blue border
                        TitleBackgroundColor = Color.FromArgb(200, 20, 20, 22),         // Semi-transparent dark
                        TitleTextColor = Color.FromArgb(255, 255, 255, 255),            // White text
                        ContentBackgroundColor = Color.FromArgb(255, 30, 30, 33),       // Slightly lighter gray
                        ContentTextColor = Color.FromArgb(255, 240, 240, 240),          // Off-white text
                        ContentSecondaryTextColor = Color.FromArgb(255, 160, 160, 160), // Gray secondary
                        ScrollbarColor = Color.FromArgb(255, 80, 80, 85),               // Gray scrollbar
                        ScrollbarHoverColor = Color.FromArgb(255, 100, 100, 110)        // Lighter on hover
                    };
            }
        }

        public static string GetThemeDisplayName(FenceTheme theme)
        {
            switch (theme)
            {
                case FenceTheme.Light:
                    return "Light";
                case FenceTheme.DarkBlue:
                    return "Dark Blue";
                case FenceTheme.DarkGreen:
                    return "Dark Green";
                case FenceTheme.Dark:
                default:
                    return "Dark";
            }
        }
    }

    /// <summary>
    /// Contains all colors for a specific theme - a complete color palette.
    /// </summary>
    public class FenceThemeDefinition
    {
        // Container colors
        public Color BackgroundColor { get; set; }
        public Color BorderColor { get; set; }

        // Title bar colors
        public Color TitleBackgroundColor { get; set; }
        public Color TitleTextColor { get; set; }

        // Content area colors
        public Color ContentBackgroundColor { get; set; }
        public Color ContentTextColor { get; set; }
        public Color ContentSecondaryTextColor { get; set; }  // For secondary info like file size

        // Scrollbar colors (for WPF)
        public Color ScrollbarColor { get; set; }
        public Color ScrollbarHoverColor { get; set; }
    }
}
