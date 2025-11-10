using log4net;
using NoFences.Util;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace NoFences.View.Canvas.Controls
{
    /// <summary>
    /// An Image control that supports lazy loading, animated GIF playback, and automatic disposal
    /// to optimize memory usage with large image sets.
    /// </summary>
    public class LazyImage : Border
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LazyImage));

        private Image imageControl;
        private string imagePath;
        private bool isLoaded = false;
        private Size? cachedImageSize = null;

        // Thumbnail size for fast initial display
        private const int ThumbnailDecodeWidth = 400;

        public LazyImage(string imagePath)
        {
            this.imagePath = imagePath;

            // Create image control
            imageControl = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Child = imageControl;
            ClipToBounds = false; // Don't clip to allow full image rendering

            // Get image size without loading full image
            cachedImageSize = GetImageSize(imagePath);

            // Set placeholder background
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));

            // Load when visible
            Loaded += LazyImage_Loaded;
            Unloaded += LazyImage_Unloaded;
        }

        private void LazyImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                LoadImage();
            }
        }

        private void LazyImage_Unloaded(object sender, RoutedEventArgs e)
        {
            // Dispose image to free memory when not visible
            UnloadImage();
        }

        private void LoadImage()
        {
            if (isLoaded || string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return;

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

                    // Decode to smaller size for memory efficiency
                    bitmap.DecodePixelWidth = ThumbnailDecodeWidth;

                    // Handle EXIF rotation
                    bitmap.Rotation = GetExifRotation(imagePath);

                    bitmap.EndInit();
                    bitmap.Freeze(); // For thread safety

                    // Preprocess image to fix pure black pixels (prevents unwanted transparency)
                    // This is obligatory to prevent pure black pixels from becoming transparent
                    BitmapSource finalBitmap = ImagePreprocessor.PreprocessImage(bitmap);

                    imageControl.Source = finalBitmap;
                }

                isLoaded = true;

                // Remove placeholder background
                Background = Brushes.Transparent;
            }
            catch (Exception ex)
            {
                log.Error($"Error loading lazy image {imagePath}: {ex.Message}");
                imageControl.Source = null;
            }
        }

        private void UnloadImage()
        {
            if (imageControl.Source != null || isLoaded)
            {
                // Clear animated GIF if it was set
                ImageBehavior.SetAnimatedSource(imageControl, null);

                // Clear standard image source
                imageControl.Source = null;

                isLoaded = false;
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
        }

        /// <summary>
        /// Gets the actual size of the image for layout calculations without loading the full image
        /// </summary>
        private Size? GetImageSize(string imagePath)
        {
            try
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        return new Size(frame.PixelWidth, frame.PixelHeight);
                    }
                }
            }
            catch
            {
                // If we can't read size, use default aspect ratio
            }

            return null;
        }

        /// <summary>
        /// Calculates the display size for this image based on target width and aspect ratio
        /// </summary>
        public Size CalculateDisplaySize(double targetWidth)
        {
            if (cachedImageSize.HasValue && cachedImageSize.Value.Width > 0)
            {
                double aspectRatio = cachedImageSize.Value.Height / cachedImageSize.Value.Width;
                double height = targetWidth * aspectRatio;

                // Clamp height to reasonable range
                height = Math.Max(100, Math.Min(height, 600));

                return new Size(targetWidth, height);
            }

            // Default size if we couldn't read image dimensions
            return new Size(targetWidth, targetWidth); // Square
        }

        /// <summary>
        /// Measures the desired size based on the image's aspect ratio
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            // If we have cached image size, calculate proper aspect ratio
            if (cachedImageSize.HasValue && cachedImageSize.Value.Width > 0 && !double.IsInfinity(constraint.Width))
            {
                double aspectRatio = cachedImageSize.Value.Height / cachedImageSize.Value.Width;
                double height = constraint.Width * aspectRatio;

                // Clamp height to reasonable range
                height = Math.Max(100, Math.Min(height, 800));

                // Call base measure with calculated size
                base.MeasureOverride(new Size(constraint.Width, height));

                return new Size(constraint.Width, height);
            }

            // Default to square if no cached size
            if (!double.IsInfinity(constraint.Width))
            {
                double size = Math.Min(constraint.Width, 400);
                base.MeasureOverride(new Size(size, size));
                return new Size(size, size);
            }

            return base.MeasureOverride(constraint);
        }

        private Rotation GetExifRotation(string imagePath)
        {
            try
            {
                using (var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.None, BitmapCacheOption.None);

                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        var metadata = frame.Metadata as BitmapMetadata;

                        if (metadata != null && metadata.ContainsQuery("System.Photo.Orientation"))
                        {
                            var orientation = metadata.GetQuery("System.Photo.Orientation");
                            if (orientation != null)
                            {
                                var orientationValue = Convert.ToUInt16(orientation);

                                switch (orientationValue)
                                {
                                    case 3:
                                    case 4:
                                        return Rotation.Rotate180;
                                    case 5:
                                    case 6:
                                        return Rotation.Rotate90;
                                    case 7:
                                    case 8:
                                        return Rotation.Rotate270;
                                    default:
                                        return Rotation.Rotate0;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // If we can't read EXIF data, just use default rotation
            }

            return Rotation.Rotate0;
        }

        public string ImagePath => imagePath;
    }
}
