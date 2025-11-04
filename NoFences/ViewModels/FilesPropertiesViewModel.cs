using NoFences.Core.Model;
using NoFences.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace NoFences.ViewModels
{
    /// <summary>
    /// ViewModel for Files fence type properties with smart filtering support.
    /// </summary>
    public class FilesPropertiesViewModel : ViewModelBase
    {
        private readonly FenceInfo fenceInfo;

        private string _sourcePath;
        public string SourcePath
        {
            get => _sourcePath;
            set => SetProperty(ref _sourcePath, value);
        }

        private string _selectedFilterType;
        public string SelectedFilterType
        {
            get => _selectedFilterType;
            set => SetProperty(ref _selectedFilterType, value);
        }

        private bool _includeSubfolders;
        public bool IncludeSubfolders
        {
            get => _includeSubfolders;
            set => SetProperty(ref _includeSubfolders, value);
        }

        private string _selectedFileCategory;
        public string SelectedFileCategory
        {
            get => _selectedFileCategory;
            set => SetProperty(ref _selectedFileCategory, value);
        }

        private string _selectedSoftwareCategory;
        public string SelectedSoftwareCategory
        {
            get => _selectedSoftwareCategory;
            set => SetProperty(ref _selectedSoftwareCategory, value);
        }

        private string _extensionInput;
        public string ExtensionInput
        {
            get => _extensionInput;
            set => SetProperty(ref _extensionInput, value);
        }

        private string _patternInput;
        public string PatternInput
        {
            get => _patternInput;
            set => SetProperty(ref _patternInput, value);
        }

        private object _selectedExtension;
        public object SelectedExtension
        {
            get => _selectedExtension;
            set => SetProperty(ref _selectedExtension, value);
        }

        private object _selectedPattern;
        public object SelectedPattern
        {
            get => _selectedPattern;
            set => SetProperty(ref _selectedPattern, value);
        }

        public ObservableCollection<string> FilterTypes { get; }
        public ObservableCollection<string> FileCategories { get; }
        public ObservableCollection<string> SoftwareCategories { get; }
        public ObservableCollection<string> Extensions { get; }
        public ObservableCollection<string> Patterns { get; }

        // Commands
        public ICommand BrowseFolderCommand { get; }
        public ICommand AddExtensionCommand { get; }
        public ICommand RemoveExtensionCommand { get; }
        public ICommand AddPatternCommand { get; }
        public ICommand RemovePatternCommand { get; }

        public FilesPropertiesViewModel(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;

            // Initialize commands
            BrowseFolderCommand = new RelayCommand(ExecuteBrowseFolder);
            AddExtensionCommand = new RelayCommand(ExecuteAddExtension);
            RemoveExtensionCommand = new RelayCommand(ExecuteRemoveExtension);
            AddPatternCommand = new RelayCommand(ExecuteAddPattern);
            RemovePatternCommand = new RelayCommand(ExecuteRemovePattern);

            // Initialize collections
            FilterTypes = new ObservableCollection<string>(
                Enum.GetValues(typeof(FileFilterType)).Cast<FileFilterType>().Select(t => t.ToString())
            );

            FileCategories = new ObservableCollection<string>(
                Enum.GetValues(typeof(FileCategory)).Cast<FileCategory>()
                    .Select(cat => FileTypeMapper.GetCategoryDisplayName(cat))
            );

            // Load software categories dynamically from database
            var availableCategories = NoFencesDataLayer.Services.SoftwareCatalogService.GetAvailableCategoriesStatic();
            SoftwareCategories = new ObservableCollection<string>(
                availableCategories
                    .OrderBy(cat => SoftwareCategorizer.GetCategoryDisplayName(cat))
                    .Select(cat => SoftwareCategorizer.GetCategoryDisplayName(cat))
            );

            Extensions = new ObservableCollection<string>();
            Patterns = new ObservableCollection<string>();

            // Load from fence info
            LoadFromFenceInfo();
        }

        private void LoadFromFenceInfo()
        {
            SourcePath = fenceInfo.Path ?? string.Empty;

            // Check if new smart filter exists
            if (fenceInfo.Filter != null)
            {
                SelectedFilterType = fenceInfo.Filter.FilterType.ToString();
                IncludeSubfolders = fenceInfo.Filter.IncludeSubfolders;

                // Load filter-specific data
                switch (fenceInfo.Filter.FilterType)
                {
                    case FileFilterType.Category:
                        SelectedFileCategory = FileTypeMapper.GetCategoryDisplayName(fenceInfo.Filter.Category);
                        break;

                    case FileFilterType.Extensions:
                        if (fenceInfo.Filter.Extensions != null)
                        {
                            foreach (var ext in fenceInfo.Filter.Extensions)
                            {
                                Extensions.Add(ext);
                            }
                        }
                        break;

                    case FileFilterType.Software:
                        SelectedSoftwareCategory = SoftwareCategorizer.GetCategoryDisplayName(fenceInfo.Filter.SoftwareCategory);
                        break;

                    case FileFilterType.Pattern:
                        if (!string.IsNullOrEmpty(fenceInfo.Filter.Pattern))
                        {
                            Patterns.Add(fenceInfo.Filter.Pattern);
                        }
                        break;
                }
            }
            else
            {
                // Load legacy filters or default to None
                SelectedFilterType = FileFilterType.None.ToString();

                if (fenceInfo.Filters != null && fenceInfo.Filters.Count > 0)
                {
                    // Migrate to Pattern filter type
                    SelectedFilterType = FileFilterType.Pattern.ToString();
                    foreach (var filter in fenceInfo.Filters)
                    {
                        Patterns.Add(filter);
                    }
                }
            }
        }

        private void ExecuteBrowseFolder()
        {
            var dialog = new FolderBrowserDialog();
            if (!string.IsNullOrEmpty(SourcePath))
            {
                dialog.SelectedPath = SourcePath;
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SourcePath = dialog.SelectedPath;
            }
        }

        private void ExecuteAddExtension()
        {
            if (!string.IsNullOrWhiteSpace(ExtensionInput))
            {
                var ext = ExtensionInput.Trim();
                if (!ext.StartsWith("."))
                    ext = "." + ext;

                if (!Extensions.Contains(ext))
                {
                    Extensions.Add(ext);
                }

                ExtensionInput = string.Empty;
            }
        }

        private void ExecuteRemoveExtension()
        {
            if (SelectedExtension != null && SelectedExtension is string ext)
            {
                Extensions.Remove(ext);
            }
        }

        private void ExecuteAddPattern()
        {
            if (!string.IsNullOrWhiteSpace(PatternInput))
            {
                var pattern = PatternInput.Trim();
                if (!Patterns.Contains(pattern))
                {
                    Patterns.Add(pattern);
                }

                PatternInput = string.Empty;
            }
        }

        private void ExecuteRemovePattern()
        {
            if (SelectedPattern != null && SelectedPattern is string pattern)
            {
                Patterns.Remove(pattern);
            }
        }

        public void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.Path = SourcePath;

            if (string.IsNullOrEmpty(SelectedFilterType))
            {
                fenceInfo.Filter = null;
                return;
            }

            var filterType = (FileFilterType)Enum.Parse(typeof(FileFilterType), SelectedFilterType);

            if (filterType == FileFilterType.None)
            {
                fenceInfo.Filter = null;
                fenceInfo.Filters = new List<string>(); // Clear legacy filters
                return;
            }

            // Create new filter
            var filter = new FileFilter
            {
                FilterType = filterType,
                IncludeSubfolders = IncludeSubfolders
            };

            switch (filterType)
            {
                case FileFilterType.Category:
                    if (!string.IsNullOrEmpty(SelectedFileCategory))
                    {
                        // Find matching enum value
                        foreach (FileCategory cat in Enum.GetValues(typeof(FileCategory)))
                        {
                            if (FileTypeMapper.GetCategoryDisplayName(cat) == SelectedFileCategory)
                            {
                                filter.Category = cat;
                                break;
                            }
                        }
                    }
                    break;

                case FileFilterType.Extensions:
                    filter.Extensions = new List<string>(Extensions);
                    break;

                case FileFilterType.Software:
                    if (!string.IsNullOrEmpty(SelectedSoftwareCategory))
                    {
                        // Find matching enum value
                        foreach (SoftwareCategory cat in Enum.GetValues(typeof(SoftwareCategory)))
                        {
                            if (SoftwareCategorizer.GetCategoryDisplayName(cat) == SelectedSoftwareCategory)
                            {
                                filter.SoftwareCategory = cat;
                                break;
                            }
                        }
                    }
                    break;

                case FileFilterType.Pattern:
                    if (Patterns.Count > 0)
                    {
                        filter.Pattern = Patterns.First(); // Use first pattern
                    }
                    break;
            }

            fenceInfo.Filter = filter;
            fenceInfo.Filters = new List<string>(); // Clear legacy filters when using new system
        }
    }
}
