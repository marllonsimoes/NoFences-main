using System;
using System.Collections.Generic;

namespace NoFences.Core.Model
{
    public class FenceInfo
    {
        /* 
         * DO NOT RENAME PROPERTIES. Used for XML serialization.
         */

        public Guid Id { get; set; }

        public string Name { get; set; }

        public int PosX { get; set; }

        public int PosY { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the DPI scaled window height.
        /// </summary>
        public int Height { get; set; }

        public bool Locked { get; set; }

        public bool CanMinify { get; set; }

        /// <summary>
        /// Gets or sets the logical window title height.
        /// </summary>
        public int TitleHeight { get; set; } = 25;

        public string Type { get; set; } = EntryType.Files.ToString();

        public string Path { get; set; }

        public List<string> Items { get; set; } = new List<string>();

        public List<string> Filters { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the new smart filter configuration.
        /// Takes priority over legacy Filters list when not null.
        /// Supports category-based, extension-based, and software-based filtering.
        /// </summary>
        public FileFilter Filter { get; set; } = null;

        public int Interval { get; set; } = 30_000;

        /// <summary>
        /// Gets or sets whether this fence appears behind desktop icons (like a wallpaper)
        /// or above desktop icons (traditional behavior). Default is false (above icons).
        /// </summary>
        public bool BehindDesktopIcons { get; set; } = false;

        /// <summary>
        /// Gets or sets the visual theme for this fence.
        /// Stored as string for XML serialization. Default is "Dark".
        /// </summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>
        /// Gets or sets the corner radius for rounded borders in pixels.
        /// Range: 0 (sharp corners) to 20 (very rounded). Default is 0.
        /// </summary>
        public int CornerRadius { get; set; } = 0;

        /// <summary>
        /// Gets or sets the picture display mode for picture fences.
        /// Stored as string for XML serialization. Default is "Slideshow".
        /// </summary>
        public string PictureDisplayMode { get; set; } = "Slideshow";

        /// <summary>
        /// Gets or sets whether the fence should automatically expand its height
        /// to fit all content (particularly useful for masonry grids).
        /// Default is false (manual sizing).
        /// </summary>
        public bool AutoHeight { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum column width for masonry grid layout in picture fences.
        /// Default is 200 pixels.
        /// </summary>
        public int MasonryMinColumnWidth { get; set; } = 200;

        /// <summary>
        /// Gets or sets the maximum column width for masonry grid layout in picture fences.
        /// Maximum is capped at 1/3 of screen width at runtime.
        /// Default is 400 pixels.
        /// </summary>
        public int MasonryMaxColumnWidth { get; set; } = 400;

        /// <summary>
        /// Gets or sets the maximum number of images to display in masonry grid mode.
        /// Default is 50 images for memory efficiency.
        /// </summary>
        public int MasonryMaxImages { get; set; } = 50;

        /// <summary>
        /// Gets or sets the weather location for clock fences (e.g., "London", "New York", "Tokyo").
        /// If empty, weather will not be displayed.
        /// </summary>
        public string WeatherLocation { get; set; } = "";

        /// <summary>
        /// Gets or sets the OpenWeatherMap API key for weather data.
        /// Users can get a free key at https://openweathermap.org/api
        /// If empty, uses a default demo key (limited usage).
        /// </summary>
        public string WeatherApiKey { get; set; } = "";

        /// <summary>
        /// Gets or sets whether the fence should use fade animations when mouse is not hovering.
        /// When enabled, fence fades to 30% opacity when mouse leaves (if has content).
        /// Default is true (fade enabled).
        /// </summary>
        public bool EnableFadeEffect { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the current video should loop continuously.
        /// Default is true (loop enabled).
        /// </summary>
        public bool VideoLoop { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the playlist should be played in random order.
        /// Default is false (play in order).
        /// </summary>
        public bool VideoShuffle { get; set; } = false;

        /// <summary>
        /// Gets or sets whether videos should only play when mouse hovers over the fence (like Live Photo).
        /// When enabled, video is paused by default and plays only on hover.
        /// Default is false (auto-play).
        /// </summary>
        public bool VideoPlayOnHover { get; set; } = false;

        /// <summary>
        /// Gets or sets the video volume (0-100).
        /// Default is 0 (muted).
        /// </summary>
        public int VideoVolume { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the playlist panel should be visible.
        /// When enabled, shows a side panel with the list of videos.
        /// Default is true (show playlist).
        /// </summary>
        public bool VideoShowPlaylist { get; set; } = true;

        public FenceInfo()
        {

        }

        public FenceInfo(Guid id)
        {
            Id = id;
        }
    }
}
