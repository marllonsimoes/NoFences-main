using log4net;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NoFences.Util
{
    /// <summary>
    /// Handles preprocessing of images to fix compatibility issues with the canvas transparency system.
    ///
    /// Background:
    /// The DesktopCanvas uses TransparencyKey = RGB(0,0,0) to achieve transparency. This means any
    /// pure black pixels in images will become transparent. This preprocessor automatically converts
    /// pure black pixels to near-black to prevent unwanted transparency while maintaining visual appearance.
    /// </summary>
    public static class ImagePreprocessor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ImagePreprocessor));

        /// <summary>
        /// Replacement color for pure black pixels. RGB(1,1,1) is visually identical to black
        /// but won't trigger the transparency key.
        /// </summary>
        private static readonly Color NearBlackColor = Color.FromRgb(1, 1, 1);

        /// <summary>
        /// Pure black color that needs to be replaced
        /// </summary>
        private static readonly Color PureBlack = Color.FromRgb(0, 0, 0);

        /// <summary>
        /// Global setting to enable/disable image preprocessing. Default is true.
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Preprocesses a bitmap by replacing pure black pixels with near-black pixels.
        /// This prevents unwanted transparency when using RGB(0,0,0) as TransparencyKey.
        /// </summary>
        /// <param name="source">The source bitmap to preprocess</param>
        /// <returns>Preprocessed bitmap, or original if preprocessing is disabled or not needed</returns>
        public static BitmapSource PreprocessImage(BitmapSource source)
        {
            if (!IsEnabled || source == null)
                return source;

            try
            {
                // Convert to a format we can manipulate
                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = source;
                formattedBitmap.DestinationFormat = PixelFormats.Bgra32;
                formattedBitmap.EndInit();

                // Create writable bitmap for pixel manipulation
                WriteableBitmap writeable = new WriteableBitmap(formattedBitmap);

                int width = writeable.PixelWidth;
                int height = writeable.PixelHeight;
                int stride = width * 4; // 4 bytes per pixel (BGRA)
                byte[] pixels = new byte[height * stride];

                // Copy pixel data
                writeable.CopyPixels(pixels, stride, 0);

                bool hasBlackPixels = false;
                int blackPixelCount = 0;

                // Scan and replace pure black pixels
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * 4;

                        byte b = pixels[index];     // Blue
                        byte g = pixels[index + 1]; // Green
                        byte r = pixels[index + 2]; // Red
                        byte a = pixels[index + 3]; // Alpha

                        // Check if pixel is pure black (and not fully transparent)
                        if (r == 0 && g == 0 && b == 0 && a > 0)
                        {
                            hasBlackPixels = true;
                            blackPixelCount++;

                            // Replace with near-black
                            pixels[index] = NearBlackColor.B;
                            pixels[index + 1] = NearBlackColor.G;
                            pixels[index + 2] = NearBlackColor.R;
                            // Keep original alpha
                        }
                    }
                }

                // Only create new bitmap if we actually modified pixels
                if (hasBlackPixels)
                {
                    log.Debug($"Converted {blackPixelCount} pure black pixels to near-black");

                    // Write modified pixels back
                    writeable.WritePixels(
                        new System.Windows.Int32Rect(0, 0, width, height),
                        pixels,
                        stride,
                        0);

                    // Freeze for performance and thread safety
                    writeable.Freeze();
                    return writeable;
                }

                // No black pixels found, return original
                return source;
            }
            catch (Exception ex)
            {
                log.Error($"Error preprocessing image: {ex.Message}");
                // Return original on error
                return source;
            }
        }

        /// <summary>
        /// Quick check to see if an image likely contains pure black pixels.
        /// This is a fast heuristic that can be used to skip preprocessing when not needed.
        /// </summary>
        /// <param name="source">The source bitmap to check</param>
        /// <returns>True if the image might contain black pixels</returns>
        public static bool MightContainBlackPixels(BitmapSource source)
        {
            if (source == null)
                return false;

            try
            {
                // Sample just the first row to get a quick indication
                // This is a heuristic - we might miss black pixels in the rest of the image
                // but it's much faster than scanning the entire image
                int sampleWidth = Math.Min(source.PixelWidth, 100);
                int stride = sampleWidth * 4;
                byte[] samplePixels = new byte[stride];

                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = source;
                formattedBitmap.DestinationFormat = PixelFormats.Bgra32;
                formattedBitmap.EndInit();

                var writeable = new WriteableBitmap(formattedBitmap);
                writeable.CopyPixels(
                    new System.Windows.Int32Rect(0, 0, sampleWidth, 1),
                    samplePixels,
                    stride,
                    0);

                for (int x = 0; x < sampleWidth; x++)
                {
                    int index = x * 4;
                    byte b = samplePixels[index];
                    byte g = samplePixels[index + 1];
                    byte r = samplePixels[index + 2];
                    byte a = samplePixels[index + 3];

                    if (r == 0 && g == 0 && b == 0 && a > 0)
                        return true;
                }

                return false;
            }
            catch
            {
                // On error, assume it might have black pixels (safer to preprocess)
                return true;
            }
        }

        /// <summary>
        /// Gets statistics about black pixel presence in an image.
        /// Useful for debugging and logging.
        /// </summary>
        public static ImageStatistics GetImageStatistics(BitmapSource source)
        {
            var stats = new ImageStatistics();

            if (source == null)
                return stats;

            try
            {
                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = source;
                formattedBitmap.DestinationFormat = PixelFormats.Bgra32;
                formattedBitmap.EndInit();

                WriteableBitmap writeable = new WriteableBitmap(formattedBitmap);

                int width = writeable.PixelWidth;
                int height = writeable.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[height * stride];

                writeable.CopyPixels(pixels, stride, 0);

                stats.TotalPixels = width * height;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * 4;

                        byte b = pixels[index];
                        byte g = pixels[index + 1];
                        byte r = pixels[index + 2];
                        byte a = pixels[index + 3];

                        if (r == 0 && g == 0 && b == 0 && a > 0)
                        {
                            stats.BlackPixelCount++;
                        }
                    }
                }

                stats.BlackPixelPercentage = (stats.BlackPixelCount / (double)stats.TotalPixels) * 100.0;
            }
            catch (Exception ex)
            {
                stats.Error = ex.Message;
            }

            return stats;
        }
    }

    /// <summary>
    /// Statistics about black pixel presence in an image
    /// </summary>
    public class ImageStatistics
    {
        public int TotalPixels { get; set; }
        public int BlackPixelCount { get; set; }
        public double BlackPixelPercentage { get; set; }
        public string Error { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Error))
                return $"Error: {Error}";

            return $"{BlackPixelCount}/{TotalPixels} ({BlackPixelPercentage:F2}%) black pixels";
        }
    }
}
