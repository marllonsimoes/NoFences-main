using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NoFences.Core.Model
{
    /// <summary>
    /// Types of filters that can be applied to files
    /// </summary>
    public enum FileFilterType
    {
        None,               // No filtering
        Category,           // Filter by predefined category (Documents, Images, etc.)
        Extensions,         // Filter by specific extensions (.pdf, .docx, etc.)
        Software,           // Filter installed software by category
        Pattern             // Legacy regex pattern matching (for backward compatibility)
    }

    /// <summary>
    /// Represents a filter that can be applied to fence content
    /// </summary>
    public class FileFilter
    {
        /// <summary>
        /// Type of filter to apply
        /// </summary>
        public FileFilterType FilterType { get; set; }

        /// <summary>
        /// Category to filter by (when FilterType = Category)
        /// </summary>
        public FileCategory Category { get; set; }

        /// <summary>
        /// List of extensions to filter by (when FilterType = Extensions)
        /// Should include the dot (e.g., ".pdf", ".docx")
        /// </summary>
        public List<string> Extensions { get; set; }

        /// <summary>
        /// Software category to filter by (when FilterType = Software)
        /// </summary>
        public SoftwareCategory SoftwareCategory { get; set; }

        /// <summary>
        /// Legacy pattern for regex matching (when FilterType = Pattern)
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Whether to include subfolders when scanning directories
        /// </summary>
        public bool IncludeSubfolders { get; set; }

        public FileFilter()
        {
            FilterType = FileFilterType.None;
            Category = FileCategory.All;
            Extensions = new List<string>();
            SoftwareCategory = SoftwareCategory.All;
            IncludeSubfolders = false;
        }

        /// <summary>
        /// Creates a category-based filter
        /// </summary>
        public static FileFilter FromCategory(FileCategory category)
        {
            return new FileFilter
            {
                FilterType = FileFilterType.Category,
                Category = category
            };
        }

        /// <summary>
        /// Creates an extension-based filter
        /// </summary>
        public static FileFilter FromExtensions(params string[] extensions)
        {
            return new FileFilter
            {
                FilterType = FileFilterType.Extensions,
                Extensions = extensions.Select(ext => ext.StartsWith(".") ? ext : "." + ext).ToList()
            };
        }

        /// <summary>
        /// Creates a software category filter
        /// </summary>
        public static FileFilter FromSoftwareCategory(SoftwareCategory category)
        {
            return new FileFilter
            {
                FilterType = FileFilterType.Software,
                SoftwareCategory = category
            };
        }

        /// <summary>
        /// Checks if a file passes this filter
        /// </summary>
        public bool MatchesFile(string filePath)
        {
            if (FilterType == FileFilterType.None)
                return true;

            switch (FilterType)
            {
                case FileFilterType.Category:
                    return FileTypeMapper.FileMatchesCategory(filePath, Category);

                case FileFilterType.Extensions:
                    if (Extensions == null || Extensions.Count == 0)
                        return true;
                    string ext = Path.GetExtension(filePath);
                    return Extensions.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase));

                case FileFilterType.Pattern:
                    // Legacy pattern matching - will be handled by existing MatchesPattern logic
                    return true;

                case FileFilterType.Software:
                    // Software filtering is handled separately via InstalledAppsUtil
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Gets a human-readable description of this filter
        /// </summary>
        public string GetDescription()
        {
            switch (FilterType)
            {
                case FileFilterType.None:
                    return "No filter";

                case FileFilterType.Category:
                    return $"Category: {FileTypeMapper.GetCategoryDisplayName(Category)}";

                case FileFilterType.Extensions:
                    if (Extensions == null || Extensions.Count == 0)
                        return "Custom extensions (none specified)";
                    return $"Extensions: {string.Join(", ", Extensions)}";

                case FileFilterType.Software:
                    return $"Software: {SoftwareCategorizer.GetCategoryDisplayName(SoftwareCategory)}";

                case FileFilterType.Pattern:
                    return $"Pattern: {Pattern ?? "(empty)"}";

                default:
                    return "Unknown filter";
            }
        }
    }
}
