using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NoFences.Util
{
    /// <summary>
    /// Utility class for reading EXIF rotation data from image files.
    /// Extracted from PictureFenceHandlerWpf to improve code reusability.
    ///
    /// EXIF orientation values:
    /// 1 = Normal (0°)
    /// 3 or 4 = Upside down (180°)
    /// 5 or 6 = Rotated 90° CW
    /// 7 or 8 = Rotated 270° CW (90° CCW)
    /// </summary>
    public static class ExifRotationReader
    {
        /// <summary>
        /// Reads EXIF orientation data from an image file and returns the corresponding rotation.
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>Rotation value (Rotate0, Rotate90, Rotate180, Rotate270)</returns>
        public static Rotation GetRotation(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return Rotation.Rotate0;

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
                // Common reasons: unsupported format, corrupted file, insufficient permissions
            }

            return Rotation.Rotate0;
        }
    }
}
