using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NoFences.Behaviors
{
    /// <summary>
    /// Manages rounded corner application for fence containers.
    /// Applies Region clipping to container and child controls to create rounded visual effect.
    /// </summary>
    public class FenceRoundedCornersBehavior : IDisposable
    {
        #region Private Fields

        private readonly Control containerControl;
        private readonly Func<int> getCornerRadius;
        private readonly int borderSize;

        private Control titlePanel;
        private Control contentControl;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new rounded corners behavior
        /// </summary>
        /// <param name="containerControl">The main container control</param>
        /// <param name="getCornerRadius">Function to get the current corner radius</param>
        /// <param name="borderSize">The border thickness (affects inner radius calculation)</param>
        public FenceRoundedCornersBehavior(
            Control containerControl,
            Func<int> getCornerRadius,
            int borderSize)
        {
            this.containerControl = containerControl ?? throw new ArgumentNullException(nameof(containerControl));
            this.getCornerRadius = getCornerRadius ?? throw new ArgumentNullException(nameof(getCornerRadius));
            this.borderSize = borderSize;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers the title panel for rounded corner application
        /// </summary>
        public void RegisterTitlePanel(Control panel)
        {
            titlePanel = panel;
        }

        /// <summary>
        /// Registers the content control for rounded corner application
        /// </summary>
        public void RegisterContentControl(Control control)
        {
            contentControl = control;
        }

        /// <summary>
        /// Applies rounded corners to the container and registered child controls
        /// </summary>
        public void Apply()
        {
            try
            {
                int radius = getCornerRadius();

                if (radius <= 0 || containerControl.Width <= 0 || containerControl.Height <= 0)
                {
                    // No rounding - clear regions
                    ClearRegions();
                    return;
                }

                int innerRadius = Math.Max(1, radius - borderSize);

                // 1. Apply rounded region to main container (clips everything)
                ApplyContainerRounding(radius);

                // 2. Apply rounded corners to title panel (top corners only)
                ApplyTitlePanelRounding(innerRadius);

                // 3. Apply rounded corners to content control (bottom corners only)
                ApplyContentRounding(innerRadius);
            }
            catch (Exception ex)
            {
                // Log but don't crash - rounded corners are cosmetic
                System.Diagnostics.Debug.WriteLine($"FenceRoundedCornersBehavior: Error applying corners - {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all region clipping, restoring sharp corners
        /// </summary>
        public void ClearRegions()
        {
            containerControl.Region?.Dispose();
            containerControl.Region = null;

            if (titlePanel != null)
            {
                titlePanel.Region?.Dispose();
                titlePanel.Region = null;
            }

            if (contentControl != null)
            {
                contentControl.Region?.Dispose();
                contentControl.Region = null;
            }
        }

        #endregion

        #region Private Methods

        private void ApplyContainerRounding(int radius)
        {
            using (var path = CreateRoundedRectanglePath(
                new Rectangle(0, 0, containerControl.Width, containerControl.Height),
                radius))
            {
                containerControl.Region?.Dispose();
                containerControl.Region = new Region(path);
            }
        }

        private void ApplyTitlePanelRounding(int innerRadius)
        {
            if (titlePanel == null || titlePanel.Width <= 0 || titlePanel.Height <= 0)
                return;

            // Title panel gets top corners rounded, bottom straight
            // We extend the height by innerRadius so the bottom isn't cut off
            using (var titlePath = CreateRoundedRectanglePath(
                new Rectangle(0, 0, titlePanel.Width, titlePanel.Height + innerRadius),
                innerRadius))
            {
                titlePanel.Region?.Dispose();
                titlePanel.Region = new Region(titlePath);
            }
        }

        private void ApplyContentRounding(int innerRadius)
        {
            if (contentControl == null || contentControl.Width <= 0 || contentControl.Height <= 0)
                return;

            // Content gets bottom corners rounded, top straight
            var elementPath = new GraphicsPath();
            var rect = new Rectangle(0, 0, contentControl.Width, contentControl.Height);
            int diameter = innerRadius * 2;

            elementPath.StartFigure();

            // Top edge (straight)
            elementPath.AddLine(rect.Left, rect.Top, rect.Right, rect.Top);

            // Right edge down to corner
            elementPath.AddLine(rect.Right, rect.Top, rect.Right, rect.Bottom - innerRadius);

            // Bottom-right corner (rounded)
            if (innerRadius > 0)
            {
                elementPath.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            }

            // Bottom edge
            elementPath.AddLine(rect.Right - innerRadius, rect.Bottom, rect.Left + innerRadius, rect.Bottom);

            // Bottom-left corner (rounded)
            if (innerRadius > 0)
            {
                elementPath.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            }

            // Left edge back to top
            elementPath.AddLine(rect.Left, rect.Bottom - innerRadius, rect.Left, rect.Top);

            elementPath.CloseFigure();

            contentControl.Region?.Dispose();
            contentControl.Region = new Region(elementPath);
            elementPath.Dispose();
        }

        /// <summary>
        /// Creates a GraphicsPath for a rounded rectangle
        /// </summary>
        private GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            // Ensure radius doesn't exceed half the width or height
            int diameter = radius * 2;
            radius = Math.Min(radius, Math.Min(rect.Width / 2, rect.Height / 2));
            diameter = radius * 2;

            // Create rounded rectangle path with arcs at each corner
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);                              // Top-left
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);               // Top-right
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90); // Bottom-right
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);               // Bottom-left

            path.CloseFigure();
            return path;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            ClearRegions();
        }

        #endregion
    }
}
