using NoFences.Core.Model;
using NoFences.Model;
using NoFences.View.Canvas.TypeEditors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using ControlzEx.Theming;

namespace NoFences.View.Canvas
{
    /// <summary>
    /// Modern WPF edit window for fence properties with system theme support.
    /// Dynamically shows/hides property panels based on fence type.
    /// Uses MahApps.Metro for automatic dark/light theme based on Windows settings.
    /// </summary>
    public partial class FenceEditWindow : MetroWindow
    {
        private FenceInfo fenceInfo;
        private FenceInfo originalFenceInfo;

        // Type-specific property panels
        private FilesPropertiesPanel filesPanel;
        private PicturePropertiesPanel picturePanel;
        private VideoPropertiesPanel videoPanel;
        private ClockPropertiesPanel clockPanel;
        private WidgetPropertiesPanel widgetPanel;

        public FenceEditWindow(FenceInfo fenceInfo)
        {
            InitializeComponent();

            // Apply MahApps.Metro theme sync with Windows dark/light mode
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            // Store reference and create a copy for editing
            this.originalFenceInfo = fenceInfo;
            this.fenceInfo = CloneFenceInfo(fenceInfo);

            InitializeControls();
            LoadFenceInfo();
        }

        private void InitializeControls()
        {
            // Populate Type combo
            foreach (var type in Enum.GetValues(typeof(EntryType)))
            {
                cmbType.Items.Add(type.ToString());
            }

            // Populate Theme combo
            foreach (var theme in Enum.GetValues(typeof(FenceTheme)))
            {
                cmbTheme.Items.Add(theme.ToString());
            }

            // Bind title height slider to textbox
            sliderTitleHeight.ValueChanged += (s, e) =>
            {
                txtTitleHeight.Text = ((int)sliderTitleHeight.Value).ToString();
            };

            txtTitleHeight.TextChanged += (s, e) =>
            {
                if (int.TryParse(txtTitleHeight.Text, out int value) && value >= 16 && value <= 60)
                {
                    sliderTitleHeight.Value = value;
                }
            };

            // Bind corner radius slider to NumericUpDown
            sliderCornerRadius.ValueChanged += (s, e) =>
            {
                if (numCornerRadius != null)
                    numCornerRadius.Value = sliderCornerRadius.Value;
            };

            numCornerRadius.ValueChanged += (s, e) =>
            {
                if (sliderCornerRadius != null && numCornerRadius.Value.HasValue)
                    sliderCornerRadius.Value = numCornerRadius.Value.Value;
            };

            // Create type-specific panels (but don't show them yet)
            filesPanel = new FilesPropertiesPanel();
            picturePanel = new PicturePropertiesPanel();
            videoPanel = new VideoPropertiesPanel();
            clockPanel = new ClockPropertiesPanel();
            widgetPanel = new WidgetPropertiesPanel();
        }

        private void LoadFenceInfo()
        {
            // Load common properties
            txtName.Text = fenceInfo.Name;
            txtTitleHeight.Text = fenceInfo.TitleHeight.ToString();
            sliderTitleHeight.Value = fenceInfo.TitleHeight;
            cmbType.SelectedItem = fenceInfo.Type;
            chkBehindIcons.IsChecked = fenceInfo.BehindDesktopIcons;
            chkAutoHeight.IsChecked = fenceInfo.AutoHeight;
            chkCanMinify.IsChecked = fenceInfo.CanMinify;
            chkEnableFade.IsChecked = fenceInfo.EnableFadeEffect;

            // Load corner radius
            numCornerRadius.Value = fenceInfo.CornerRadius;
            sliderCornerRadius.Value = fenceInfo.CornerRadius;

            // Load theme
            if (!string.IsNullOrEmpty(fenceInfo.Theme) && Enum.TryParse<FenceTheme>(fenceInfo.Theme, out var theme))
            {
                cmbTheme.SelectedItem = theme.ToString();
            }
            else
            {
                cmbTheme.SelectedItem = FenceTheme.Dark.ToString();
            }

            // Load type-specific properties
            LoadTypeSpecificPanel();
        }

        private void CmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTypeSpecificPanel();
        }

        private void LoadTypeSpecificPanel()
        {
            if (cmbType.SelectedItem == null)
                return;

            var selectedType = cmbType.SelectedItem.ToString();

            // Clear existing content
            typeSpecificContent.Content = null;
            grpTypeSpecific.Visibility = Visibility.Collapsed;

            // Show appropriate panel based on type
            if (selectedType == EntryType.Files.ToString())
            {
                filesPanel.LoadFromFenceInfo(fenceInfo);
                typeSpecificContent.Content = filesPanel;
                grpTypeSpecific.Visibility = Visibility.Visible;
            }
            else if (selectedType == EntryType.Pictures.ToString())
            {
                picturePanel.LoadFromFenceInfo(fenceInfo);
                typeSpecificContent.Content = picturePanel;
                grpTypeSpecific.Visibility = Visibility.Visible;
            }
            else if (selectedType == EntryType.Video.ToString())
            {
                videoPanel.LoadFromFenceInfo(fenceInfo);
                typeSpecificContent.Content = videoPanel;
                grpTypeSpecific.Visibility = Visibility.Visible;
            }
            else if (selectedType == EntryType.Clock.ToString())
            {
                clockPanel.LoadFromFenceInfo(fenceInfo);
                typeSpecificContent.Content = clockPanel;
                grpTypeSpecific.Visibility = Visibility.Visible;
            }
            else if (selectedType == EntryType.Widget.ToString())
            {
                widgetPanel.LoadFromFenceInfo(fenceInfo);
                typeSpecificContent.Content = widgetPanel;
                grpTypeSpecific.Visibility = Visibility.Visible;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a fence name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save common properties to original FenceInfo
            originalFenceInfo.Name = txtName.Text.Trim();
            originalFenceInfo.TitleHeight = (int)sliderTitleHeight.Value;
            originalFenceInfo.Type = cmbType.SelectedItem.ToString();
            originalFenceInfo.Theme = cmbTheme.SelectedItem.ToString();
            originalFenceInfo.BehindDesktopIcons = chkBehindIcons.IsChecked ?? false;
            originalFenceInfo.AutoHeight = chkAutoHeight.IsChecked ?? false;
            originalFenceInfo.CanMinify = chkCanMinify.IsChecked ?? false;
            originalFenceInfo.EnableFadeEffect = chkEnableFade.IsChecked ?? true;
            originalFenceInfo.CornerRadius = (int)(numCornerRadius.Value ?? 0);

            // Save type-specific properties
            var selectedType = cmbType.SelectedItem.ToString();
            if (selectedType == EntryType.Files.ToString())
            {
                filesPanel.SaveToFenceInfo(originalFenceInfo);
            }
            else if (selectedType == EntryType.Pictures.ToString())
            {
                picturePanel.SaveToFenceInfo(originalFenceInfo);
            }
            else if (selectedType == EntryType.Video.ToString())
            {
                videoPanel.SaveToFenceInfo(originalFenceInfo);
            }
            else if (selectedType == EntryType.Clock.ToString())
            {
                clockPanel.SaveToFenceInfo(originalFenceInfo);
            }
            else if (selectedType == EntryType.Widget.ToString())
            {
                widgetPanel.SaveToFenceInfo(originalFenceInfo);
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private FenceInfo CloneFenceInfo(FenceInfo original)
        {
            // Create a shallow copy for editing
            return new FenceInfo
            {
                Id = original.Id,
                Name = original.Name,
                PosX = original.PosX,
                PosY = original.PosY,
                Width = original.Width,
                Height = original.Height,
                Locked = original.Locked,
                CanMinify = original.CanMinify,
                TitleHeight = original.TitleHeight,
                Type = original.Type,
                Path = original.Path,
                Items = new List<string>(original.Items ?? new List<string>()),
                Filters = new List<string>(original.Filters ?? new List<string>()),
                Filter = original.Filter, // New smart filter
                Interval = original.Interval,
                BehindDesktopIcons = original.BehindDesktopIcons,
                Theme = original.Theme,
                CornerRadius = original.CornerRadius,
                PictureDisplayMode = original.PictureDisplayMode,
                AutoHeight = original.AutoHeight,
                MasonryMinColumnWidth = original.MasonryMinColumnWidth,
                MasonryMaxColumnWidth = original.MasonryMaxColumnWidth,
                MasonryMaxImages = original.MasonryMaxImages,
                WeatherLocation = original.WeatherLocation,
                WeatherApiKey = original.WeatherApiKey,
                EnableFadeEffect = original.EnableFadeEffect,
                VideoLoop = original.VideoLoop,
                VideoShuffle = original.VideoShuffle,
                VideoPlayOnHover = original.VideoPlayOnHover,
                VideoVolume = original.VideoVolume,
                VideoShowPlaylist = original.VideoShowPlaylist
            };
        }

        /// <summary>
        /// Gets the edited fence info. Only valid after DialogResult = true.
        /// </summary>
        public FenceInfo GetEditedFenceInfo()
        {
            return originalFenceInfo;
        }

        /// <summary>
        /// Help button click handler (title bar) - shows help window for current fence type being edited
        /// </summary>
        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpForFenceType(fenceInfo.Type);
        }

        /// <summary>
        /// Type-specific help button click handler - shows help for selected fence type in dropdown
        /// </summary>
        private void BtnTypeHelp_Click(object sender, RoutedEventArgs e)
        {
            // Get the currently selected type from the combobox
            if (cmbType.SelectedItem != null)
            {
                string selectedType = cmbType.SelectedItem.ToString();
                ShowHelpForFenceType(selectedType);
            }
        }

        /// <summary>
        /// Shows help window for a specific fence type
        /// </summary>
        private void ShowHelpForFenceType(string fenceType)
        {
            try
            {
                // Create a temporary FenceInfo with the specified type for help display
                var tempFenceInfo = new FenceInfo
                {
                    Name = fenceInfo.Name,
                    Type = fenceType
                };

                var helpWindow = new FenceHelpWindow(tempFenceInfo);
                helpWindow.Owner = this;
                helpWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open help window: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
