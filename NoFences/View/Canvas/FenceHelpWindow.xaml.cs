using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using NoFences.Core.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace NoFences.View.Canvas
{
    /// <summary>
    /// Help window showing tips, shortcuts, and instructions for fences
    /// </summary>
    public partial class FenceHelpWindow : MetroWindow
    {
        public FenceHelpWindow(FenceInfo fenceInfo)
        {
            InitializeComponent();

            // Set fence name and type
            txtFenceName.Text = fenceInfo.Name;
            txtFenceType.Text = $"{fenceInfo.Type} Fence";

            // Set icon based on fence type
            SetFenceTypeIcon(fenceInfo.Type);

            // Populate help content based on fence type
            PopulateHelpContent(fenceInfo.Type);
        }

        private void SetFenceTypeIcon(string fenceType)
        {
            PackIconMaterialKind iconKind;

            switch (fenceType)
            {
                case "Files":
                    iconKind = PackIconMaterialKind.FolderOpen;
                    break;
                case "Pictures":
                    iconKind = PackIconMaterialKind.Image;
                    break;
                case "Clock":
                    iconKind = PackIconMaterialKind.Clock;
                    break;
                case "Widget":
                    iconKind = PackIconMaterialKind.ViewDashboard;
                    break;
                default:
                    iconKind = PackIconMaterialKind.HelpCircle;
                    break;
            }

            iconFenceType.Kind = iconKind;
        }

        private void PopulateHelpContent(string fenceType)
        {
            contentPanel.Children.Clear();

            // Common tips for all fences
            AddCommonTips();

            // Type-specific tips
            switch (fenceType)
            {
                case "Files":
                    AddFilesFenceTips();
                    break;
                case "Pictures":
                    AddPicturesFenceTips();
                    break;
                case "Clock":
                    AddClockFenceTips();
                    break;
                case "Widget":
                    AddWidgetFenceTips();
                    break;
            }

            // Keyboard shortcuts
            AddKeyboardShortcuts();
        }

        private void AddCommonTips()
        {
            AddSectionHeader("📋 Common Actions");

            AddTip("• Right-click the fence to open the context menu");
            AddTip("• Click 'Edit Fence...' to customize appearance, theme, and settings");
            AddTip("• Drag the title bar to move the fence around your desktop");
            AddTip("• Hover over edges to see resize borders, then drag to resize");
            AddTip("• Lock the fence to prevent accidental movement or resizing");
            AddTip("• Enable 'Can Minify' to auto-collapse the fence when mouse leaves");
            AddTip("• Toggle 'Enable fade effect' to control transparency behavior");
        }

        private void AddFilesFenceTips()
        {
            AddSectionHeader("📁 Files Fence Tips");

            AddTip("• Drag and drop files or folders directly onto the fence to add them");
            AddTip("• Double-click files to open them with the default application");
            AddTip("• Right-click files to see Windows context menu options");
            AddTip("• Use Smart Filters to automatically categorize files:");
            AddTipIndented("- 'Installed Software' shows all installed applications");
            AddTipIndented("- 'Installed Games' shows games from Steam, Epic, etc.");
            AddTipIndented("- 'Recent Files' shows recently modified files");
            AddTip("• Configure a folder path to monitor and auto-display its contents");
            AddTip("• Icons automatically extract from executables and files");
        }

        private void AddPicturesFenceTips()
        {
            AddSectionHeader("🖼️ Pictures Fence Tips");

            AddTip("• Drag and drop images onto the fence to add them");
            AddTip("• Supported formats: JPG, PNG, GIF, BMP, ICO, WEBP, SVG");
            AddTip("• Right-click images for rotation and zoom options");
            AddTip("• Display modes:");
            AddTipIndented("- 'Grid' arranges images in uniform rows and columns");
            AddTipIndented("- 'Masonry' creates a Pinterest-style layout preserving aspect ratios");
            AddTip("• Masonry mode respects column width settings (min/max)");
            AddTip("• Set max images to limit how many are displayed");
            AddTip("• Enable 'Auto Height' to expand fence vertically to show all images");
        }

        private void AddClockFenceTips()
        {
            AddSectionHeader("🕐 Clock Fence Tips");

            AddTip("• Displays current time, date, and optional weather information");
            AddTip("• Configure weather by setting 'Weather Location' (e.g., 'London', 'Tokyo')");
            AddTip("• Get a free API key from https://openweathermap.org/api");
            AddTip("• Weather updates automatically every 15 minutes");
            AddTip("• Weather display shows:");
            AddTipIndented("- Current temperature and 'feels like' temperature");
            AddTipIndented("- Weather condition with visual icon");
            AddTipIndented("- Humidity and cloud coverage percentages");
            AddTipIndented("- Sunrise and sunset times");
            AddTipIndented("- Wind speed and direction");
            AddTip("• Adjust title height to resize the clock display area");
        }

        private void AddWidgetFenceTips()
        {
            AddSectionHeader("🔧 Widget Fence Tips");

            AddTip("• Widget fences display dynamic content and controls");
            AddTip("• Currently supports custom widget implementations");
            AddTip("• More widgets will be added in future updates");
            AddTip("• Use Edit window to configure widget-specific settings");
        }

        private void AddKeyboardShortcuts()
        {
            AddSectionHeader("⌨️ Keyboard Shortcuts");

            var shortcutsPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };

            AddShortcut(shortcutsPanel, "Right-click title", "Open context menu");
            AddShortcut(shortcutsPanel, "Drag title bar", "Move fence");
            AddShortcut(shortcutsPanel, "Hover edges", "Show resize borders");
            AddShortcut(shortcutsPanel, "Double-click items", "Open file/folder");

            contentPanel.Children.Add(shortcutsPanel);
        }

        private void AddSectionHeader(string text)
        {
            var header = new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("SectionHeader")
            };
            contentPanel.Children.Add(header);
        }

        private void AddTip(string text)
        {
            var tip = new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("TipItem")
            };
            contentPanel.Children.Add(tip);
        }

        private void AddTipIndented(string text)
        {
            var tip = new TextBlock
            {
                Text = text,
                Style = (Style)FindResource("TipItem"),
                Margin = new Thickness(20, 4, 0, 4)
            };
            contentPanel.Children.Add(tip);
        }

        private void AddShortcut(Panel parent, string action, string description)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };

            var keyBorder = new Border
            {
                Style = (Style)FindResource("ShortcutKey")
            };

            var keyText = new TextBlock
            {
                Text = action,
                Foreground = (SolidColorBrush)FindResource("SubtleHeaderBrush"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };

            keyBorder.Child = keyText;
            DockPanel.SetDock(keyBorder, Dock.Left);
            row.Children.Add(keyBorder);

            var descText = new TextBlock
            {
                Text = description,
                Foreground = (SolidColorBrush)FindResource("LabelForeground"),
                FontSize = 12,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            row.Children.Add(descText);
            parent.Children.Add(row);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
