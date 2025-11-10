using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Represents file metadata extracted on-demand
    /// Uses Windows Shell API for document properties
    /// </summary>
    public class FileMetadata
    {
        #region Basic Properties

        /// <summary>
        /// Full path to the file
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// File name with extension
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// File extension (with dot)
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// Creation date/time
        /// </summary>
        public DateTime CreationTime { get; }

        /// <summary>
        /// Last modified date/time
        /// </summary>
        public DateTime ModifiedTime { get; }

        /// <summary>
        /// Last accessed date/time
        /// </summary>
        public DateTime AccessedTime { get; }

        /// <summary>
        /// Automatically detected file category
        /// </summary>
        public FileCategory Category { get; }

        /// <summary>
        /// Whether this is a directory
        /// </summary>
        public bool IsDirectory { get; }

        #endregion

        #region Lazy-Loaded Metadata

        private Dictionary<string, object> _metadata;
        private bool _metadataLoaded = false;
        private object _metadataLock = new object();

        /// <summary>
        /// Gets all extracted metadata as key-value pairs
        /// Loaded on first access (lazy)
        /// </summary>
        public Dictionary<string, object> Metadata
        {
            get
            {
                if (!_metadataLoaded)
                {
                    lock (_metadataLock)
                    {
                        if (!_metadataLoaded)
                        {
                            _metadata = ExtractMetadata();
                            _metadataLoaded = true;
                        }
                    }
                }
                return _metadata;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates FileMetadata from a file path
        /// Basic properties are loaded immediately, extended metadata is lazy-loaded
        /// </summary>
        public FileMetadata(string filePath)
        {
            FilePath = filePath;

            if (Directory.Exists(filePath))
            {
                IsDirectory = true;
                var dirInfo = new DirectoryInfo(filePath);
                FileName = dirInfo.Name;
                Extension = string.Empty;
                Size = 0;
                CreationTime = dirInfo.CreationTime;
                ModifiedTime = dirInfo.LastWriteTime;
                AccessedTime = dirInfo.LastAccessTime;
                Category = FileCategory.Folders;
            }
            else if (File.Exists(filePath))
            {
                IsDirectory = false;
                var fileInfo = new FileInfo(filePath);
                FileName = fileInfo.Name;
                Extension = fileInfo.Extension;
                Size = fileInfo.Length;
                CreationTime = fileInfo.CreationTime;
                ModifiedTime = fileInfo.LastWriteTime;
                AccessedTime = fileInfo.LastAccessTime;
                Category = FileTypeMapper.GetCategoryForFile(filePath);
            }
            else
            {
                // File doesn't exist - use path info only
                FileName = Path.GetFileName(filePath);
                Extension = Path.GetExtension(filePath);
                Size = 0;
                CreationTime = DateTime.MinValue;
                ModifiedTime = DateTime.MinValue;
                AccessedTime = DateTime.MinValue;
                Category = FileTypeMapper.GetCategoryForFile(filePath);
            }
        }

        #endregion

        #region Metadata Extraction

        /// <summary>
        /// Extracts metadata based on file type
        /// Called lazily when Metadata property is first accessed
        /// </summary>
        private Dictionary<string, object> ExtractMetadata()
        {
            var metadata = new Dictionary<string, object>();

            if (!File.Exists(FilePath) && !Directory.Exists(FilePath))
                return metadata;

            try
            {
                switch (Category)
                {
                    case FileCategory.Documents:
                    case FileCategory.Spreadsheets:
                    case FileCategory.Presentations:
                        ExtractDocumentMetadata(metadata);
                        break;

                    case FileCategory.Images:
                        ExtractImageMetadata(metadata);
                        break;

                    case FileCategory.Executables:
                        ExtractExecutableMetadata(metadata);
                        break;

                    case FileCategory.Videos:
                    case FileCategory.Audio:
                        ExtractMediaMetadata(metadata);
                        break;

                    case FileCategory.Shortcuts:
                        ExtractShortcutMetadata(metadata);
                        break;

                    default:
                        // For other types, extract basic Shell properties
                        ExtractBasicShellProperties(metadata);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting metadata for {FilePath}: {ex.Message}");
                metadata["Error"] = ex.Message;
            }

            return metadata;
        }

        /// <summary>
        /// Extracts document metadata using Windows Shell API
        /// Properties: Title, Author, Subject, Keywords, Comments, PageCount
        /// </summary>
        private void ExtractDocumentMetadata(Dictionary<string, object> metadata)
        {
            var shellProps = GetShellProperties(FilePath);

            // Common document properties
            if (shellProps.ContainsKey("System.Title"))
                metadata["Title"] = shellProps["System.Title"];
            if (shellProps.ContainsKey("System.Author"))
                metadata["Author"] = shellProps["System.Author"];
            if (shellProps.ContainsKey("System.Subject"))
                metadata["Subject"] = shellProps["System.Subject"];
            if (shellProps.ContainsKey("System.Keywords"))
                metadata["Keywords"] = shellProps["System.Keywords"];
            if (shellProps.ContainsKey("System.Comment"))
                metadata["Comments"] = shellProps["System.Comment"];
            if (shellProps.ContainsKey("System.Document.PageCount"))
                metadata["PageCount"] = shellProps["System.Document.PageCount"];
            if (shellProps.ContainsKey("System.Document.WordCount"))
                metadata["WordCount"] = shellProps["System.Document.WordCount"];
        }

        /// <summary>
        /// Extracts image metadata (dimensions, EXIF)
        /// Properties: Width, Height, BitDepth, DateTaken, CameraModel
        /// </summary>
        private void ExtractImageMetadata(Dictionary<string, object> metadata)
        {
            try
            {
                // Use System.Drawing for dimensions
                using (var image = Image.FromFile(FilePath))
                {
                    metadata["Width"] = image.Width;
                    metadata["Height"] = image.Height;
                    metadata["Dimensions"] = $"{image.Width}x{image.Height}";
                }
            }
            catch
            {
                // If System.Drawing fails, try Shell API
            }

            // Get additional properties from Shell
            var shellProps = GetShellProperties(FilePath);

            if (shellProps.ContainsKey("System.Image.HorizontalSize"))
                metadata["Width"] = shellProps["System.Image.HorizontalSize"];
            if (shellProps.ContainsKey("System.Image.VerticalSize"))
                metadata["Height"] = shellProps["System.Image.VerticalSize"];
            if (shellProps.ContainsKey("System.Image.BitDepth"))
                metadata["BitDepth"] = shellProps["System.Image.BitDepth"];
            if (shellProps.ContainsKey("System.Photo.DateTaken"))
                metadata["DateTaken"] = shellProps["System.Photo.DateTaken"];
            if (shellProps.ContainsKey("System.Photo.CameraModel"))
                metadata["CameraModel"] = shellProps["System.Photo.CameraModel"];
        }

        /// <summary>
        /// Extracts executable metadata using FileVersionInfo
        /// Properties: ProductName, CompanyName, FileVersion, FileDescription
        /// </summary>
        private void ExtractExecutableMetadata(Dictionary<string, object> metadata)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(FilePath);

                if (!string.IsNullOrEmpty(versionInfo.ProductName))
                    metadata["ProductName"] = versionInfo.ProductName;
                if (!string.IsNullOrEmpty(versionInfo.CompanyName))
                    metadata["CompanyName"] = versionInfo.CompanyName;
                if (!string.IsNullOrEmpty(versionInfo.FileVersion))
                    metadata["FileVersion"] = versionInfo.FileVersion;
                if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
                    metadata["ProductVersion"] = versionInfo.ProductVersion;
                if (!string.IsNullOrEmpty(versionInfo.FileDescription))
                    metadata["Description"] = versionInfo.FileDescription;
                if (!string.IsNullOrEmpty(versionInfo.LegalCopyright))
                    metadata["Copyright"] = versionInfo.LegalCopyright;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting executable metadata: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts media metadata (duration, bitrate, codec)
        /// Properties: Duration, Bitrate, FrameWidth, FrameHeight
        /// </summary>
        private void ExtractMediaMetadata(Dictionary<string, object> metadata)
        {
            var shellProps = GetShellProperties(FilePath);

            if (shellProps.ContainsKey("System.Media.Duration"))
            {
                var duration = shellProps["System.Media.Duration"];
                if (duration is long durationTicks)
                {
                    // Duration is in 100-nanosecond units
                    var timespan = TimeSpan.FromTicks(durationTicks / 10);
                    metadata["Duration"] = timespan.ToString(@"hh\:mm\:ss");
                }
            }

            if (shellProps.ContainsKey("System.Video.FrameWidth"))
                metadata["FrameWidth"] = shellProps["System.Video.FrameWidth"];
            if (shellProps.ContainsKey("System.Video.FrameHeight"))
                metadata["FrameHeight"] = shellProps["System.Video.FrameHeight"];
            if (shellProps.ContainsKey("System.Audio.EncodingBitrate"))
                metadata["Bitrate"] = shellProps["System.Audio.EncodingBitrate"];
        }

        /// <summary>
        /// Extracts shortcut metadata (target path, url)
        /// Reuses existing shortcut parsing logic
        /// </summary>
        private void ExtractShortcutMetadata(Dictionary<string, object> metadata)
        {
            try
            {
                var shortcutInfo = Util.FileUtils.GetShortcutInfo(FilePath);
                if (shortcutInfo != null)
                {
                    if (!string.IsNullOrEmpty(shortcutInfo.Name))
                        metadata["Name"] = shortcutInfo.Name;
                    if (!string.IsNullOrEmpty(shortcutInfo.Path))
                        metadata["TargetPath"] = shortcutInfo.Path;
                    if (!string.IsNullOrEmpty(shortcutInfo.Url))
                        metadata["Url"] = shortcutInfo.Url;
                    if (!string.IsNullOrEmpty(shortcutInfo.Description))
                        metadata["Description"] = shortcutInfo.Description;
                    if (!string.IsNullOrEmpty(shortcutInfo.WorkingDirectory))
                        metadata["WorkingDirectory"] = shortcutInfo.WorkingDirectory;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting shortcut metadata: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts basic Shell properties for any file type
        /// </summary>
        private void ExtractBasicShellProperties(Dictionary<string, object> metadata)
        {
            var shellProps = GetShellProperties(FilePath);

            if (shellProps.ContainsKey("System.FileDescription"))
                metadata["Description"] = shellProps["System.FileDescription"];
            if (shellProps.ContainsKey("System.ItemTypeText"))
                metadata["FileType"] = shellProps["System.ItemTypeText"];
        }

        #endregion

        #region Windows Shell API

        /// <summary>
        /// Gets file properties using Windows Shell Property System
        /// Returns dictionary of property canonical names to values
        /// </summary>
        private static Dictionary<string, object> GetShellProperties(string filePath)
        {
            var properties = new Dictionary<string, object>();

            try
            {
                // Initialize Shell COM object
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                    return properties;

                dynamic shell = Activator.CreateInstance(shellType);
                if (shell == null)
                    return properties;

                try
                {
                    string directory = Path.GetDirectoryName(filePath);
                    string fileName = Path.GetFileName(filePath);

                    var folder = shell.NameSpace(directory);
                    if (folder == null)
                        return properties;

                    var folderItem = folder.ParseName(fileName);
                    if (folderItem == null)
                        return properties;

                    // Get extended properties (0-320+ property IDs)
                    for (int i = 0; i < 320; i++)
                    {
                        try
                        {
                            string propName = folder.GetDetailsOf(null, i);
                            if (string.IsNullOrWhiteSpace(propName))
                                continue;

                            string propValue = folder.GetDetailsOf(folderItem, i);
                            if (!string.IsNullOrWhiteSpace(propValue))
                            {
                                properties[propName] = propValue;
                            }
                        }
                        catch
                        {
                            // Some property indices might fail
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(shell);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error accessing Shell properties: {ex.Message}");
            }

            return properties;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a specific metadata value by key
        /// </summary>
        public object GetMetadataValue(string key)
        {
            return Metadata.ContainsKey(key) ? Metadata[key] : null;
        }

        /// <summary>
        /// Checks if metadata contains a specific key
        /// </summary>
        public bool HasMetadata(string key)
        {
            return Metadata.ContainsKey(key);
        }

        /// <summary>
        /// Gets formatted file size string (KB, MB, GB)
        /// </summary>
        public string GetFormattedSize()
        {
            if (IsDirectory)
                return "Folder";

            if (Size < 1024)
                return $"{Size} bytes";
            else if (Size < 1024 * 1024)
                return $"{Size / 1024.0:F2} KB";
            else if (Size < 1024 * 1024 * 1024)
                return $"{Size / (1024.0 * 1024.0):F2} MB";
            else
                return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        #endregion

        public override string ToString()
        {
            return $"{FileName} ({Category}) - {GetFormattedSize()}";
        }
    }
}
