using log4net;
using NoFences.Core.Model;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32.Desktop;
using NoFences.Win32.Window;
using NoFences.Win32.Shell;
using System;
using System.Drawing;
using System.Windows.Forms;
using WpfControls = System.Windows.Controls;

namespace NoFences.Behaviors
{
    /// <summary>
    /// Manages fade in/out animation for fence containers.
    /// Fences fade out when the mouse leaves and fade in when the mouse enters.
    /// Handles opacity for title, borders, and WPF content.
    /// </summary>
    public class FenceFadeAnimationBehavior : IDisposable
    {
        #region Private Fields

        private static readonly ILog log = LogManager.GetLogger(typeof(FenceFadeAnimationBehavior));

        private readonly Control containerControl;
        private readonly Func<FenceInfo> getFenceInfo;
        private readonly Func<bool> hasContentCheck;

        private Timer fadeTimer;
        private Timer mouseTrackingTimer;
        private bool isFadedOut = false;
        private double currentOpacity = 1.0;
        private bool wasMouseOver = false; // Track previous mouse state for event firing

        private const double FadedContentOpacity = 0.8;
        private const float FadeInDuration = 500.0f; // milliseconds
        private const float FadeOutDuration = 1000.0f; // milliseconds
        private const float FadeStepInterval = 16.0f; // ~60 FPS
        private const int MouseCheckInterval = 100; // Check mouse position every 100ms

        #endregion

        #region Events

        /// <summary>
        /// Fired when opacity changes, allowing container to update its visuals
        /// </summary>
        public event EventHandler<double> OpacityChanged;

        /// <summary>
        /// Fired when mouse enters the fence area
        /// </summary>
        public event EventHandler MouseEntered;

        /// <summary>
        /// Fired when mouse leaves the fence area
        /// </summary>
        public event EventHandler MouseLeft;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new fade animation behavior
        /// </summary>
        /// <param name="containerControl">The control to apply fading to</param>
        /// <param name="getFenceInfo">Function to get current fence info</param>
        /// <param name="hasContentCheck">Function to check if fence has content (required for fade)</param>
        public FenceFadeAnimationBehavior(
            Control containerControl,
            Func<FenceInfo> getFenceInfo,
            Func<bool> hasContentCheck)
        {
            this.containerControl = containerControl ?? throw new ArgumentNullException(nameof(containerControl));
            this.getFenceInfo = getFenceInfo ?? throw new ArgumentNullException(nameof(getFenceInfo));
            this.hasContentCheck = hasContentCheck ?? throw new ArgumentNullException(nameof(hasContentCheck));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Current opacity value (0.0 - 1.0)
        /// </summary>
        public double CurrentOpacity => currentOpacity;

        /// <summary>
        /// Whether the fence is currently faded out
        /// </summary>
        public bool IsFadedOut => isFadedOut;

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts mouse tracking for automatic fade in/out
        /// </summary>
        public void Start()
        {
            if (mouseTrackingTimer != null)
                return;

            mouseTrackingTimer = new Timer();
            mouseTrackingTimer.Interval = MouseCheckInterval;
            mouseTrackingTimer.Tick += MouseTrackingTimer_Tick;
            mouseTrackingTimer.Start();

            log.Debug($"Started mouse tracking");
        }

        /// <summary>
        /// Stops mouse tracking and cleans up timers
        /// </summary>
        public void Stop()
        {
            if (fadeTimer != null)
            {
                fadeTimer.Stop();
                fadeTimer.Dispose();
                fadeTimer = null;
            }

            if (mouseTrackingTimer != null)
            {
                mouseTrackingTimer.Stop();
                mouseTrackingTimer.Dispose();
                mouseTrackingTimer = null;
            }
        }

        /// <summary>
        /// Manually triggers fade in animation
        /// </summary>
        public void FadeIn()
        {
            // Don't fade if fade effect is disabled or fence has no content
            if (!ShouldEnableFade())
            {
                // If fade is disabled, immediately set to full opacity
                var fenceInfo = getFenceInfo();
                if (fenceInfo != null && !fenceInfo.EnableFadeEffect)
                {
                    isFadedOut = false;
                    currentOpacity = 1.0;
                    RaiseOpacityChanged();
                }
                return;
            }

            if (fadeTimer != null)
            {
                fadeTimer.Stop();
                fadeTimer.Dispose();
            }

            isFadedOut = false;

            var fadeStartTime = DateTime.Now;
            log.Debug($"Starting fade-in from opacity {currentOpacity}");

            // Calculate steps for smooth animation
            double targetOpacity = 1.0;
            float steps = FadeInDuration / FadeStepInterval;
            double opacityStep = (targetOpacity - currentOpacity) / steps;

            fadeTimer = new Timer();
            fadeTimer.Interval = (int)FadeStepInterval;
            fadeTimer.Tick += (s, e) =>
            {
                currentOpacity += opacityStep;

                if (currentOpacity >= targetOpacity)
                {
                    currentOpacity = targetOpacity;
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    fadeTimer = null;
                    var elapsed = (DateTime.Now - fadeStartTime).TotalMilliseconds;
                    log.Debug($"Fade-in completed in {elapsed}ms");
                }

                RaiseOpacityChanged();
            };
            fadeTimer.Start();
        }

        /// <summary>
        /// Manually triggers fade out animation
        /// </summary>
        public void FadeOut()
        {
            // Don't fade if fade effect is disabled or fence has no content
            if (!ShouldEnableFade())
            {
                // If fade is disabled, keep at full opacity
                var fenceInfo = getFenceInfo();
                if (fenceInfo != null && !fenceInfo.EnableFadeEffect)
                {
                    isFadedOut = false;
                    currentOpacity = 1.0;
                    RaiseOpacityChanged();
                }
                return;
            }

            if (fadeTimer != null)
            {
                fadeTimer.Stop();
                fadeTimer.Dispose();
            }

            isFadedOut = true;

            var fadeStartTime = DateTime.Now;
            log.Debug($"Starting fade-out");

            // Calculate steps for smooth animation
            double targetOpacity = 0.0; // Fully transparent for title/borders
            float steps = FadeOutDuration / FadeStepInterval;
            double opacityStep = (targetOpacity - currentOpacity) / steps;

            fadeTimer = new Timer();
            fadeTimer.Interval = (int)FadeStepInterval;
            fadeTimer.Tick += (s, e) =>
            {
                currentOpacity += opacityStep;

                if (currentOpacity <= targetOpacity)
                {
                    currentOpacity = targetOpacity;
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    fadeTimer = null;
                    var elapsed = (DateTime.Now - fadeStartTime).TotalMilliseconds;
                    log.Debug($"Fade-out completed in {elapsed}ms");
                }

                RaiseOpacityChanged();
            };
            fadeTimer.Start();
        }

        /// <summary>
        /// Sets opacity to minified state (30%)
        /// </summary>
        public void SetMinifiedOpacity()
        {
            if (!isFadedOut)
            {
                isFadedOut = true;
                currentOpacity = 0.3;
                RaiseOpacityChanged();
            }
        }

        /// <summary>
        /// Resets opacity to full
        /// </summary>
        public void ResetOpacity()
        {
            isFadedOut = false;
            currentOpacity = 1.0;
            RaiseOpacityChanged();
        }

        #endregion

        #region Private Methods

        private void MouseTrackingTimer_Tick(object sender, EventArgs e)
        {
            // Check if fade effect is enabled first
            var fenceInfo = getFenceInfo();
            if (fenceInfo != null && !fenceInfo.EnableFadeEffect)
            {
                // Fade is disabled - ensure we're at full opacity and don't process further
                if (currentOpacity != 1.0 || isFadedOut)
                {
                    isFadedOut = false;
                    currentOpacity = 1.0;
                    RaiseOpacityChanged();
                }
                return;
            }

            // Check if mouse is over the fence control bounds
            Point mousePos = containerControl.PointToClient(Control.MousePosition);
            bool isMouseOverBounds = containerControl.ClientRectangle.Contains(mousePos);

            // IMPORTANT: Also check if the fence is actually the window under the cursor
            // (not occluded by another window on top)
            bool isMouseActuallyOver = isMouseOverBounds && IsFenceWindowUnderCursor();

            // Detect state change and fire events
            if (isMouseActuallyOver && !wasMouseOver)
            {
                // Mouse just entered
                wasMouseOver = true;
                MouseEntered?.Invoke(this, EventArgs.Empty);
            }
            else if (!isMouseActuallyOver && wasMouseOver)
            {
                // Mouse just left
                wasMouseOver = false;
                MouseLeft?.Invoke(this, EventArgs.Empty);
            }

            if (isMouseActuallyOver)
            {
                // Mouse is over - fade in if currently faded out
                if (isFadedOut)
                {
                    log.Debug($"Mouse detected over fence");
                    FadeIn();
                }
            }
            else
            {
                // Mouse left - apply fade out if needed
                if (!isFadedOut && ShouldEnableFade())
                {
                    log.Debug($"Mouse left fence");
                    FadeOut();
                }
            }
        }

        /// <summary>
        /// Checks if this fence (or its parent canvas) is actually the window under the cursor.
        /// Returns false if another window is on top, even if cursor is within fence bounds.
        /// </summary>
        private bool IsFenceWindowUnderCursor()
        {
            try
            {
                // Get cursor position in screen coordinates
                if (!WindowUtil.GetCursorPos(out WindowUtil.POINT cursorPos))
                    return false;

                // Get the window under the cursor
                IntPtr windowUnderCursor = WindowUtil.WindowFromPoint(cursorPos);
                if (windowUnderCursor == IntPtr.Zero)
                    return false;

                // Check if it's this fence
                if (windowUnderCursor == containerControl.Handle)
                    return true;

                // Check if it's the parent canvas (child controls return parent's handle)
                if (containerControl.Parent != null && windowUnderCursor == containerControl.Parent.Handle)
                    return true;

                // Check if the window under cursor is a descendant of the canvas
                // (could be a child control like ElementHost, title panel, etc.)
                IntPtr ancestor = WindowUtil.GetAncestor(windowUnderCursor, WindowUtil.GA_ROOT);
                if (containerControl.Parent != null && ancestor == containerControl.Parent.Handle)
                    return true;

                return false;
            }
            catch
            {
                // If anything fails, assume we're not under cursor
                return false;
            }
        }

        /// <summary>
        /// Checks if fade animation should be enabled for this fence
        /// </summary>
        private bool ShouldEnableFade()
        {
            var fenceInfo = getFenceInfo();
            bool hasContent = hasContentCheck();
            bool fadeEnabled = fenceInfo?.EnableFadeEffect ?? true;

            log.Debug($"Fade check - HasContent={hasContent}, FadeEnabled={fadeEnabled}");
            return hasContent && fadeEnabled;
        }

        private void RaiseOpacityChanged()
        {
            OpacityChanged?.Invoke(this, currentOpacity);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }
}
