using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoFences.Behaviors
{
    /// <summary>
    /// Manages drag behavior for fence containers via title bar.
    /// Handles mouse dragging, screen boundary enforcement, and cursor changes.
    /// </summary>
    public class FenceDragBehavior
    {
        #region Private Fields

        private readonly Control containerControl;
        private readonly Control dragHandle;
        private readonly Func<Rectangle> getBoundaryArea;
        private readonly Func<bool> canDragCheck;

        private bool isDragging;
        private Point dragStartPoint;
        private const int MinVisible = 50; // Minimum pixels visible on screen

        #endregion

        #region Events

        /// <summary>
        /// Fired when the fence position changes during drag
        /// </summary>
        public event EventHandler<Point> PositionChanged;

        /// <summary>
        /// Fired when dragging starts
        /// </summary>
        public event EventHandler DragStarted;

        /// <summary>
        /// Fired when dragging ends
        /// </summary>
        public event EventHandler<Point> DragEnded;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new drag behavior
        /// </summary>
        /// <param name="containerControl">The control to move when dragging</param>
        /// <param name="dragHandle">The control that initiates the drag (usually title bar)</param>
        /// <param name="getBoundaryArea">Function to get the boundary rectangle (usually parent bounds)</param>
        /// <param name="canDragCheck">Function to check if dragging is allowed</param>
        public FenceDragBehavior(
            Control containerControl,
            Control dragHandle,
            Func<Rectangle> getBoundaryArea,
            Func<bool> canDragCheck)
        {
            this.containerControl = containerControl ?? throw new ArgumentNullException(nameof(containerControl));
            this.dragHandle = dragHandle ?? throw new ArgumentNullException(nameof(dragHandle));
            this.getBoundaryArea = getBoundaryArea ?? throw new ArgumentNullException(nameof(getBoundaryArea));
            this.canDragCheck = canDragCheck ?? throw new ArgumentNullException(nameof(canDragCheck));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the fence is currently being dragged
        /// </summary>
        public bool IsDragging => isDragging;

        #endregion

        #region Public Methods

        /// <summary>
        /// Attaches the behavior to the drag handle events
        /// </summary>
        public void Attach()
        {
            dragHandle.MouseDown += DragHandle_MouseDown;
            dragHandle.MouseMove += DragHandle_MouseMove;
            dragHandle.MouseUp += DragHandle_MouseUp;
        }

        /// <summary>
        /// Detaches the behavior from the drag handle events
        /// </summary>
        public void Detach()
        {
            dragHandle.MouseDown -= DragHandle_MouseDown;
            dragHandle.MouseMove -= DragHandle_MouseMove;
            dragHandle.MouseUp -= DragHandle_MouseUp;
        }

        /// <summary>
        /// Applies screen boundary constraints to a position and size
        /// </summary>
        public Rectangle ApplyBoundaries(int x, int y, int width, int height)
        {
            Rectangle boundaries = getBoundaryArea();

            // Keep at least MinVisible pixels visible on each side
            // Constrain X
            if (x < -width + MinVisible)
                x = -width + MinVisible;
            if (x > boundaries.Width - MinVisible)
                x = boundaries.Width - MinVisible;

            // Constrain Y
            if (y < 0)
                y = 0; // Don't go above boundary top
            if (y > boundaries.Height - MinVisible)
                y = boundaries.Height - MinVisible;

            // Constrain width/height to fit on screen
            if (x + width > boundaries.Width + width - MinVisible)
                width = Math.Max(150, boundaries.Width - x + width - MinVisible);
            if (y + height > boundaries.Height)
                height = Math.Max(150, boundaries.Height - y);

            return new Rectangle(x, y, width, height);
        }

        #endregion

        #region Private Event Handlers

        private void DragHandle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (!canDragCheck())
                return;

            isDragging = true;
            dragStartPoint = e.Location;
            dragHandle.Cursor = Cursors.SizeAll;

            DragStarted?.Invoke(this, EventArgs.Empty);
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || e.Button != MouseButtons.Left)
                return;

            // Calculate new position
            int newX = containerControl.Location.X + (e.X - dragStartPoint.X);
            int newY = containerControl.Location.Y + (e.Y - dragStartPoint.Y);

            // Apply screen boundaries
            var bounds = ApplyBoundaries(newX, newY, containerControl.Width, containerControl.Height);
            Point newLocation = new Point(bounds.X, bounds.Y);

            // Update position
            containerControl.Location = newLocation;

            // Notify
            PositionChanged?.Invoke(this, newLocation);
        }

        private void DragHandle_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isDragging)
                return;

            isDragging = false;
            dragHandle.Cursor = Cursors.Default;

            // Final notification with current position
            DragEnded?.Invoke(this, containerControl.Location);
        }

        #endregion
    }
}
