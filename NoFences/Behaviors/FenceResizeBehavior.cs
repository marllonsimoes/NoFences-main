using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoFences.Behaviors
{
    /// <summary>
    /// Manages resize behavior for fence containers via border panels.
    /// Handles 5 resize directions: Left, Right, Top, Bottom, BottomRight (corner).
    /// Enforces minimum size and screen boundaries during resize.
    /// </summary>
    public class FenceResizeBehavior
    {
        #region Private Fields

        private readonly Control containerControl;
        private readonly Func<Rectangle> getBoundaryArea;
        private readonly Func<bool> canResizeCheck;
        private readonly int minSize;

        private bool isResizing;
        private Point resizeStartPoint; // In screen coordinates
        private Point resizeStartLocation;
        private Size resizeStartSize;
        private ResizeDirection resizeDirection;

        // Border panels
        private Control borderLeft;
        private Control borderRight;
        private Control borderTop;
        private Control borderBottom;
        private Control borderBottomRight; // Corner grip

        #endregion

        #region Events

        /// <summary>
        /// Fired when resize starts
        /// </summary>
        public event EventHandler<ResizeStartedEventArgs> ResizeStarted;

        /// <summary>
        /// Fired during resize (for throttled updates)
        /// </summary>
        public event EventHandler<ResizeChangedEventArgs> ResizeChanged;

        /// <summary>
        /// Fired when resize ends
        /// </summary>
        public event EventHandler<ResizeEndedEventArgs> ResizeEnded;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new resize behavior
        /// </summary>
        /// <param name="containerControl">The control to resize</param>
        /// <param name="getBoundaryArea">Function to get boundary rectangle</param>
        /// <param name="canResizeCheck">Function to check if resizing is allowed</param>
        /// <param name="minSize">Minimum width/height in pixels (default 150)</param>
        public FenceResizeBehavior(
            Control containerControl,
            Func<Rectangle> getBoundaryArea,
            Func<bool> canResizeCheck,
            int minSize = 150)
        {
            this.containerControl = containerControl ?? throw new ArgumentNullException(nameof(containerControl));
            this.getBoundaryArea = getBoundaryArea ?? throw new ArgumentNullException(nameof(getBoundaryArea));
            this.canResizeCheck = canResizeCheck ?? throw new ArgumentNullException(nameof(canResizeCheck));
            this.minSize = minSize;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether resize is currently in progress
        /// </summary>
        public bool IsResizing => isResizing;

        /// <summary>
        /// Current resize direction
        /// </summary>
        public ResizeDirection Direction => resizeDirection;

        /// <summary>
        /// Minimum size constraint
        /// </summary>
        public int MinSize => minSize;

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers border panels for resize behavior
        /// </summary>
        public void RegisterBorders(
            Control left,
            Control right,
            Control top,
            Control bottom,
            Control bottomRight)
        {
            borderLeft = left;
            borderRight = right;
            borderTop = top;
            borderBottom = bottom;
            borderBottomRight = bottomRight;
        }

        /// <summary>
        /// Attaches event handlers to border panels
        /// </summary>
        public void Attach()
        {
            if (borderLeft != null)
            {
                borderLeft.MouseDown += Border_MouseDown;
                borderLeft.MouseMove += Border_MouseMove;
                borderLeft.MouseUp += Border_MouseUp;
            }

            if (borderRight != null)
            {
                borderRight.MouseDown += Border_MouseDown;
                borderRight.MouseMove += Border_MouseMove;
                borderRight.MouseUp += Border_MouseUp;
            }

            if (borderTop != null)
            {
                borderTop.MouseDown += Border_MouseDown;
                borderTop.MouseMove += Border_MouseMove;
                borderTop.MouseUp += Border_MouseUp;
            }

            if (borderBottom != null)
            {
                borderBottom.MouseDown += Border_MouseDown;
                borderBottom.MouseMove += Border_MouseMove;
                borderBottom.MouseUp += Border_MouseUp;
            }

            if (borderBottomRight != null)
            {
                borderBottomRight.MouseDown += Border_MouseDown;
                borderBottomRight.MouseMove += Border_MouseMove;
                borderBottomRight.MouseUp += Border_MouseUp;
            }
        }

        /// <summary>
        /// Detaches event handlers from border panels
        /// </summary>
        public void Detach()
        {
            if (borderLeft != null)
            {
                borderLeft.MouseDown -= Border_MouseDown;
                borderLeft.MouseMove -= Border_MouseMove;
                borderLeft.MouseUp -= Border_MouseUp;
            }

            if (borderRight != null)
            {
                borderRight.MouseDown -= Border_MouseDown;
                borderRight.MouseMove -= Border_MouseMove;
                borderRight.MouseUp -= Border_MouseUp;
            }

            if (borderTop != null)
            {
                borderTop.MouseDown -= Border_MouseDown;
                borderTop.MouseMove -= Border_MouseMove;
                borderTop.MouseUp -= Border_MouseUp;
            }

            if (borderBottom != null)
            {
                borderBottom.MouseDown -= Border_MouseDown;
                borderBottom.MouseMove -= Border_MouseMove;
                borderBottom.MouseUp -= Border_MouseUp;
            }

            if (borderBottomRight != null)
            {
                borderBottomRight.MouseDown -= Border_MouseDown;
                borderBottomRight.MouseMove -= Border_MouseMove;
                borderBottomRight.MouseUp -= Border_MouseUp;
            }
        }

        /// <summary>
        /// Calculates new bounds during resize based on mouse position
        /// </summary>
        public Rectangle CalculateResizeBounds(Point currentMousePosition)
        {
            int deltaX = currentMousePosition.X - resizeStartPoint.X;
            int deltaY = currentMousePosition.Y - resizeStartPoint.Y;

            int newWidth = resizeStartSize.Width;
            int newHeight = resizeStartSize.Height;
            int newX = resizeStartLocation.X;
            int newY = resizeStartLocation.Y;

            switch (resizeDirection)
            {
                case ResizeDirection.Right:
                    newWidth = Math.Max(minSize, resizeStartSize.Width + deltaX);
                    break;

                case ResizeDirection.Bottom:
                    newHeight = Math.Max(minSize, resizeStartSize.Height + deltaY);
                    break;

                case ResizeDirection.BottomRight:
                    newWidth = Math.Max(minSize, resizeStartSize.Width + deltaX);
                    newHeight = Math.Max(minSize, resizeStartSize.Height + deltaY);
                    break;

                case ResizeDirection.Left:
                    // Moving left = negative deltaX, width should increase
                    int desiredWidth = resizeStartSize.Width - deltaX;
                    if (desiredWidth >= minSize)
                    {
                        newWidth = desiredWidth;
                        newX = resizeStartLocation.X + deltaX;
                    }
                    else
                    {
                        newWidth = minSize;
                        newX = resizeStartLocation.X + resizeStartSize.Width - minSize;
                    }
                    break;

                case ResizeDirection.Top:
                    // Moving up = negative deltaY, height should increase
                    int desiredHeight = resizeStartSize.Height - deltaY;
                    if (desiredHeight >= minSize)
                    {
                        newHeight = desiredHeight;
                        newY = resizeStartLocation.Y + deltaY;
                    }
                    else
                    {
                        newHeight = minSize;
                        newY = resizeStartLocation.Y + resizeStartSize.Height - minSize;
                    }
                    break;
            }

            return new Rectangle(newX, newY, newWidth, newHeight);
        }

        /// <summary>
        /// Applies boundary constraints to resize bounds
        /// </summary>
        public Rectangle ApplyBoundaries(Rectangle bounds)
        {
            Rectangle boundaries = getBoundaryArea();
            const int MinVisible = 50;

            int x = bounds.X;
            int y = bounds.Y;
            int width = bounds.Width;
            int height = bounds.Height;

            // Constrain X
            if (x < -width + MinVisible)
                x = -width + MinVisible;
            if (x > boundaries.Width - MinVisible)
                x = boundaries.Width - MinVisible;

            // Constrain Y
            if (y < 0)
                y = 0;
            if (y > boundaries.Height - MinVisible)
                y = boundaries.Height - MinVisible;

            // Constrain width/height to fit on screen
            if (x + width > boundaries.Width + width - MinVisible)
                width = Math.Max(minSize, boundaries.Width - x + width - MinVisible);
            if (y + height > boundaries.Height)
                height = Math.Max(minSize, boundaries.Height - y);

            return new Rectangle(x, y, width, height);
        }

        #endregion

        #region Private Event Handlers

        private void Border_MouseDown(object sender, MouseEventArgs e)
        {
            if (!canResizeCheck() || e.Button != MouseButtons.Left)
                return;

            isResizing = true;
            resizeStartPoint = Control.MousePosition; // Screen coordinates
            resizeStartLocation = containerControl.Location;
            resizeStartSize = containerControl.Size;

            // Determine direction based on which border
            if (sender == borderLeft)
                resizeDirection = ResizeDirection.Left;
            else if (sender == borderRight)
                resizeDirection = ResizeDirection.Right;
            else if (sender == borderTop)
                resizeDirection = ResizeDirection.Top;
            else if (sender == borderBottom)
                resizeDirection = ResizeDirection.Bottom;
            else if (sender == borderBottomRight)
                resizeDirection = ResizeDirection.BottomRight;
            else
                resizeDirection = ResizeDirection.None;

            // Suspend layout for performance
            containerControl.SuspendLayout();

            ResizeStarted?.Invoke(this, new ResizeStartedEventArgs(
                resizeDirection,
                resizeStartLocation,
                resizeStartSize));
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isResizing || e.Button != MouseButtons.Left)
                return;

            Point currentPos = Control.MousePosition;

            // Calculate new bounds
            Rectangle newBounds = CalculateResizeBounds(currentPos);

            // Apply boundaries
            newBounds = ApplyBoundaries(newBounds);

            // Update container
            containerControl.SetBounds(newBounds.X, newBounds.Y, newBounds.Width, newBounds.Height);

            // Notify
            ResizeChanged?.Invoke(this, new ResizeChangedEventArgs(
                resizeDirection,
                new Point(newBounds.X, newBounds.Y),
                new Size(newBounds.Width, newBounds.Height)));
        }

        private void Border_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isResizing)
                return;

            isResizing = false;

            // Resume layout
            containerControl.ResumeLayout(true);

            // Final notification
            ResizeEnded?.Invoke(this, new ResizeEndedEventArgs(
                resizeDirection,
                containerControl.Location,
                containerControl.Size));

            resizeDirection = ResizeDirection.None;
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Resize direction enumeration
    /// </summary>
    public enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        BottomRight
    }

    /// <summary>
    /// Event args for resize started
    /// </summary>
    public class ResizeStartedEventArgs : EventArgs
    {
        public ResizeDirection Direction { get; }
        public Point StartLocation { get; }
        public Size StartSize { get; }

        public ResizeStartedEventArgs(ResizeDirection direction, Point startLocation, Size startSize)
        {
            Direction = direction;
            StartLocation = startLocation;
            StartSize = startSize;
        }
    }

    /// <summary>
    /// Event args for resize changed
    /// </summary>
    public class ResizeChangedEventArgs : EventArgs
    {
        public ResizeDirection Direction { get; }
        public Point NewLocation { get; }
        public Size NewSize { get; }

        public ResizeChangedEventArgs(ResizeDirection direction, Point newLocation, Size newSize)
        {
            Direction = direction;
            NewLocation = newLocation;
            NewSize = newSize;
        }
    }

    /// <summary>
    /// Event args for resize ended
    /// </summary>
    public class ResizeEndedEventArgs : EventArgs
    {
        public ResizeDirection Direction { get; }
        public Point FinalLocation { get; }
        public Size FinalSize { get; }

        public ResizeEndedEventArgs(ResizeDirection direction, Point finalLocation, Size finalSize)
        {
            Direction = direction;
            FinalLocation = finalLocation;
            FinalSize = finalSize;
        }
    }

    #endregion
}
