using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NoFences.View.Canvas.Controls
{
    /// <summary>
    /// A responsive panel that arranges child elements in a masonry (Pinterest-style) layout
    /// with columns of irregular heights. Items flow into the shortest column.
    /// Automatically calculates column count based on available width and column size constraints.
    /// </summary>
    public class MasonryPanel : Panel
    {
        public static readonly DependencyProperty MinColumnWidthProperty =
            DependencyProperty.Register(
                "MinColumnWidth",
                typeof(double),
                typeof(MasonryPanel),
                new FrameworkPropertyMetadata(300.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty MaxColumnWidthProperty =
            DependencyProperty.Register(
                "MaxColumnWidth",
                typeof(double),
                typeof(MasonryPanel),
                new FrameworkPropertyMetadata(400.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ColumnSpacingProperty =
            DependencyProperty.Register(
                "ColumnSpacing",
                typeof(double),
                typeof(MasonryPanel),
                new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Minimum width for each column
        /// </summary>
        public double MinColumnWidth
        {
            get => (double)GetValue(MinColumnWidthProperty);
            set => SetValue(MinColumnWidthProperty, value);
        }

        /// <summary>
        /// Maximum width for each column
        /// </summary>
        public double MaxColumnWidth
        {
            get => (double)GetValue(MaxColumnWidthProperty);
            set => SetValue(MaxColumnWidthProperty, value);
        }

        /// <summary>
        /// Spacing between columns and items
        /// </summary>
        public double ColumnSpacing
        {
            get => (double)GetValue(ColumnSpacingProperty);
            set => SetValue(ColumnSpacingProperty, value);
        }

        private int calculatedColumnCount = 3; // Cached for performance

        /// <summary>
        /// Calculates optimal column count based on available width and column constraints
        /// </summary>
        private int CalculateColumnCount(double availableWidth)
        {
            if (double.IsInfinity(availableWidth) || availableWidth <= 0)
                return 1;

            // Start with max column width to get minimum columns
            int columnCount = 1;

            while (true)
            {
                double totalSpacing = ColumnSpacing * (columnCount - 1);
                double columnWidth = (availableWidth - totalSpacing) / columnCount;

                // If we're at or above MinColumnWidth, we can fit another column
                if (columnWidth >= MinColumnWidth)
                {
                    // Check if adding another column would still meet MinColumnWidth
                    double nextSpacing = ColumnSpacing * columnCount;
                    double nextColumnWidth = (availableWidth - nextSpacing) / (columnCount + 1);

                    if (nextColumnWidth >= MinColumnWidth)
                    {
                        columnCount++;
                    }
                    else
                    {
                        // Can't fit another column at MinColumnWidth, stop here
                        break;
                    }
                }
                else
                {
                    // Current column width is below minimum, reduce column count
                    columnCount = Math.Max(1, columnCount - 1);
                    break;
                }
            }

            return columnCount;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count == 0)
                return new Size(0, 0);

            // Calculate optimal column count based on available width
            calculatedColumnCount = CalculateColumnCount(availableSize.Width);

            // Calculate column width (will be between Min and Max)
            double totalSpacing = ColumnSpacing * (calculatedColumnCount - 1);
            double columnWidth = (availableSize.Width - totalSpacing) / calculatedColumnCount;

            // Clamp to max column width
            columnWidth = Math.Min(columnWidth, MaxColumnWidth);

            // Track column heights
            double[] columnHeights = new double[calculatedColumnCount];

            // Measure each child and assign to shortest column
            foreach (UIElement child in Children)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                    continue;

                // Measure with column width constraint
                child.Measure(new Size(columnWidth, double.PositiveInfinity));

                // Find shortest column
                int shortestColumn = 0;
                double shortestHeight = columnHeights[0];
                for (int i = 1; i < calculatedColumnCount; i++)
                {
                    if (columnHeights[i] < shortestHeight)
                    {
                        shortestHeight = columnHeights[i];
                        shortestColumn = i;
                    }
                }

                // Add item height to that column (plus spacing if not first item)
                if (columnHeights[shortestColumn] > 0)
                    columnHeights[shortestColumn] += ColumnSpacing;
                columnHeights[shortestColumn] += child.DesiredSize.Height;
            }

            // Total height is the tallest column
            double totalHeight = columnHeights.Max();
            return new Size(availableSize.Width, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0 || calculatedColumnCount <= 0)
                return finalSize;

            // Calculate column width (same as in Measure)
            double totalSpacing = ColumnSpacing * (calculatedColumnCount - 1);
            double columnWidth = (finalSize.Width - totalSpacing) / calculatedColumnCount;
            columnWidth = Math.Min(columnWidth, MaxColumnWidth);

            // Track column heights and positions
            double[] columnHeights = new double[calculatedColumnCount];
            double[] columnX = new double[calculatedColumnCount];

            // Track items per column for vertical centering
            var columnItems = new List<List<UIElement>>();
            for (int i = 0; i < calculatedColumnCount; i++)
            {
                columnItems.Add(new List<UIElement>());
            }

            // Calculate X positions for each column
            for (int i = 0; i < calculatedColumnCount; i++)
            {
                columnX[i] = i * (columnWidth + ColumnSpacing);
            }

            // First pass: Assign items to columns and calculate column heights
            foreach (UIElement child in Children)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                    continue;

                // Find shortest column
                int shortestColumn = 0;
                double shortestHeight = columnHeights[0];
                for (int i = 1; i < calculatedColumnCount; i++)
                {
                    if (columnHeights[i] < shortestHeight)
                    {
                        shortestHeight = columnHeights[i];
                        shortestColumn = i;
                    }
                }

                // Add item to column
                columnItems[shortestColumn].Add(child);

                // Update column height
                double itemHeight = child.DesiredSize.Height;
                if (columnHeights[shortestColumn] > 0)
                    columnHeights[shortestColumn] += ColumnSpacing;
                columnHeights[shortestColumn] += itemHeight;
            }

            // Find the tallest column height
            double maxColumnHeight = columnHeights.Max();

            // Second pass: Arrange items with vertical centering per column
            for (int col = 0; col < calculatedColumnCount; col++)
            {
                var items = columnItems[col];
                if (items.Count == 0)
                    continue;

                // Calculate vertical offset to center this column
                double columnContentHeight = columnHeights[col];
                double verticalOffset = (maxColumnHeight - columnContentHeight) / 2.0;

                // Arrange items in this column
                double currentY = verticalOffset;
                foreach (var child in items)
                {
                    double x = columnX[col];
                    child.Arrange(new Rect(x, currentY, columnWidth, child.DesiredSize.Height));
                    currentY += child.DesiredSize.Height + ColumnSpacing;
                }
            }

            return finalSize;
        }

        /// <summary>
        /// Gets the current calculated column count
        /// </summary>
        public int GetCalculatedColumnCount() => calculatedColumnCount;
    }
}
