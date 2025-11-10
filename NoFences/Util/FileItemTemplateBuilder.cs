using NoFences.Core.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NoFences.Util
{
    /// <summary>
    /// Utility class for creating WPF DataTemplates for file items in Files fences.
    /// Extracted from FilesFenceHandlerWpf to improve separation of concerns.
    /// </summary>
    public static class FileItemTemplateBuilder
    {
        private const int ItemWidth = 80;
        private const int ItemHeight = 100;
        private const int ItemPadding = 10;

        /// <summary>
        /// Creates a DataTemplate for displaying file items in a Files fence.
        /// </summary>
        /// <param name="theme">Theme definition for colors</param>
        /// <param name="onDoubleClick">Callback for double-click action (receives file path)</param>
        /// <returns>DataTemplate configured for file item display</returns>
        public static DataTemplate Create(FenceThemeDefinition theme, Action<string> onDoubleClick)
        {
            var template = new DataTemplate();

            // Convert theme color to WPF brush
            var textBrush = new SolidColorBrush(Color.FromArgb(
                theme.ContentTextColor.A,
                theme.ContentTextColor.R,
                theme.ContentTextColor.G,
                theme.ContentTextColor.B));

            // Root container
            var containerFactory = new FrameworkElementFactory(typeof(Border));
            containerFactory.SetValue(Border.WidthProperty, (double)ItemWidth);
            containerFactory.SetValue(Border.HeightProperty, (double)ItemHeight);
            containerFactory.SetValue(Border.MarginProperty, new Thickness(ItemPadding / 2));
            containerFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            containerFactory.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
            containerFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            containerFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            containerFactory.SetValue(Border.CursorProperty, Cursors.Hand);

            // Stack for icon + text
            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            stackFactory.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

            // Icon
            var imageFactory = new FrameworkElementFactory(typeof(Image));
            imageFactory.SetValue(Image.WidthProperty, 48.0);
            imageFactory.SetValue(Image.HeightProperty, 48.0);
            imageFactory.SetValue(Image.SourceProperty, new Binding("Icon"));
            imageFactory.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Text with theme color and ellipsis for long names
            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetValue(TextBlock.TextProperty, new Binding("Name"));
            textFactory.SetValue(TextBlock.ForegroundProperty, textBrush);  // Use theme color!
            textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            textFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis); // Add ellipsis for long text
            textFactory.SetValue(TextBlock.MaxWidthProperty, (double)(ItemWidth - 10));
            textFactory.SetValue(TextBlock.MaxHeightProperty, 30.0); // Limit to 2 lines (11pt font * 1.2 line-height * 2 lines ≈ 26.4px + margin)
            textFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 5, 0, 0));
            textFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            textFactory.SetValue(TextBlock.LineHeightProperty, 13.0); // Tighter line height to fit 2 lines in 30px

            stackFactory.AppendChild(imageFactory);
            stackFactory.AppendChild(textFactory);
            containerFactory.AppendChild(stackFactory);

            // Add event handlers
            containerFactory.AddHandler(UIElement.MouseEnterEvent, new MouseEventHandler((sender, e) =>
                OnItemMouseEnter(sender, e)));

            containerFactory.AddHandler(UIElement.MouseLeaveEvent, new MouseEventHandler((sender, e) =>
                OnItemMouseLeave(sender, e)));

            containerFactory.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler((sender, e) =>
                OnItemMouseLeftButtonDown(sender, e, onDoubleClick)));

            template.VisualTree = containerFactory;

            return template;
        }

        /// <summary>
        /// Handles mouse enter event for file items - shows hover effect
        /// </summary>
        private static void OnItemMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(80, 100, 150, 200));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 100, 150, 255));
            }
        }

        /// <summary>
        /// Handles mouse leave event for file items - removes hover effect
        /// </summary>
        private static void OnItemMouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
            }
        }

        /// <summary>
        /// Handles mouse left button down event for file items - double-click to open
        /// </summary>
        private static void OnItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e, Action<string> onDoubleClick)
        {
            if (e.ClickCount == 2 && sender is Border border)
            {
                // Extract path from the Border's DataContext
                var dataContext = border.DataContext;
                if (dataContext != null)
                {
                    // Use reflection to get Path property (FileItemViewModel)
                    var pathProperty = dataContext.GetType().GetProperty("Path");
                    if (pathProperty != null)
                    {
                        var path = pathProperty.GetValue(dataContext) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            onDoubleClick?.Invoke(path);
                        }
                    }
                }
            }
        }
    }
}
