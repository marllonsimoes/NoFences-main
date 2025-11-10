using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Util;
using NoFences.View.Canvas.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace NoFences.View.Canvas.Handlers
{
    /// <summary>
    /// WPF-based handler for displaying pictures in a fence with multiple display modes:
    /// - Slideshow: Single image rotating through all images
    /// - MasonryGrid: True masonry layout with varied image sizes and lazy loading
    /// - Hybrid: Grid layout with images that rotate periodically
    /// This is part of the NEW canvas-based architecture.
    ///
    /// For the original WinForms version, see View/Fences/Handlers/PictureFenceHandler.cs
    /// </summary>
    public class PictureFenceHandlerWpf : IFenceHandlerWpf
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PictureFenceHandlerWpf));

        private FenceInfo fenceInfo;
        private PictureDisplayMode displayMode;

        // Slideshow mode
        private Image imageControl;
        private string currentPicture;

        // Grid modes (MasonryGrid and Hybrid)
        private MasonryPanel masonryPanel;
        private List<LazyImage> lazyImages;
        private HashSet<string> displayedImages; // Track displayed images to prevent duplicates
        private Random random = new Random();

        private DispatcherTimer timer;
        private readonly string[] validExtensions = new[] { ".jpg", ".png", ".gif", ".jpeg", ".bmp" };

        // Note: MaxVisibleImages and HybridGridSize are now configurable via fenceInfo.MasonryMaxImages
        // Defaults to 50 if not set

        // Event raised when content changes (for auto-height)
        public event EventHandler ContentChanged;

        public void Initialize(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));

            // Parse display mode
            if (!string.IsNullOrEmpty(fenceInfo.PictureDisplayMode) &&
                Enum.TryParse<PictureDisplayMode>(fenceInfo.PictureDisplayMode, out var mode))
            {
                displayMode = mode;
            }
            else
            {
                displayMode = PictureDisplayMode.Slideshow; // Default
            }

            log.Debug($"Initialized with mode {displayMode}");
        }

        public UIElement CreateContentElement(int titleHeight, FenceThemeDefinition theme)
        {
            log.Debug($"Creating content element with mode {displayMode}");

            var contentBg = new SolidColorBrush(Color.FromArgb(
                theme.ContentBackgroundColor.A,
                theme.ContentBackgroundColor.R,
                theme.ContentBackgroundColor.G,
                theme.ContentBackgroundColor.B));

            UIElement content;

            switch (displayMode)
            {
                case PictureDisplayMode.Slideshow:
                    content = CreateSlideshowContent(contentBg);
                    break;

                case PictureDisplayMode.MasonryGrid:
                    content = CreateMasonryGridContent(contentBg);
                    break;

                case PictureDisplayMode.Hybrid:
                    content = CreateHybridContent(contentBg);
                    break;

                default:
                    content = CreateSlideshowContent(contentBg);
                    break;
            }

            var border = new Border
            {
                Background = contentBg,
                Child = content
            };

            return border;
        }

        private UIElement CreateSlideshowContent(Brush background)
        {
            // Create single image control
            imageControl = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Load first picture
            SetNextSlidePicture();
            LoadCurrentPicture();

            log.Debug($"Loading picture: {currentPicture}");

            // Setup timer for slideshow if multiple images
            if (fenceInfo.Items?.Count > 1)
            {
                timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(fenceInfo.Interval > 0 ? fenceInfo.Interval : 5000)
                };
                timer.Tick += Timer_Tick_Slideshow;
                timer.Start();
                log.Debug($"Slideshow timer started with {fenceInfo.Items.Count} images");
            }

            return imageControl;
        }

        private UIElement CreateMasonryGridContent(Brush background)
        {
            lazyImages = new List<LazyImage>();
            displayedImages = new HashSet<string>();

            // Get screen width to apply 1/3 cap on maximum column width
            var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var maxAllowedColumnWidth = (int)(screenWidth / 3.0);

            // Apply user settings with 1/3 screen width cap
            int minColWidth = Math.Max(100, fenceInfo.MasonryMinColumnWidth); // Minimum 100px
            int maxColWidth = Math.Min(fenceInfo.MasonryMaxColumnWidth, maxAllowedColumnWidth);
            maxColWidth = Math.Max(minColWidth, maxColWidth); // Ensure max >= min

            // Create responsive masonry panel (auto-calculates columns based on width)
            masonryPanel = new MasonryPanel
            {
                MinColumnWidth = minColWidth,
                MaxColumnWidth = maxColWidth,
                ColumnSpacing = 8,
                Background = background,
                VerticalAlignment = VerticalAlignment.Center // Center vertically
            };

            log.Debug($"MasonryGrid using columns {minColWidth}-{maxColWidth}px (screen cap: {maxAllowedColumnWidth}px)");

            // Get available images (no duplicates)
            var availableImages = GetUniqueAvailableImages();

            if (availableImages.Count > 0)
            {
                // Limit to configured max images for memory efficiency
                int maxImages = Math.Max(1, fenceInfo.MasonryMaxImages); // Ensure at least 1
                int imagesToLoad = Math.Min(maxImages, availableImages.Count);
                log.Debug($"Loading {imagesToLoad} of {availableImages.Count} images (max: {maxImages})");

                // Shuffle for variety
                var shuffledImages = availableImages.OrderBy(x => random.Next()).Take(imagesToLoad).ToList();

                foreach (var imagePath in shuffledImages)
                {
                    if (displayedImages.Contains(imagePath))
                        continue; // Skip duplicates

                    try
                    {
                        var lazyImage = new LazyImage(imagePath);

                        // Don't set explicit Width/Height - let MasonryPanel measure based on column width
                        // The image aspect ratio will be preserved via Stretch.Uniform
                        lazyImage.Margin = new Thickness(0, 0, 0, 8);

                        lazyImages.Add(lazyImage);
                        displayedImages.Add(imagePath);
                        masonryPanel.Children.Add(lazyImage);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error creating lazy image for {imagePath}: {ex.Message}", ex);
                    }
                }
            }

            // Create a container grid that will handle both centering and scrolling
            var outerGrid = new Grid
            {
                Background = background,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Add ScrollViewer that fills the grid
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center, // Center content vertically
                Background = Brushes.Transparent
            };

            // Masonry panel with top and bottom margins for centering effect
            var contentBorder = new Border
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = masonryPanel
            };

            scrollViewer.Content = contentBorder;
            outerGrid.Children.Add(scrollViewer);

            log.Debug($"MasonryGrid created with {lazyImages.Count} unique images");
            return outerGrid;
        }

        private UIElement CreateHybridContent(Brush background)
        {
            lazyImages = new List<LazyImage>();
            displayedImages = new HashSet<string>();

            // Get screen width to apply 1/3 cap on maximum column width
            var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var maxAllowedColumnWidth = (int)(screenWidth / 3.0);

            // Apply user settings with 1/3 screen width cap
            int minColWidth = Math.Max(100, fenceInfo.MasonryMinColumnWidth); // Minimum 100px
            int maxColWidth = Math.Min(fenceInfo.MasonryMaxColumnWidth, maxAllowedColumnWidth);
            maxColWidth = Math.Max(minColWidth, maxColWidth); // Ensure max >= min

            // Create responsive masonry panel for hybrid mode
            masonryPanel = new MasonryPanel
            {
                MinColumnWidth = minColWidth,
                MaxColumnWidth = maxColWidth,
                ColumnSpacing = 8,
                Background = background,
                VerticalAlignment = VerticalAlignment.Center
            };

            log.Debug($"Hybrid using columns {minColWidth}-{maxColWidth}px (screen cap: {maxAllowedColumnWidth}px)");

            var availableImages = GetUniqueAvailableImages();

            // Determine how many images to show (limit for performance)
            int maxImages = Math.Max(1, fenceInfo.MasonryMaxImages); // Ensure at least 1
            int gridSize = Math.Min(maxImages, availableImages.Count);

            if (gridSize > 0)
            {
                // Load initial random images
                LoadRandomImagesIntoMasonry(gridSize);

                // Setup timer to rotate images periodically - shuffles NEW SET each time
                if (availableImages.Count > 0)
                {
                    timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(fenceInfo.Interval > 0 ? fenceInfo.Interval : 5000)
                    };
                    timer.Tick += Timer_Tick_Hybrid_NewSet;
                    timer.Start();
                    log.Debug($"Hybrid timer started with {availableImages.Count} total images");
                }
            }

            // Create a container grid that will handle both centering and scrolling
            var outerGrid = new Grid
            {
                Background = background,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Add ScrollViewer that fills the grid
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center, // Center content vertically
                Background = Brushes.Transparent
            };

            // Masonry panel with top and bottom margins for centering effect
            var contentBorder = new Border
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = masonryPanel
            };

            scrollViewer.Content = contentBorder;
            outerGrid.Children.Add(scrollViewer);

            log.Debug($"Hybrid loaded {lazyImages.Count} image slots");
            return outerGrid;
        }

        private void SetNextSlidePicture()
        {
            if (fenceInfo.Items?.Count > 0)
            {
                var nextIndex = fenceInfo.Items.Count > 1
                    ? random.Next(0, fenceInfo.Items.Count)
                    : 0;

                currentPicture = fenceInfo.Items.ElementAt(nextIndex);
            }
        }

        private void LoadCurrentPicture()
        {
            if (string.IsNullOrEmpty(currentPicture) || !File.Exists(currentPicture))
            {
                imageControl.Source = null;
                return;
            }

            LoadImageIntoControl(currentPicture, imageControl);
        }

        private void LoadImageIntoControl(string imagePath, Image imageControl)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                imageControl.Source = null;
                return;
            }

            try
            {
                string extension = Path.GetExtension(imagePath).ToLowerInvariant();

                // Use WpfAnimatedGif for GIF files (handles both animated and static GIFs)
                if (extension == ".gif")
                {
                    // Load GIF as BitmapImage first
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    // Set animated source - WpfAnimatedGif will automatically detect if it's animated
                    ImageBehavior.SetAnimatedSource(imageControl, bitmap);
                    ImageBehavior.SetAutoStart(imageControl, true);
                    ImageBehavior.SetRepeatBehavior(imageControl, System.Windows.Media.Animation.RepeatBehavior.Forever);

                    log.Debug($"Loaded GIF image (animated if applicable): {imagePath}");
                }
                else
                {
                    // Use standard BitmapImage loading for non-GIF images
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);

                    // Handle EXIF rotation using ExifRotationReader utility
                    bitmap.Rotation = ExifRotationReader.GetRotation(imagePath);

                    bitmap.EndInit();
                    bitmap.Freeze(); // For thread safety

                    // Preprocess image to fix pure black pixels (prevents unwanted transparency)
                    // This is obligatory to prevent pure black pixels from becoming transparent
                    BitmapSource finalBitmap = ImagePreprocessor.PreprocessImage(bitmap);

                    imageControl.Source = finalBitmap;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error loading picture {imagePath}: {ex.Message}", ex);
                imageControl.Source = null;
            }
        }

        /// <summary>
        /// Gets unique list of available image files (no duplicates)
        /// </summary>
        private List<string> GetUniqueAvailableImages()
        {
            if (fenceInfo.Items == null || fenceInfo.Items.Count == 0)
                return new List<string>();

            // Use HashSet to ensure uniqueness, then filter for existing files
            var uniqueImages = new HashSet<string>(fenceInfo.Items);
            return uniqueImages.Where(File.Exists).ToList();
        }

        /// <summary>
        /// Loads random images into the masonry panel
        /// </summary>
        private void LoadRandomImagesIntoMasonry(int count)
        {
            var availableImages = GetUniqueAvailableImages();
            if (availableImages.Count == 0)
                return;

            // Get images that aren't already displayed
            var availableForLoading = availableImages
                .Where(img => !displayedImages.Contains(img))
                .ToList();

            if (availableForLoading.Count == 0)
            {
                // All images displayed, can reuse
                availableForLoading = availableImages;
            }

            // Shuffle and select
            var selectedImages = availableForLoading
                .OrderBy(x => random.Next())
                .Take(count)
                .ToList();

            foreach (var imagePath in selectedImages)
            {
                if (displayedImages.Contains(imagePath))
                    continue; // Extra safety check

                try
                {
                    var lazyImage = new LazyImage(imagePath);

                    // Don't set explicit Width/Height - let MasonryPanel measure based on column width
                    // The image aspect ratio will be preserved via Stretch.Uniform
                    lazyImage.Margin = new Thickness(0, 0, 0, 0);

                    lazyImages.Add(lazyImage);
                    displayedImages.Add(imagePath);
                    masonryPanel.Children.Add(lazyImage);
                }
                catch (Exception ex)
                {
                    log.Error($"Error loading image {imagePath}: {ex.Message}", ex);
                }
            }
        }

        private void Timer_Tick_Slideshow(object sender, EventArgs e)
        {
            SetNextSlidePicture();
            LoadCurrentPicture();

            // Notify content changed for auto-height adjustment
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Timer_Tick_Hybrid_NewSet(object sender, EventArgs e)
        {
            // Load completely NEW set of images every interval
            if (masonryPanel == null)
                return;

            var availableImages = GetUniqueAvailableImages();
            if (availableImages.Count == 0)
                return;

            log.Debug($"Shuffling to new image set");

            // Clear current images
            masonryPanel.Children.Clear();
            lazyImages.Clear();
            displayedImages.Clear();

            // Load new random set
            int maxImages = Math.Max(1, fenceInfo.MasonryMaxImages); // Ensure at least 1
            int gridSize = Math.Min(maxImages, availableImages.Count);
            LoadRandomImagesIntoMasonry(gridSize);

            // Notify content changed for auto-height adjustment
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Timer_Tick_Hybrid(object sender, EventArgs e)
        {
            // Randomly replace one or more images in the masonry grid
            if (lazyImages == null || lazyImages.Count == 0)
                return;

            var availableImages = GetUniqueAvailableImages();
            if (availableImages.Count == 0)
                return;

            // Get images not currently displayed
            var availableForSwap = availableImages
                .Where(img => !displayedImages.Contains(img))
                .ToList();

            if (availableForSwap.Count == 0)
            {
                // All images are displayed, reset and allow reuse
                displayedImages.Clear();
                foreach (var img in lazyImages)
                {
                    displayedImages.Add(img.ImagePath);
                }
                availableForSwap = availableImages
                    .Where(img => !displayedImages.Contains(img))
                    .ToList();

                if (availableForSwap.Count == 0)
                    return; // Still nothing to swap
            }

            // Replace 1-3 random images
            int imagesToReplace = Math.Min(3, lazyImages.Count);
            var indicesToReplace = Enumerable.Range(0, lazyImages.Count)
                .OrderBy(x => random.Next())
                .Take(imagesToReplace)
                .ToList();

            foreach (var index in indicesToReplace)
            {
                if (index >= lazyImages.Count)
                    continue;

                // Select a random image from available pool
                var newImagePath = availableForSwap[random.Next(availableForSwap.Count)];

                // Remove old image from tracking
                var oldImage = lazyImages[index];
                displayedImages.Remove(oldImage.ImagePath);

                // Create new lazy image
                try
                {
                    var newLazyImage = new LazyImage(newImagePath);

                    // Don't set explicit Width/Height - let MasonryPanel measure
                    newLazyImage.Margin = new Thickness(0, 0, 0, 8);

                    // Replace in panel
                    masonryPanel.Children.RemoveAt(index);
                    masonryPanel.Children.Insert(index, newLazyImage);

                    // Update tracking
                    lazyImages[index] = newLazyImage;
                    displayedImages.Add(newImagePath);
                    availableForSwap.Remove(newImagePath); // Don't reuse in same tick
                }
                catch (Exception ex)
                {
                    log.Error($"Error swapping image: {ex.Message}", ex);
                }
            }

            // Notify content changed for auto-height adjustment
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Refresh()
        {
            switch (displayMode)
            {
                case PictureDisplayMode.Slideshow:
                    SetNextSlidePicture();
                    LoadCurrentPicture();
                    break;

                case PictureDisplayMode.MasonryGrid:
                case PictureDisplayMode.Hybrid:
                    // Refresh masonry grid
                    if (masonryPanel != null && lazyImages != null)
                    {
                        masonryPanel.Children.Clear();
                        lazyImages.Clear();
                        displayedImages.Clear();

                        var availableImages = GetUniqueAvailableImages();
                        int maxImages = Math.Max(1, fenceInfo.MasonryMaxImages); // Ensure at least 1
                        int imagesToLoad = Math.Min(maxImages, availableImages.Count);

                        LoadRandomImagesIntoMasonry(imagesToLoad);
                    }
                    break;
            }
        }

        public void Cleanup()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= Timer_Tick_Slideshow;
                timer.Tick -= Timer_Tick_Hybrid;
                timer.Tick -= Timer_Tick_Hybrid_NewSet;
                timer = null;
            }

            imageControl = null;

            // Clean up lazy images
            if (lazyImages != null)
            {
                foreach (var img in lazyImages)
                {
                    // LazyImage will dispose itself via Unloaded event
                }
                lazyImages.Clear();
                lazyImages = null;
            }

            displayedImages?.Clear();
            displayedImages = null;
            masonryPanel = null;
        }

        public bool HasContent()
        {
            // Picture fence has content if there are images to display
            return fenceInfo?.Items != null && fenceInfo.Items.Count > 0;
        }
    }
}
