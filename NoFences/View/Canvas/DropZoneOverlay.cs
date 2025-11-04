using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NoFences.View.Canvas
{
    /// <summary>
    /// Visual drop zone overlay that appears during drag & drop operations.
    /// Shows 1-3 zones depending on available actions for the dropped content.
    /// </summary>
    public class DropZoneOverlay : Grid
    {
        private readonly List<DropZone> dropZones = new List<DropZone>();
        private DropZone hoveredZone = null;

        public DropZoneOverlay()
        {
            // Transparent background - zones will have their own backgrounds
            // This prevents the Grid from intercepting mouse events
            this.Background = Brushes.Transparent;
            this.Visibility = Visibility.Collapsed;

            // Fade in/out animation
            this.Opacity = 0;

            // Enable drag & drop on the overlay itself
            this.AllowDrop = true;

            // Handle drag events at overlay level
            this.DragEnter += Overlay_DragEnter;
            this.DragOver += Overlay_DragOver;
            this.DragLeave += Overlay_DragLeave;
            this.Drop += Overlay_Drop;
        }

        private void Overlay_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            // Don't set e.Handled - let events bubble
        }

        private void Overlay_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;

                // Update hover highlighting based on mouse position
                var mousePos = e.GetPosition(this);
                UpdateHover(mousePos);
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            // Don't set e.Handled - let events bubble
        }

        private void Overlay_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            // Don't hide here - let FenceContainer handle this
            // Don't set e.Handled - let events bubble
        }

        private void Overlay_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // Store which zone was hit and raise event for FenceContainer
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var mousePos = e.GetPosition(this);
                LastDropZone = GetZoneAtPosition(mousePos);
                LastDropPaths = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];

                // Raise custom event to notify FenceContainer
                DropCompleted?.Invoke(this, new DropCompletedEventArgs
                {
                    DropZone = LastDropZone,
                    DroppedPaths = LastDropPaths
                });
            }

            e.Handled = true; // Handle here since we're raising our own event
        }

        // Event to notify FenceContainer when drop completes
        public event EventHandler<DropCompletedEventArgs> DropCompleted;

        // Properties to store drop results
        public DropZoneDefinition LastDropZone { get; private set; }
        public string[] LastDropPaths { get; private set; }

        public void ClearDropResults()
        {
            LastDropZone = null;
            LastDropPaths = null;
        }

        /// <summary>
        /// Creates and displays drop zones based on available actions
        /// </summary>
        public void ShowZones(List<DropZoneDefinition> zones)
        {
            // Clear existing zones
            this.Children.Clear();
            this.RowDefinitions.Clear();
            dropZones.Clear();
            hoveredZone = null;

            if (zones == null || zones.Count == 0)
                return;

            // Create equal-height rows for each zone
            for (int i = 0; i < zones.Count; i++)
            {
                this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            // Create visual zone for each action
            for (int i = 0; i < zones.Count; i++)
            {
                var zoneDef = zones[i];
                var zone = new DropZone(zoneDef);

                // Make zones hit-testable so they can receive mouse events
                zone.IsHitTestVisible = true;

                Grid.SetRow(zone, i);
                this.Children.Add(zone);
                dropZones.Add(zone);

                // Add separator line between zones (except last)
                if (i < zones.Count - 1)
                {
                    var separator = new Border
                    {
                        Height = 2,
                        Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(20, 0, 20, 0),
                        IsHitTestVisible = false // Separators don't need hit testing
                    };
                    Grid.SetRow(separator, i);
                    this.Children.Add(separator);
                }
            }

            // Show with fade-in animation
            this.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            this.BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Hides the overlay with fade-out animation
        /// </summary>
        public void Hide()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                this.Visibility = Visibility.Collapsed;
                this.Children.Clear();
                this.RowDefinitions.Clear();
                dropZones.Clear();
                hoveredZone = null;
            };
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Updates zone highlighting based on mouse position
        /// </summary>
        public void UpdateHover(Point mousePosition)
        {
            DropZone newHoveredZone = null;

            // Find which zone the mouse is over
            foreach (var zone in dropZones)
            {
                var bounds = new Rect(zone.TranslatePoint(new Point(0, 0), this),
                                     new Size(zone.ActualWidth, zone.ActualHeight));

                if (bounds.Contains(mousePosition))
                {
                    newHoveredZone = zone;
                    break;
                }
            }

            // Update highlighting
            if (newHoveredZone != hoveredZone)
            {
                // Unhighlight previous zone
                if (hoveredZone != null)
                {
                    hoveredZone.SetHighlighted(false);
                }

                // Highlight new zone
                if (newHoveredZone != null)
                {
                    newHoveredZone.SetHighlighted(true);
                }

                hoveredZone = newHoveredZone;
            }
        }

        /// <summary>
        /// Gets the action ID of the currently hovered zone
        /// </summary>
        public string GetHoveredAction()
        {
            return hoveredZone?.Definition.ActionId;
        }

        /// <summary>
        /// Gets the zone definition at the specified position
        /// </summary>
        public DropZoneDefinition GetZoneAtPosition(Point mousePosition)
        {
            foreach (var zone in dropZones)
            {
                var bounds = new Rect(zone.TranslatePoint(new Point(0, 0), this),
                                     new Size(zone.ActualWidth, zone.ActualHeight));

                if (bounds.Contains(mousePosition))
                {
                    return zone.Definition;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Individual drop zone visual element
    /// </summary>
    public class DropZone : Border
    {
        private readonly Border highlightBorder;
        private readonly TextBlock iconText;
        private readonly TextBlock labelText;
        private readonly TextBlock descriptionText;

        public DropZoneDefinition Definition { get; }

        public DropZone(DropZoneDefinition definition)
        {
            this.Definition = definition;

            // Zone container with semi-transparent dark background
            this.Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 30));
            this.BorderThickness = new Thickness(0);
            this.Margin = new Thickness(10);

            // Highlight border (shown on hover)
            highlightBorder = new Border
            {
                BorderBrush = new SolidColorBrush(definition.HighlightColor),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Color.FromArgb(30,
                    definition.HighlightColor.R,
                    definition.HighlightColor.G,
                    definition.HighlightColor.B)),
                Visibility = Visibility.Collapsed
            };

            // Content stack panel
            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Icon (emoji)
            iconText = new TextBlock
            {
                Text = definition.Icon,
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Label (main action text)
            labelText = new TextBlock
            {
                Text = definition.Label,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Description (what will happen)
            descriptionText = new TextBlock
            {
                Text = definition.Description,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 300
            };

            stackPanel.Children.Add(iconText);
            stackPanel.Children.Add(labelText);
            stackPanel.Children.Add(descriptionText);

            highlightBorder.Child = stackPanel;
            this.Child = highlightBorder;
        }

        /// <summary>
        /// Sets whether this zone is highlighted (hovered)
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            highlightBorder.Visibility = highlighted ? Visibility.Visible : Visibility.Collapsed;

            // Subtle scale animation on hover
            if (highlighted)
            {
                var scaleTransform = new ScaleTransform(1, 1);
                this.RenderTransform = scaleTransform;
                this.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleAnimation = new DoubleAnimation(1, 1.05, TimeSpan.FromMilliseconds(100));
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
            else
            {
                if (this.RenderTransform is ScaleTransform scale)
                {
                    var scaleAnimation = new DoubleAnimation(1.05, 1, TimeSpan.FromMilliseconds(100));
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                }
            }
        }
    }

    /// <summary>
    /// Defines a drop zone's appearance and action
    /// </summary>
    public class DropZoneDefinition
    {
        public string ActionId { get; set; }
        public string Icon { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public Color HighlightColor { get; set; }

        public DropZoneDefinition(string actionId, string icon, string label, string description, Color highlightColor)
        {
            ActionId = actionId;
            Icon = icon;
            Label = label;
            Description = description;
            HighlightColor = highlightColor;
        }
    }

    /// <summary>
    /// Event args for drop completed event
    /// </summary>
    public class DropCompletedEventArgs : EventArgs
    {
        public DropZoneDefinition DropZone { get; set; }
        public string[] DroppedPaths { get; set; }
    }
}
