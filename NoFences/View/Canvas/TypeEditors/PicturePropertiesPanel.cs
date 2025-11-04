using NoFences.Core.Model;
using NoFences.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.TypeEditors
{
    /// <summary>
    /// Properties panel for Pictures fence type
    /// </summary>
    public class PicturePropertiesPanel : TypePropertiesPanel
    {
        private ComboBox cmbDisplayMode;
        private TextBox txtInterval;
        private ComboBox cmbTimeUnit;
        private TextBox txtMinColumnWidth;
        private TextBox txtMaxColumnWidth;
        private TextBox txtMaxImages;
        private Button btnSelectImages;
        private TextBlock txtImageCount;

        private List<string> selectedImages = new List<string>();

        public PicturePropertiesPanel()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Display Mode
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Interval
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2: Column Widths
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 3: Max Images
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 4: Image Selection
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 5: Future

            // Display Mode
            var modePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            modePanel.Children.Add(new TextBlock { Text = "Display Mode:", Margin = new Thickness(0, 0, 0, 4) });
            cmbDisplayMode = new ComboBox();
            foreach (var mode in Enum.GetValues(typeof(PictureDisplayMode)))
            {
                cmbDisplayMode.Items.Add(mode.ToString());
            }
            modePanel.Children.Add(cmbDisplayMode);
            modePanel.Children.Add(new TextBlock
            {
                Text = "• Slideshow: One image at a time\n• MasonryGrid: All images in grid\n• Hybrid: Grid with rotation",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetRow(modePanel, 0);
            grid.Children.Add(modePanel);

            // Interval
            var intervalPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            intervalPanel.Children.Add(new TextBlock { Text = "Rotation Interval:", Margin = new Thickness(0, 0, 0, 4) });

            var intervalInputPanel = new StackPanel { Orientation = Orientation.Horizontal };
            txtInterval = new TextBox { Width = 60, Text = "5" };
            cmbTimeUnit = new ComboBox { Width = 100, Margin = new Thickness(8, 0, 0, 0) };
            cmbTimeUnit.Items.Add("Seconds");
            cmbTimeUnit.Items.Add("Minutes");
            cmbTimeUnit.SelectedIndex = 0;
            intervalInputPanel.Children.Add(txtInterval);
            intervalInputPanel.Children.Add(cmbTimeUnit);
            intervalPanel.Children.Add(intervalInputPanel);

            Grid.SetRow(intervalPanel, 1);
            grid.Children.Add(intervalPanel);

            // Masonry Column Widths
            var columnWidthPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            columnWidthPanel.Children.Add(new TextBlock { Text = "Masonry Column Widths (px):", Margin = new Thickness(0, 0, 0, 4) });

            var columnWidthGrid = new Grid();
            columnWidthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            columnWidthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            columnWidthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            columnWidthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            columnWidthGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var minLabel = new TextBlock { Text = "Min:", VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(minLabel, 0);
            columnWidthGrid.Children.Add(minLabel);

            txtMinColumnWidth = new TextBox { Text = "200" };
            Grid.SetColumn(txtMinColumnWidth, 1);
            columnWidthGrid.Children.Add(txtMinColumnWidth);

            var maxLabel = new TextBlock { Text = "Max:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            Grid.SetColumn(maxLabel, 3);
            columnWidthGrid.Children.Add(maxLabel);

            txtMaxColumnWidth = new TextBox { Text = "400" };
            Grid.SetColumn(txtMaxColumnWidth, 4);
            columnWidthGrid.Children.Add(txtMaxColumnWidth);

            columnWidthPanel.Children.Add(columnWidthGrid);
            columnWidthPanel.Children.Add(new TextBlock
            {
                Text = "Controls image column sizes in MasonryGrid and Hybrid modes\n(Max capped at 1/3 screen width)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetRow(columnWidthPanel, 2);
            grid.Children.Add(columnWidthPanel);

            // Max Images
            var maxImagesPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            maxImagesPanel.Children.Add(new TextBlock { Text = "Max Images to Display:", Margin = new Thickness(0, 0, 0, 4) });

            txtMaxImages = new TextBox { Width = 80, HorizontalAlignment = HorizontalAlignment.Left, Text = "50" };
            maxImagesPanel.Children.Add(txtMaxImages);

            maxImagesPanel.Children.Add(new TextBlock
            {
                Text = "Maximum number of images to show at once in MasonryGrid and Hybrid modes\n(Higher values use more memory)",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            });
            Grid.SetRow(maxImagesPanel, 3);
            grid.Children.Add(maxImagesPanel);

            // Image selection
            var imagePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            imagePanel.Children.Add(new TextBlock { Text = "Images/Folder:", Margin = new Thickness(0, 0, 0, 4) });

            btnSelectImages = new Button { Content = "Select Images or Folder...", HorizontalAlignment = HorizontalAlignment.Left };
            btnSelectImages.Click += BtnSelectImages_Click;
            imagePanel.Children.Add(btnSelectImages);

            txtImageCount = new TextBlock { Margin = new Thickness(0, 4, 0, 0), Text = "No images selected" };
            imagePanel.Children.Add(txtImageCount);

            Grid.SetRow(imagePanel, 4);
            grid.Children.Add(imagePanel);

            Content = grid;
        }

        private void BtnSelectImages_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement multi-file selection or folder selection dialog
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select folder containing images";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Get all image files from folder
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var files = System.IO.Directory.GetFiles(dialog.SelectedPath)
                    .Where(f => imageExtensions.Contains(System.IO.Path.GetExtension(f).ToLower()))
                    .ToList();

                selectedImages = files;
                txtImageCount.Text = $"{files.Count} images selected from {dialog.SelectedPath}";
            }
        }

        public override void LoadFromFenceInfo(FenceInfo fenceInfo)
        {
            // Display mode
            if (!string.IsNullOrEmpty(fenceInfo.PictureDisplayMode) &&
                Enum.TryParse<PictureDisplayMode>(fenceInfo.PictureDisplayMode, out var mode))
            {
                cmbDisplayMode.SelectedItem = mode.ToString();
            }
            else
            {
                cmbDisplayMode.SelectedItem = PictureDisplayMode.Slideshow.ToString();
            }

            // Interval
            int intervalValue = fenceInfo.Interval;
            if (intervalValue >= 60000)
            {
                txtInterval.Text = (intervalValue / 60000).ToString();
                cmbTimeUnit.SelectedIndex = 1; // Minutes
            }
            else
            {
                txtInterval.Text = (intervalValue / 1000).ToString();
                cmbTimeUnit.SelectedIndex = 0; // Seconds
            }

            // Masonry column widths
            txtMinColumnWidth.Text = fenceInfo.MasonryMinColumnWidth.ToString();
            txtMaxColumnWidth.Text = fenceInfo.MasonryMaxColumnWidth.ToString();

            // Max images
            txtMaxImages.Text = fenceInfo.MasonryMaxImages.ToString();

            // Images
            if (fenceInfo.Items != null && fenceInfo.Items.Count > 0)
            {
                selectedImages = new List<string>(fenceInfo.Items);
                txtImageCount.Text = $"{selectedImages.Count} images";
            }
        }

        public override void SaveToFenceInfo(FenceInfo fenceInfo)
        {
            fenceInfo.PictureDisplayMode = cmbDisplayMode.SelectedItem?.ToString() ?? PictureDisplayMode.Slideshow.ToString();

            // Calculate interval in milliseconds
            if (int.TryParse(txtInterval.Text, out int interval))
            {
                int multiplier = cmbTimeUnit.SelectedIndex == 0 ? 1000 : 60000;
                fenceInfo.Interval = interval * multiplier;
            }

            // Masonry column widths with validation
            if (int.TryParse(txtMinColumnWidth.Text, out int minWidth))
            {
                fenceInfo.MasonryMinColumnWidth = Math.Max(100, minWidth); // Minimum 100px
            }

            if (int.TryParse(txtMaxColumnWidth.Text, out int maxWidth))
            {
                fenceInfo.MasonryMaxColumnWidth = Math.Max(fenceInfo.MasonryMinColumnWidth, maxWidth); // Ensure max >= min
            }

            // Max images with validation
            if (int.TryParse(txtMaxImages.Text, out int maxImages))
            {
                fenceInfo.MasonryMaxImages = Math.Max(1, Math.Min(200, maxImages)); // Range: 1-200
            }

            fenceInfo.Items = new List<string>(selectedImages);
        }
    }
}
