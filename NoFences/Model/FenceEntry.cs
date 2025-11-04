using NoFences.Core.Model;
using NoFences.Util;
using NoFences.Win32.Desktop;
using NoFences.Win32.Window;
using NoFences.Win32.Shell;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace NoFences.Model
{
    public class FenceEntry
    {
        public string Path { get; }

        public EntryType Type { get; }

        public string IconPath { get; }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        private FenceEntry(string path, EntryType type, string iconPath = null)
        {
            Path = path;
            Type = type;
            IconPath = iconPath;
        }

        public static FenceEntry FromPath(string path, string iconPath = null)
        {
            if (File.Exists(path) || Directory.Exists(path))
                return new FenceEntry(path, EntryType.Files, iconPath);
            else return null;
        }

        public Icon ExtractIcon(ThumbnailProvider thumbnailProvider)
        {
            try
            {
                // Use separate icon path if provided (for Steam games, etc.)
                string pathForIcon = !string.IsNullOrEmpty(IconPath) ? IconPath : Path;

                // Check if it's a directory
                if (Directory.Exists(Path))
                {
                    return IconUtil.FolderLarge;
                }

                // It's a file - extract icon
                if (Type == EntryType.Files)
                {
                    // For .url shortcuts with icon path, extract from the icon path
                    if (!string.IsNullOrEmpty(IconPath) && File.Exists(IconPath))
                    {
                        try
                        {
                            return Icon.ExtractAssociatedIcon(IconPath);
                        }
                        catch
                        {
                            // If icon extraction fails, fall through to default path
                            Debug.WriteLine($"FenceEntry: Failed to extract icon from IconPath '{IconPath}', falling back to Path");
                        }
                    }

                    // Standard icon extraction
                    if (thumbnailProvider.IsSupported(Path))
                        return thumbnailProvider.GenerateThumbnail(Path);
                    else
                        return Icon.ExtractAssociatedIcon(Path);
                }
                else
                {
                    return IconUtil.FolderLarge;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FenceEntry: Error extracting icon for '{Path}': {ex.Message}");
                // Return default document icon on error
                return IconUtil.DocumentFile;
            }
        }
    }
}
