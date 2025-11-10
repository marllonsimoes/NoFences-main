using NoFences.Core.Model;
using NoFences.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NoFences.ViewModels
{
    /// <summary>
    /// ViewModel for editing fence properties with MVVM pattern.
    /// Manual INotifyPropertyChanged implementation for .NET Framework 4.8.1 compatibility.
    /// </summary>
    public class FenceEditViewModel : ViewModelBase
    {
        private readonly FenceInfo originalFenceInfo;

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _titleHeight;
        public int TitleHeight
        {
            get => _titleHeight;
            set => SetProperty(ref _titleHeight, value);
        }

        private string _selectedType;
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    UpdateTypeSpecificViewModel();
                }
            }
        }

        private string _selectedTheme;
        public string SelectedTheme
        {
            get => _selectedTheme;
            set => SetProperty(ref _selectedTheme, value);
        }

        private bool _behindDesktopIcons;
        public bool BehindDesktopIcons
        {
            get => _behindDesktopIcons;
            set => SetProperty(ref _behindDesktopIcons, value);
        }

        private bool _autoHeight;
        public bool AutoHeight
        {
            get => _autoHeight;
            set => SetProperty(ref _autoHeight, value);
        }

        private bool _canMinify;
        public bool CanMinify
        {
            get => _canMinify;
            set => SetProperty(ref _canMinify, value);
        }

        private int _cornerRadius;
        public int CornerRadius
        {
            get => _cornerRadius;
            set => SetProperty(ref _cornerRadius, value);
        }

        private object _typeSpecificViewModel;
        public object TypeSpecificViewModel
        {
            get => _typeSpecificViewModel;
            set => SetProperty(ref _typeSpecificViewModel, value);
        }

        public ObservableCollection<string> AvailableTypes { get; }
        public ObservableCollection<string> AvailableThemes { get; }

        public bool DialogResult { get; private set; }

        // Commands
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public FenceEditViewModel(FenceInfo fenceInfo)
        {
            this.originalFenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));

            // Initialize commands
            OkCommand = new RelayCommand(ExecuteOk);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // Populate collections
            AvailableTypes = new ObservableCollection<string>(
                Enum.GetValues(typeof(EntryType)).Cast<EntryType>().Select(t => t.ToString())
            );

            AvailableThemes = new ObservableCollection<string>(
                Enum.GetValues(typeof(FenceTheme)).Cast<FenceTheme>().Select(t => t.ToString())
            );

            // Load fence info
            LoadFromFenceInfo(fenceInfo);
        }

        private void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            Name = fenceInfo.Name;
            TitleHeight = fenceInfo.TitleHeight;
            SelectedType = fenceInfo.Type;
            SelectedTheme = !string.IsNullOrEmpty(fenceInfo.Theme) ? fenceInfo.Theme : FenceTheme.Dark.ToString();
            BehindDesktopIcons = fenceInfo.BehindDesktopIcons;
            AutoHeight = fenceInfo.AutoHeight;
            CanMinify = fenceInfo.CanMinify;
            CornerRadius = fenceInfo.CornerRadius;

            // Load type-specific ViewModel
            UpdateTypeSpecificViewModel();
        }

        private void UpdateTypeSpecificViewModel()
        {
            if (string.IsNullOrEmpty(SelectedType))
                return;

            // Create appropriate ViewModel based on type
            if (SelectedType == EntryType.Files.ToString())
            {
                TypeSpecificViewModel = new FilesPropertiesViewModel(originalFenceInfo);
            }
            else if (SelectedType == EntryType.Pictures.ToString())
            {
                TypeSpecificViewModel = new PicturePropertiesViewModel(originalFenceInfo);
            }
            else if (SelectedType == EntryType.Clock.ToString())
            {
                TypeSpecificViewModel = new ClockPropertiesViewModel(originalFenceInfo);
            }
            else if (SelectedType == EntryType.Widget.ToString())
            {
                TypeSpecificViewModel = new WidgetPropertiesViewModel(originalFenceInfo);
            }
            else
            {
                TypeSpecificViewModel = null;
            }
        }

        private void ExecuteOk()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Please enter a fence name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save to original FenceInfo
            SaveToFenceInfo();

            DialogResult = true;
        }

        private void ExecuteCancel()
        {
            DialogResult = false;
        }

        private void SaveToFenceInfo()
        {
            originalFenceInfo.Name = Name.Trim();
            originalFenceInfo.TitleHeight = TitleHeight;
            originalFenceInfo.Type = SelectedType;
            originalFenceInfo.Theme = SelectedTheme;
            originalFenceInfo.BehindDesktopIcons = BehindDesktopIcons;
            originalFenceInfo.AutoHeight = AutoHeight;
            originalFenceInfo.CanMinify = CanMinify;
            originalFenceInfo.CornerRadius = CornerRadius;

            // Save type-specific properties
            if (TypeSpecificViewModel is FilesPropertiesViewModel filesVM)
            {
                filesVM.SaveToFenceInfo(originalFenceInfo);
            }
            else if (TypeSpecificViewModel is PicturePropertiesViewModel pictureVM)
            {
                pictureVM.SaveToFenceInfo(originalFenceInfo);
            }
            else if (TypeSpecificViewModel is ClockPropertiesViewModel clockVM)
            {
                clockVM.SaveToFenceInfo(originalFenceInfo);
            }
            else if (TypeSpecificViewModel is WidgetPropertiesViewModel widgetVM)
            {
                widgetVM.SaveToFenceInfo(originalFenceInfo);
            }
        }

        public FenceInfo GetEditedFenceInfo()
        {
            return originalFenceInfo;
        }
    }
}
