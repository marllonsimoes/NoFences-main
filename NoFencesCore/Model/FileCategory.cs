using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Predefined file categories for smart filtering
    /// </summary>
    public enum FileCategory
    {
        All,                // Show everything (no filtering)
        Documents,          // Text documents, PDFs, etc.
        Spreadsheets,       // Excel, CSV, etc.
        Presentations,      // PowerPoint, etc.
        Images,             // Photos, graphics
        Videos,             // Video files
        Audio,              // Music, sound files
        Archives,           // ZIP, RAR, etc.
        Executables,        // Programs, scripts
        Code,               // Source code files
        Shortcuts,          // .lnk and .url files
        Folders,            // Directories only
        Custom              // User-defined extension list
    }

    /// <summary>
    /// Maps file extensions to categories and provides category-related utilities
    /// </summary>
    public static class FileTypeMapper
    {
        // Extension to category mappings (lowercase, with dots)
        private static readonly Dictionary<string, FileCategory> ExtensionMap = new Dictionary<string, FileCategory>(StringComparer.OrdinalIgnoreCase)
        {
            // Documents
            { ".pdf", FileCategory.Documents },
            { ".doc", FileCategory.Documents },
            { ".docx", FileCategory.Documents },
            { ".odt", FileCategory.Documents },
            { ".txt", FileCategory.Documents },
            { ".rtf", FileCategory.Documents },
            { ".md", FileCategory.Documents },
            { ".markdown", FileCategory.Documents },
            { ".tex", FileCategory.Documents },
            { ".wps", FileCategory.Documents },

            // Spreadsheets
            { ".xls", FileCategory.Spreadsheets },
            { ".xlsx", FileCategory.Spreadsheets },
            { ".ods", FileCategory.Spreadsheets },
            { ".csv", FileCategory.Spreadsheets },
            { ".tsv", FileCategory.Spreadsheets },
            { ".xlsm", FileCategory.Spreadsheets },
            { ".xlsb", FileCategory.Spreadsheets },

            // Presentations
            { ".ppt", FileCategory.Presentations },
            { ".pptx", FileCategory.Presentations },
            { ".odp", FileCategory.Presentations },
            { ".pps", FileCategory.Presentations },
            { ".ppsx", FileCategory.Presentations },
            { ".key", FileCategory.Presentations },

            // Images
            { ".jpg", FileCategory.Images },
            { ".jpeg", FileCategory.Images },
            { ".png", FileCategory.Images },
            { ".gif", FileCategory.Images },
            { ".bmp", FileCategory.Images },
            { ".svg", FileCategory.Images },
            { ".webp", FileCategory.Images },
            { ".ico", FileCategory.Images },
            { ".tiff", FileCategory.Images },
            { ".tif", FileCategory.Images },
            { ".psd", FileCategory.Images },
            { ".ai", FileCategory.Images },
            { ".heic", FileCategory.Images },
            { ".raw", FileCategory.Images },

            // Videos
            { ".mp4", FileCategory.Videos },
            { ".avi", FileCategory.Videos },
            { ".mkv", FileCategory.Videos },
            { ".mov", FileCategory.Videos },
            { ".wmv", FileCategory.Videos },
            { ".webm", FileCategory.Videos },
            { ".flv", FileCategory.Videos },
            { ".m4v", FileCategory.Videos },
            { ".mpg", FileCategory.Videos },
            { ".mpeg", FileCategory.Videos },
            { ".3gp", FileCategory.Videos },
            { ".ogv", FileCategory.Videos },

            // Audio
            { ".mp3", FileCategory.Audio },
            { ".wav", FileCategory.Audio },
            { ".flac", FileCategory.Audio },
            { ".ogg", FileCategory.Audio },
            { ".m4a", FileCategory.Audio },
            { ".wma", FileCategory.Audio },
            { ".aac", FileCategory.Audio },
            { ".opus", FileCategory.Audio },
            { ".ape", FileCategory.Audio },
            { ".alac", FileCategory.Audio },
            { ".aiff", FileCategory.Audio },

            // Archives
            { ".zip", FileCategory.Archives },
            { ".rar", FileCategory.Archives },
            { ".7z", FileCategory.Archives },
            { ".tar", FileCategory.Archives },
            { ".gz", FileCategory.Archives },
            { ".bz2", FileCategory.Archives },
            { ".xz", FileCategory.Archives },
            { ".iso", FileCategory.Archives },
            { ".cab", FileCategory.Archives },
            { ".arj", FileCategory.Archives },

            // Executables
            { ".exe", FileCategory.Executables },
            { ".msi", FileCategory.Executables },
            { ".bat", FileCategory.Executables },
            { ".cmd", FileCategory.Executables },
            { ".ps1", FileCategory.Executables },
            { ".vbs", FileCategory.Executables },
            { ".com", FileCategory.Executables },
            { ".scr", FileCategory.Executables },

            // Code
            { ".cs", FileCategory.Code },
            { ".csproj", FileCategory.Code },
            { ".sln", FileCategory.Code },
            { ".js", FileCategory.Code },
            { ".ts", FileCategory.Code },
            { ".jsx", FileCategory.Code },
            { ".tsx", FileCategory.Code },
            { ".py", FileCategory.Code },
            { ".java", FileCategory.Code },
            { ".cpp", FileCategory.Code },
            { ".c", FileCategory.Code },
            { ".h", FileCategory.Code },
            { ".hpp", FileCategory.Code },
            { ".html", FileCategory.Code },
            { ".htm", FileCategory.Code },
            { ".css", FileCategory.Code },
            { ".scss", FileCategory.Code },
            { ".sass", FileCategory.Code },
            { ".less", FileCategory.Code },
            { ".xml", FileCategory.Code },
            { ".json", FileCategory.Code },
            { ".yaml", FileCategory.Code },
            { ".yml", FileCategory.Code },
            { ".sql", FileCategory.Code },
            { ".php", FileCategory.Code },
            { ".rb", FileCategory.Code },
            { ".go", FileCategory.Code },
            { ".rs", FileCategory.Code },
            { ".swift", FileCategory.Code },
            { ".kt", FileCategory.Code },
            { ".sh", FileCategory.Code },

            // Shortcuts
            { ".lnk", FileCategory.Shortcuts },
            { ".url", FileCategory.Shortcuts }
        };

        /// <summary>
        /// Gets all extensions for a given category
        /// </summary>
        public static List<string> GetExtensionsForCategory(FileCategory category)
        {
            if (category == FileCategory.All)
                return new List<string>(); // Empty list means no filtering

            return ExtensionMap
                .Where(kvp => kvp.Value == category)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Gets the category for a file based on its extension
        /// </summary>
        public static FileCategory GetCategoryForFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return FileCategory.All;

            // Check if it's a directory
            if (Directory.Exists(filePath))
                return FileCategory.Folders;

            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
                return FileCategory.All;

            return ExtensionMap.TryGetValue(extension, out var category)
                ? category
                : FileCategory.All;
        }

        /// <summary>
        /// Checks if a file matches a given category
        /// </summary>
        public static bool FileMatchesCategory(string filePath, FileCategory category)
        {
            if (category == FileCategory.All)
                return true;

            if (category == FileCategory.Folders)
                return Directory.Exists(filePath);

            var fileCategory = GetCategoryForFile(filePath);
            return fileCategory == category;
        }

        /// <summary>
        /// Gets a friendly display name for a category
        /// </summary>
        public static string GetCategoryDisplayName(FileCategory category)
        {
            switch (category)
            {
                case FileCategory.All: return "All Files";
                case FileCategory.Documents: return "Documents";
                case FileCategory.Spreadsheets: return "Spreadsheets";
                case FileCategory.Presentations: return "Presentations";
                case FileCategory.Images: return "Images";
                case FileCategory.Videos: return "Videos";
                case FileCategory.Audio: return "Audio";
                case FileCategory.Archives: return "Archives";
                case FileCategory.Executables: return "Executables";
                case FileCategory.Code: return "Code";
                case FileCategory.Shortcuts: return "Shortcuts";
                case FileCategory.Folders: return "Folders Only";
                case FileCategory.Custom: return "Custom Filter";
                default: return category.ToString();
            }
        }

        /// <summary>
        /// Gets a description of what file types are in a category
        /// </summary>
        public static string GetCategoryDescription(FileCategory category)
        {
            switch (category)
            {
                case FileCategory.All:
                    return "All files and folders";
                case FileCategory.Documents:
                    return "PDF, Word, Text, etc.";
                case FileCategory.Spreadsheets:
                    return "Excel, CSV, etc.";
                case FileCategory.Presentations:
                    return "PowerPoint, etc.";
                case FileCategory.Images:
                    return "JPG, PNG, GIF, etc.";
                case FileCategory.Videos:
                    return "MP4, AVI, MKV, etc.";
                case FileCategory.Audio:
                    return "MP3, FLAC, WAV, etc.";
                case FileCategory.Archives:
                    return "ZIP, RAR, 7Z, etc.";
                case FileCategory.Executables:
                    return "EXE, MSI, BAT, etc.";
                case FileCategory.Code:
                    return "Source code files";
                case FileCategory.Shortcuts:
                    return "LNK and URL shortcuts";
                case FileCategory.Folders:
                    return "Directories only";
                case FileCategory.Custom:
                    return "User-defined extensions";
                default:
                    return string.Empty;
            }
        }
    }
}
