using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NoFences.Core.Model;
using NoFences.Model;
using NoFencesDataLayer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Properties panel for Files fence type with smart filtering support
    /// </summary>
    public class FilesPropertiesPanel : TypePropertiesPanel
    {
        private TextBox txtPath;
        private Button btnBrowse;
        private ComboBox cmbFilterType;
        private StackPanel filterOptionsPanel;
        private CheckBox chkIncludeSubfolders;

        // Smart filter controls
        private ComboBox cmbFileCategory;
        private ComboBox cmbSoftwareCategory;
        private ComboBox cmbSoftwareSource;
        private TextBox txtExtension;
        private ListBox lstExtensions;
        private Button btnAddExtension;
        private Button btnRemoveExtension;

        // Legacy pattern controls
        private TextBox txtPattern;
        private ListBox lstPatterns;
        private Button btnAddPattern;
        private Button btnRemovePattern;

        private List<string> patterns = new List<string>();
        private List<string> extensions = new List<string>();

        public FilesPropertiesPanel()
        {
            var mainStack = new StackPanel();

            // Path selection
            var pathPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            pathPanel.Children.Add(new TextBlock { Text = "Source Folder:", Margin = new Thickness(0, 0, 0, 4) });

            var pathInputPanel = new DockPanel();
            btnBrowse = new Button { Content = "Browse...", Width = 80, Margin = new Thickness(8, 0, 0, 0) };
            btnBrowse.Click += BtnBrowse_Click;
            DockPanel.SetDock(btnBrowse, Dock.Right);

            txtPath = new TextBox();
            pathInputPanel.Children.Add(btnBrowse);
            pathInputPanel.Children.Add(txtPath);
            pathPanel.Children.Add(pathInputPanel);
            pathPanel.Children.Add(new TextBlock
            {
                Text = "Leave empty to manually select items",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            });

            mainStack.Children.Add(pathPanel);

            // Filter Type selection
            var filterTypePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            filterTypePanel.Children.Add(new TextBlock { Text = "Filter Type:", Margin = new Thickness(0, 0, 0, 4) });
            cmbFilterType = new ComboBox();
            foreach (var filterType in Enum.GetValues(typeof(FileFilterType)))
            {
                cmbFilterType.Items.Add(filterType.ToString());
            }
            cmbFilterType.SelectionChanged += CmbFilterType_SelectionChanged;
            filterTypePanel.Children.Add(cmbFilterType);
            filterTypePanel.Children.Add(new TextBlock
            {
                Text = "• None: Show all files\n• Category: Filter by file type (Documents, Images, etc.)\n• Extensions: Filter by specific extensions (.pdf, .docx, etc.)\n• Software: Show installed software by category\n• Pattern: Legacy regex matching",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            });

            mainStack.Children.Add(filterTypePanel);

            // Dynamic filter options panel
            filterOptionsPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            mainStack.Children.Add(filterOptionsPanel);

            // Include Subfolders checkbox
            chkIncludeSubfolders = new CheckBox
            {
                Content = "Include subfolders when scanning directory",
                Margin = new Thickness(0, 0, 0, 4)
            };
            mainStack.Children.Add(chkIncludeSubfolders);

            Content = mainStack;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = dialog.SelectedPath;
            }
        }

        private void CmbFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFilterOptionsPanel();
        }

        private void UpdateFilterOptionsPanel()
        {
            filterOptionsPanel.Children.Clear();

            if (cmbFilterType.SelectedItem == null)
                return;

            var selectedType = cmbFilterType.SelectedItem.ToString();

            if (selectedType == FileFilterType.Category.ToString())
            {
                // Show file category dropdown
                filterOptionsPanel.Children.Add(new TextBlock { Text = "File Category:", Margin = new Thickness(0, 0, 0, 4) });
                cmbFileCategory = new ComboBox();
                foreach (var category in Enum.GetValues(typeof(FileCategory)))
                {
                    var displayName = FileTypeMapper.GetCategoryDisplayName((FileCategory)category);
                    cmbFileCategory.Items.Add(displayName);
                    cmbFileCategory.Tag = category; // Store enum value
                }
                filterOptionsPanel.Children.Add(cmbFileCategory);
                filterOptionsPanel.Children.Add(new TextBlock
                {
                    Text = "Automatically filters files by predefined categories",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }
            else if (selectedType == FileFilterType.Extensions.ToString())
            {
                // Show extension list UI
                filterOptionsPanel.Children.Add(new TextBlock { Text = "File Extensions:", Margin = new Thickness(0, 0, 0, 4) });

                var addPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                btnAddExtension = new Button { Content = "Add", Width = 60, Margin = new Thickness(8, 0, 0, 0) };
                btnAddExtension.Click += BtnAddExtension_Click;
                DockPanel.SetDock(btnAddExtension, Dock.Right);

                txtExtension = new TextBox { };
                txtExtension.KeyDown += TxtExtension_KeyDown;
                addPanel.Children.Add(btnAddExtension);
                addPanel.Children.Add(txtExtension);
                filterOptionsPanel.Children.Add(addPanel);

                var listPanel = new DockPanel();
                var buttonStack = new StackPanel { Width = 80, Margin = new Thickness(8, 0, 0, 0) };
                btnRemoveExtension = new Button { Content = "Remove", Margin = new Thickness(0, 0, 0, 4) };
                btnRemoveExtension.Click += BtnRemoveExtension_Click;
                buttonStack.Children.Add(btnRemoveExtension);
                DockPanel.SetDock(buttonStack, Dock.Right);

                lstExtensions = new ListBox { MinHeight = 80 };
                listPanel.Children.Add(buttonStack);
                listPanel.Children.Add(lstExtensions);
                filterOptionsPanel.Children.Add(listPanel);

                filterOptionsPanel.Children.Add(new TextBlock
                {
                    Text = "Enter extensions with dot (e.g., .pdf, .docx, .jpg)",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }
            else if (selectedType == FileFilterType.Software.ToString())
            {
                // Show software category dropdown
                filterOptionsPanel.Children.Add(new TextBlock { Text = "Software Category:", Margin = new Thickness(0, 0, 0, 4) });
                cmbSoftwareCategory = new ComboBox();

                // Load categories dynamically from database
                var availableCategories = NoFencesDataLayer.Services.SoftwareCatalogService.GetAvailableCategoriesStatic();

                // Sort categories alphabetically by display name
                var sortedCategories = availableCategories
                    .OrderBy(cat => SoftwareCategorizer.GetCategoryDisplayName(cat))
                    .ToList();

                foreach (var category in sortedCategories)
                {
                    var displayName = SoftwareCategorizer.GetCategoryDisplayName(category);
                    cmbSoftwareCategory.Items.Add(displayName);
                }

                filterOptionsPanel.Children.Add(cmbSoftwareCategory);

                // Software Source Filter
                filterOptionsPanel.Children.Add(new TextBlock { Text = "Software Source (Optional):", Margin = new Thickness(0, 8, 0, 4) });
                cmbSoftwareSource = new ComboBox();

                // Add "All Sources" as first option (null value)
                cmbSoftwareSource.Items.Add("All Sources");

                // Get available sources from database
                var availableSources = NoFencesDataLayer.Services.InstalledSoftwareService.GetAvailableSources();
                foreach (var source in availableSources.OrderBy(s => s))
                {
                    cmbSoftwareSource.Items.Add(source);
                }

                cmbSoftwareSource.SelectedIndex = 0; // Default to "All Sources"
                filterOptionsPanel.Children.Add(cmbSoftwareSource);

                filterOptionsPanel.Children.Add(new TextBlock
                {
                    Text = "Shows installed software from the selected category.\nOptionally filter by source (Steam, GOG, Epic Games, etc.)\nOnly categories with entries in the catalog are shown.",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                // Manual metadata enrichment button
                var btnEnrichMetadata = new Button
                {
                    Content = "Enrich Metadata (Force Sync)",
                    Margin = new Thickness(0, 12, 0, 0),
                    Padding = new Thickness(12, 6, 12, 6)
                };
                btnEnrichMetadata.Click += BtnEnrichMetadata_Click;
                filterOptionsPanel.Children.Add(btnEnrichMetadata);

                filterOptionsPanel.Children.Add(new TextBlock
                {
                    Text = "Manually fetch metadata (publisher, description, genres, etc.) from online sources.\nAutomatic enrichment runs in background after database refresh.",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                // Enriched Metadata Info Panel
                var metadataInfoPanel = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 0, 120, 215)),
                    BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 120, 215)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 12, 0, 0),
                    Padding = new Thickness(12, 8, 12, 8)
                };

                var metadataInfoStack = new StackPanel();
                metadataInfoStack.Children.Add(new TextBlock
                {
                    Text = "📊 Enriched Metadata",
                    FontWeight = System.Windows.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 6)
                });

                metadataInfoStack.Children.Add(new TextBlock
                {
                    Text = "Hover over items to see enriched metadata:\n• Description  • Genres  • Rating  • Release Date\n• Developers  • Metadata Source",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.DarkGray,
                    TextWrapping = System.Windows.TextWrapping.Wrap
                });

                metadataInfoPanel.Child = metadataInfoStack;
                filterOptionsPanel.Children.Add(metadataInfoPanel);
            }
            else if (selectedType == FileFilterType.Pattern.ToString())
            {
                // Show legacy pattern list UI
                filterOptionsPanel.Children.Add(new TextBlock { Text = "Regex Patterns:", Margin = new Thickness(0, 0, 0, 4) });

                var addPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
                btnAddPattern = new Button { Content = "Add", Width = 60, Margin = new Thickness(8, 0, 0, 0) };
                btnAddPattern.Click += BtnAddPattern_Click;
                DockPanel.SetDock(btnAddPattern, Dock.Right);

                txtPattern = new TextBox();
                txtPattern.KeyDown += TxtPattern_KeyDown;
                addPanel.Children.Add(btnAddPattern);
                addPanel.Children.Add(txtPattern);
                filterOptionsPanel.Children.Add(addPanel);

                var listPanel = new DockPanel();
                var buttonStack = new StackPanel { Width = 80, Margin = new Thickness(8, 0, 0, 0) };
                btnRemovePattern = new Button { Content = "Remove", Margin = new Thickness(0, 0, 0, 4) };
                btnRemovePattern.Click += BtnRemovePattern_Click;
                buttonStack.Children.Add(btnRemovePattern);
                DockPanel.SetDock(buttonStack, Dock.Right);

                lstPatterns = new ListBox { MinHeight = 80 };
                listPanel.Children.Add(buttonStack);
                listPanel.Children.Add(lstPatterns);
                filterOptionsPanel.Children.Add(listPanel);

                filterOptionsPanel.Children.Add(new TextBlock
                {
                    Text = "Legacy pattern matching (e.g., *.txt, MyFile.*)",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }
        }

        // Extension list handlers
        private void TxtExtension_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddExtension();
                e.Handled = true;
            }
        }

        private void BtnAddExtension_Click(object sender, RoutedEventArgs e)
        {
            AddExtension();
        }

        private void AddExtension()
        {
            if (!string.IsNullOrWhiteSpace(txtExtension.Text))
            {
                var ext = txtExtension.Text.Trim();
                if (!ext.StartsWith("."))
                    ext = "." + ext;

                extensions.Add(ext);
                lstExtensions.Items.Add(ext);
                txtExtension.Clear();
            }
        }

        private void BtnRemoveExtension_Click(object sender, RoutedEventArgs e)
        {
            if (lstExtensions.SelectedItem != null)
            {
                var selected = lstExtensions.SelectedItem.ToString();
                extensions.Remove(selected);
                lstExtensions.Items.Remove(lstExtensions.SelectedItem);
            }
        }

        // Legacy pattern handlers
        private void TxtPattern_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddPattern();
                e.Handled = true;
            }
        }

        private void BtnAddPattern_Click(object sender, RoutedEventArgs e)
        {
            AddPattern();
        }

        private void AddPattern()
        {
            if (!string.IsNullOrWhiteSpace(txtPattern.Text))
            {
                patterns.Add(txtPattern.Text.Trim());
                lstPatterns.Items.Add(txtPattern.Text.Trim());
                txtPattern.Clear();
            }
        }

        private void BtnRemovePattern_Click(object sender, RoutedEventArgs e)
        {
            if (lstPatterns.SelectedItem != null)
            {
                var selected = lstPatterns.SelectedItem.ToString();
                patterns.Remove(selected);
                lstPatterns.Items.Remove(lstPatterns.SelectedItem);
            }
        }

        /// <summary>
        /// Manual metadata enrichment button handler.
        /// Enriches all un-enriched installed software with metadata from online sources.
        /// Uses IoC container to get service instance.
        /// </summary>
        private async void BtnEnrichMetadata_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "Enriching...";
                }

                // Show progress
                MessageBox.Show(
                    "Metadata enrichment started in background.\nThis may take several minutes depending on the number of entries.\nCheck the log file for progress.",
                    "Metadata Enrichment",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Get service from IoC container
                var service = Ioc.Default.GetService<InstalledSoftwareService>();
                if (service == null)
                {
                    // Fallback for cases where IoC is not initialized (shouldn't happen)
                    service = new InstalledSoftwareService();
                }

                // Loop until all entries are enriched
                int batchSize = 100;
                int totalProcessed = 0;
                int batchCount = 0;
                const int maxBatches = 50; // Safety limit to prevent infinite loop (5000 entries max)

                while (batchCount < maxBatches)
                {
                    batchCount++;
                    int entriesFound = await service.EnrichUnenrichedEntriesAsync(maxBatchSize: batchSize);

                    if (entriesFound == 0)
                    {
                        // No more unenriched entries - we're done!
                        break;
                    }

                    totalProcessed += entriesFound;

                    // Small delay between batches to avoid API rate limiting
                    if (entriesFound == batchSize)
                    {
                        // If we got a full batch, there might be more entries
                        await Task.Delay(1000);
                    }
                    else
                    {
                        // If we got less than a full batch, we're done
                        break;
                    }
                }

                MessageBox.Show(
                    $"Metadata enrichment completed!\n\nProcessed {totalProcessed} entries across {batchCount} batch(es).\n\nCheck the log file for details on enriched entries.",
                    "Enrichment Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Enrich Metadata (Force Sync)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during metadata enrichment:\n{ex.Message}",
                    "Enrichment Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Enrich Metadata (Force Sync)";
                }
            }
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            txtPath.Text = fenceInfo.Path ?? string.Empty;

            // Check if new smart filter exists
            if (fenceInfo.Filter != null)
            {
                // Load smart filter
                cmbFilterType.SelectedItem = fenceInfo.Filter.FilterType.ToString();
                chkIncludeSubfolders.IsChecked = fenceInfo.Filter.IncludeSubfolders;

                UpdateFilterOptionsPanel();

                // Load filter-specific data
                switch (fenceInfo.Filter.FilterType)
                {
                    case FileFilterType.Category:
                        if (cmbFileCategory != null)
                        {
                            var displayName = FileTypeMapper.GetCategoryDisplayName(fenceInfo.Filter.Category);
                            cmbFileCategory.SelectedItem = displayName;
                        }
                        break;

                    case FileFilterType.Extensions:
                        extensions.Clear();
                        lstExtensions.Items.Clear();
                        if (fenceInfo.Filter.Extensions != null)
                        {
                            foreach (var ext in fenceInfo.Filter.Extensions)
                            {
                                extensions.Add(ext);
                                lstExtensions.Items.Add(ext);
                            }
                        }
                        break;

                    case FileFilterType.Software:
                        if (cmbSoftwareCategory != null)
                        {
                            var displayName = SoftwareCategorizer.GetCategoryDisplayName(fenceInfo.Filter.SoftwareCategory);
                            cmbSoftwareCategory.SelectedItem = displayName;
                        }

                        // Load source filter
                        if (cmbSoftwareSource != null)
                        {
                            if (string.IsNullOrEmpty(fenceInfo.Filter.Source))
                            {
                                cmbSoftwareSource.SelectedIndex = 0; // "All Sources"
                            }
                            else
                            {
                                cmbSoftwareSource.SelectedItem = fenceInfo.Filter.Source;
                            }
                        }
                        break;

                    case FileFilterType.Pattern:
                        patterns.Clear();
                        lstPatterns.Items.Clear();
                        if (!string.IsNullOrEmpty(fenceInfo.Filter.Pattern))
                        {
                            patterns.Add(fenceInfo.Filter.Pattern);
                            lstPatterns.Items.Add(fenceInfo.Filter.Pattern);
                        }
                        break;
                }
            }
            else
            {
                // Load legacy filters
                cmbFilterType.SelectedItem = FileFilterType.None.ToString();
                chkIncludeSubfolders.IsChecked = false;

                if (fenceInfo.Filters != null && fenceInfo.Filters.Count > 0)
                {
                    // Migrate to Pattern filter type
                    cmbFilterType.SelectedItem = FileFilterType.Pattern.ToString();
                    UpdateFilterOptionsPanel();

                    patterns.Clear();
                    lstPatterns.Items.Clear();
                    foreach (var filter in fenceInfo.Filters)
                    {
                        patterns.Add(filter);
                        lstPatterns.Items.Add(filter);
                    }
                }
            }
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.Path = txtPath.Text;

            if (cmbFilterType.SelectedItem == null)
            {
                fenceInfo.Filter = null;
                return;
            }

            var selectedType = cmbFilterType.SelectedItem.ToString();
            var filterType = (FileFilterType)Enum.Parse(typeof(FileFilterType), selectedType);

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
                IncludeSubfolders = chkIncludeSubfolders.IsChecked ?? false
            };

            switch (filterType)
            {
                case FileFilterType.Category:
                    if (cmbFileCategory?.SelectedItem != null)
                    {
                        var displayName = cmbFileCategory.SelectedItem.ToString();
                        // Find matching enum value
                        foreach (FileCategory cat in Enum.GetValues(typeof(FileCategory)))
                        {
                            if (FileTypeMapper.GetCategoryDisplayName(cat) == displayName)
                            {
                                filter.Category = cat;
                                break;
                            }
                        }
                    }
                    break;

                case FileFilterType.Extensions:
                    filter.Extensions = new List<string>(extensions);
                    break;

                case FileFilterType.Software:
                    if (cmbSoftwareCategory?.SelectedItem != null)
                    {
                        var displayName = cmbSoftwareCategory.SelectedItem.ToString();
                        // Find matching enum value
                        foreach (SoftwareCategory cat in Enum.GetValues(typeof(SoftwareCategory)))
                        {
                            if (SoftwareCategorizer.GetCategoryDisplayName(cat) == displayName)
                            {
                                filter.SoftwareCategory = cat;
                                break;
                            }
                        }
                    }

                    // Save source filter
                    if (cmbSoftwareSource?.SelectedItem != null)
                    {
                        var selectedSource = cmbSoftwareSource.SelectedItem.ToString();
                        if (selectedSource == "All Sources")
                        {
                            filter.Source = null; // null = all sources
                        }
                        else
                        {
                            filter.Source = selectedSource;
                        }
                    }
                    break;

                case FileFilterType.Pattern:
                    if (patterns.Count > 0)
                    {
                        filter.Pattern = patterns.First(); // Use first pattern for now
                    }
                    break;
            }

            fenceInfo.Filter = filter;
            fenceInfo.Filters = new List<string>(); // Clear legacy filters when using new system
        }
    }
}
